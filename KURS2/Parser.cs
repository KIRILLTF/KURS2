using System.Text.RegularExpressions;

class Parser
{
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

    public Sentence Parse(string input)
    {
        if (modulePattern.IsMatch(input))
        {
            var match = modulePattern.Match(input);
            var name = SplitName(match.Groups[1].Value);
            var variables = ExtractVariables(match.Groups[2].Value);

            return new ModuleSentence(name, variables);
        }
        if (importPattern.IsMatch(input))
        {
            var match = importPattern.Match(input);
            var name = SplitName(match.Groups[1].Value);
            var variables = ExtractVariables(match.Groups[2].Value);
            var alias = match.Groups[3].Success ? match.Groups[3].Value : null;

            return new ImportSentence(name, variables, alias);
        }
        if (letPattern.IsMatch(input))
        {
            var match = letPattern.Match(input);
            var name = SplitName(match.Groups[1].Value);
            var variables = ExtractVariables(match.Groups[2].Value);
            var expression = match.Groups[3].Value;
            var hasWhere = input.EndsWith(" where");

            LetSentence letSentence = new LetSentence(name, variables, expression, hasWhere);

            // Проверка корректности выражения
            ValidateExpression(letSentence);

            // Заполняем списки операторов и функций (простейший способ)
            letSentence.Operators = ExtractOperators(expression);
            letSentence.Functions = ExtractFunctions(expression);

            return letSentence;
        }

        throw new ArgumentException($"Ошибка: Некорректная строка — {input}");
    }

    // Разбивает имя на части по точкам
    private List<string> SplitName(string name)
    {
        return name.Split('.').ToList();
    }

    // Извлекает переменные из скобок
    private List<string> ExtractVariables(string variables)
    {
        if (string.IsNullOrEmpty(variables))
            return null;

        return variables.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    // Простейший метод для извлечения операторов
    // Ищем символы +, -, /, *, ^
    private List<string> ExtractOperators(string expression)
    {
        var result = new List<string>();
        var matches = Regex.Matches(expression, @"[\+\-\*/\^]");
        foreach (Match m in matches)
        {
            result.Add(m.Value);
        }
        return result;
    }

    // Простейший метод для извлечения имен функций
    // Ищем любые последовательности (a-zA-Z_) перед "("
    private List<string> ExtractFunctions(string expression)
    {
        var result = new List<string>();
        var matches = Regex.Matches(expression, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(");
        foreach (Match m in matches)
        {
            // Группа 1 — это предполагаемое имя функции
            result.Add(m.Groups[1].Value);
        }
        return result;
    }

    // Метод для удаления ненужных символов, форматирования и т.п.
    public string StringChanger(string text)
    {
        List<string> lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

        for (int i = 0; i < lines.Count; i++)
        {
            // Оставляем только одиночные пробелы
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

    // Разбивает одну строку с несколькими import на несколько строк
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

    // Проверка корректности выражения в let-предложении
    public void ValidateExpression(LetSentence letSentence)
    {
        string expression = letSentence.Expression;

        // Проверка баланса скобок
        if (expression.Count(c => c == '(') != expression.Count(c => c == ')'))
            throw new ArgumentException($"Ошибка: Некорректное выражение — {expression}");

        // Проверка на дубли операторов вида ++, --, **, ^^ и т.п.
        if (Regex.IsMatch(expression.Replace(" ", ""), @"[\+\-\*/\^]{2,}"))
            throw new ArgumentException($"Ошибка: Некорректное выражение — {expression}");

        // Проверка, чтобы выражение не начиналось и не заканчивалось оператором
        if (Regex.IsMatch(expression.Trim(), @"^[\+\-\*/\^]|[\+\-\*/\^]$"))
            throw new ArgumentException($"Ошибка: Некорректное выражение — {expression}");
    }
}
