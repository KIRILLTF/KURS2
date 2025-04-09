using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class Parser
{
    //  Регулярные выражения для Module, Import, Let
    private static readonly Regex modulePattern = new(
        @"^\s*module\s+([a-zA-Z0-9_.]+(?:\.[a-zA-Z0-9_]+)*)(?:\s*\(([^)]*)\))?\s+where$",
        RegexOptions.Compiled
    );

    private static readonly Regex importPattern = new(
        @"^\s*import\s+([a-zA-Z0-9_.]+(?:\.[a-zA-Z0-9_]+)*)(?:\s*\(([^)]*)\))?(?:\s+as\s+([a-zA-Z0-9_.]+))?$",
        RegexOptions.Compiled
    );

    private static readonly Regex letPattern = new(
        @"^\s*let\s+([a-zA-Z0-9_.]+)(?:\s*\(([^)]*)\))?\s*:=\s*(.+?)(?:\s+where)?$",
        RegexOptions.Compiled
    );

    //  Основной метод Parse
    public Sentence Parse(string input)
    {
        // Проверяем: module
        if (modulePattern.IsMatch(input))
        {
            var match = modulePattern.Match(input);
            var name = SplitName(match.Groups[1].Value);
            var variables = ExtractVariables(match.Groups[2].Value);

            return new ModuleSentence(name, variables);
        }
        // Проверяем: import
        if (importPattern.IsMatch(input))
        {
            var match = importPattern.Match(input);
            var name = SplitName(match.Groups[1].Value);
            var variables = ExtractVariables(match.Groups[2].Value);
            var alias = match.Groups[3].Success ? match.Groups[3].Value : null;

            return new ImportSentence(name, variables, alias);
        }
        // Проверяем: let
        if (letPattern.IsMatch(input))
        {
            var match = letPattern.Match(input);
            var name = SplitName(match.Groups[1].Value);
            var variables = ExtractVariables(match.Groups[2].Value);
            var expressionString = match.Groups[3].Value;
            var hasWhere = input.EndsWith(" where");

            // Парсим выражение в дерево (AST)
            ExpressionNode expressionAst = ParseExpressionNode(expressionString);

            // Опциональная проверка строки (как у вас было)
            ValidateExpressionString(expressionString);

            return new LetSentence(
                name,
                variables,
                expressionString,
                hasWhere,
                expressionAst
            );
        }

        throw new ArgumentException($"Ошибка: Некорректная строка — {input}");
    }

    //  Помощники для Module/Import/Let

    // Разбивает имя на части по точкам
    private List<string> SplitName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new List<string>();
        return name.Split('.').ToList();
    }

    // Извлекает переменные из скобок
    private List<string> ExtractVariables(string variables)
    {
        if (string.IsNullOrEmpty(variables))
            return null;

        return variables.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    // Валидация выражения (старая логика, если нужно)
    private void ValidateExpressionString(string expression)
    {
        // Проверка на парность скобок
        if (expression.Count(c => c == '(') != expression.Count(c => c == ')'))
            throw new ArgumentException($"Ошибка: Некорректное выражение — {expression}");

        // Проверка на дублирующиеся операторы
        if (Regex.IsMatch(expression.Replace(" ", ""), @"[\+\-\*/\^]{2,}"))
            throw new ArgumentException($"Ошибка: Некорректное выражение — {expression}");

        // Проверка, что выражение не начинается/заканчивается оператором
        if (Regex.IsMatch(expression.Trim(), @"^[\+\-\*/\^]|[\+\-\*/\^]$"))
            throw new ArgumentException($"Ошибка: Некорректное выражение — {expression}");
    }

    //  Метод SplitImports (как у вас)
    public string SplitImports(string text)
    {
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var result = new List<string>();

        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("import"))
            {
                var imports = Regex.Split(line,
                    @"(?<=\))\s+(?=import)|(?<=import\s+[a-zA-Z0-9_.]+)\s+(?=import)");
                result.AddRange(imports);
            }
            else
            {
                result.Add(line);
            }
        }

        return string.Join(Environment.NewLine, result);
    }

    //  Метод StringChanger (как у вас)
    public string StringChanger(string text)
    {
        List<string> lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

        for (int i = 0; i < lines.Count; i++)
        {
            // Оставляем только "одинарные" пробелы
            lines[i] = Regex.Replace(lines[i].Trim(), @"\s+", " ");

            // Заменяем "( " на "("
            lines[i] = Regex.Replace(lines[i], @"\(\s", "(");

            // Добавляем пробел после запятой
            lines[i] = Regex.Replace(lines[i], @",(?=\S)", ", ");

            // Заменяем " )" на ")"
            lines[i] = Regex.Replace(lines[i], @"\s\)", ")");

            // Заменяем " , " на ", "
            lines[i] = Regex.Replace(lines[i], @"\s,\s", ", ");
        }

        return string.Join(Environment.NewLine, lines);
    }

    //  Ниже — логика рекурсивного спуска, расширенная
    //  для парсинга вызовов функций (func(a, b, c))
    private List<Token> tokens;
    private int currentTokenIndex;
    private Token CurrentToken => tokens[currentTokenIndex];
    private void NextToken() => currentTokenIndex++;

    /// <summary>
    /// Превращаем строку-выражение в AST
    /// </summary>
    private ExpressionNode ParseExpressionNode(string expressionString)
    {
        var lexer = new Lexer(expressionString);
        tokens = lexer.Tokenize();
        currentTokenIndex = 0;

        ExpressionNode node = ParseExpression();

        // Если после разбора осталось что-то «лишнее», кидаем ошибку
        if (CurrentToken.Type != TokenType.End)
            throw new ArgumentException($"Лишние символы в выражении: '{CurrentToken.Value}'");

        return node;
    }

    /// <summary>
    /// Expression = Term { ("+" | "-") Term }
    /// </summary>
    private ExpressionNode ParseExpression()
    {
        ExpressionNode left = ParseTerm();

        while (CurrentToken.Type == TokenType.Plus || CurrentToken.Type == TokenType.Minus)
        {
            string op = CurrentToken.Value;  // + или -
            NextToken(); // пропускаем оператор
            ExpressionNode right = ParseTerm();
            left = new BinaryExpressionNode(left, op, right);
        }

        return left;
    }

    /// <summary>
    /// Term = Factor { ("*" | "/" | "^") Factor }
    /// </summary>
    private ExpressionNode ParseTerm()
    {
        ExpressionNode left = ParseFactor();

        while (
            CurrentToken.Type == TokenType.Multiply ||
            CurrentToken.Type == TokenType.Divide ||
            CurrentToken.Type == TokenType.Power
        )
        {
            string op = CurrentToken.Value; // * / ^
            NextToken();
            ExpressionNode right = ParseFactor();
            left = new BinaryExpressionNode(left, op, right);
        }

        return left;
    }

    /// <summary>
    /// Factor = (("+" | "-") Factor) | FunctionCallOrVar | "(" Expression ")"
    /// </summary>
    private ExpressionNode ParseFactor()
    {
        // Унарные операторы +/-
        if (CurrentToken.Type == TokenType.Plus || CurrentToken.Type == TokenType.Minus)
        {
            string op = CurrentToken.Value;
            NextToken();
            ExpressionNode factor = ParseFactor();
            return new UnaryExpressionNode(op, factor);
        }

        // Скобки: (expr)
        if (CurrentToken.Type == TokenType.LParen)
        {
            NextToken(); // пропускаем '('
            ExpressionNode node = ParseExpression();

            if (CurrentToken.Type != TokenType.RParen)
                throw new ArgumentException("Пропущена закрывающая скобка ')'");

            NextToken(); // пропускаем ')'
            return node;
        }

        // Иначе считаем, что это либо число, либо functionCall, либо переменная
        return ParseFunctionCallOrVar();
    }

    /// <summary>
    /// Считает, что перед нами идентификатор (переменная).
    /// Если за ним идёт "(" — значит, это вызов функции: f(a, b, c).
    /// Иначе — просто VariableNode.
    /// </summary>
    private ExpressionNode ParseFunctionCallOrVar()
    {
        if (CurrentToken.Type == TokenType.Identifier)
        {
            string name = CurrentToken.Value;
            NextToken(); // Съедаем идентификатор

            // Если дальше идёт "(", то это вызов: name(...)
            if (CurrentToken.Type == TokenType.LParen)
            {
                NextToken(); // пропускаем '('
                List<ExpressionNode> args = new List<ExpressionNode>();

                // Если не закрывающая скобка, парсим аргументы
                if (CurrentToken.Type != TokenType.RParen)
                {
                    // Парсим хотя бы одно выражение
                    args.Add(ParseExpression());

                    // Пока идёт запятая, парсим следующее
                    while (CurrentToken.Type == TokenType.Comma)
                    {
                        NextToken(); // пропускаем ','
                        args.Add(ParseExpression());
                    }

                    // Теперь должна быть закрывающая скобка
                    if (CurrentToken.Type != TokenType.RParen)
                        throw new ArgumentException("Ожидалась ')' после списка аргументов");
                }

                NextToken(); // пропускаем ')'
                return new FunctionCallNode(name, args);
            }
            else
            {
                // Иначе просто переменная
                return new VariableNode(name);
            }
        }
        else if (CurrentToken.Type == TokenType.Number)
        {
            // Число
            if (!double.TryParse(CurrentToken.Value, out double val))
            {
                throw new ArgumentException($"Некорректное число: {CurrentToken.Value}");
            }
            NextToken();
            return new NumberNode(val);
        }

        throw new ArgumentException($"Неожиданный токен: {CurrentToken.Value}");
    }
}
