using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ParserRulesGenerator
{
    // Печатает Expression-дерево (включая ошибки, вызовы функций и кортежи)
    public static class ExpressionPrinter
    {
        public static void Print(Expression expr, string indent = "")
        {
            switch (expr)
            {
                case BinaryExpr bin:
                    Console.WriteLine($"{indent}BinaryExpr \"{bin.Op}\"");
                    Console.WriteLine($"{indent}  Left:");
                    Print(bin.Left, indent + "    ");
                    Console.WriteLine($"{indent}  Right:");
                    Print(bin.Right, indent + "    ");
                    break;
                case NumberExpr num:
                    Console.WriteLine($"{indent}NumberExpr {num.Value}");
                    break;
                case VariableExpr v:
                    Console.WriteLine($"{indent}VariableExpr \"{v.Name}\"");
                    break;
                case FunctionCallExpr fce:
                    Console.WriteLine($"{indent}FunctionCallExpr \"{fce.FunctionName}\"");
                    for (int i = 0; i < fce.Arguments.Count; i++)
                    {
                        Console.WriteLine($"{indent}  Arg{i + 1}:");
                        Print(fce.Arguments[i], indent + "    ");
                    }
                    break;
                case TupleExpr tuple:
                    Console.WriteLine($"{indent}TupleExpr:");
                    for (int i = 0; i < tuple.Items.Count; i++)
                    {
                        Console.WriteLine($"{indent}  Item{i + 1}:");
                        Print(tuple.Items[i], indent + "    ");
                    }
                    break;
                case ErrorExpr err:
                    Console.WriteLine($"{indent}ErrorExpr: {err.Message}");
                    break;
                default:
                    Console.WriteLine($"{indent}Unknown expression type: {expr.GetType().Name}");
                    break;
            }
        }
    }

    // Базовый класс всех выражений
    public abstract class Expression { }

    // Бинарное выражение: a + b, a * b, a ^ b, и т.д.
    public class BinaryExpr : Expression
    {
        public string Op;
        public Expression Left;
        public Expression Right;
        public BinaryExpr(string op, Expression left, Expression right)
        {
            Op = op;
            Left = left;
            Right = right;
        }
    }

    // Числовой литерал
    public class NumberExpr : Expression
    {
        public double Value;
        public NumberExpr(double value) => Value = value;
    }

    // Переменная (идентификатор)
    public class VariableExpr : Expression
    {
        public string Name;
        public VariableExpr(string name) => Name = name;
    }

    // Вызов функции, например: f(a, b)
    public class FunctionCallExpr : Expression
    {
        public string FunctionName;
        public List<Expression> Arguments;
        public FunctionCallExpr(string functionName, List<Expression> arguments)
        {
            FunctionName = functionName;
            Arguments = arguments;
        }
    }

    // Кортеж выражений (например, (a, b))
    public class TupleExpr : Expression
    {
        public List<Expression> Items;
        public TupleExpr(List<Expression> items)
        {
            Items = items;
        }
    }

    // Узел ошибки
    public class ErrorExpr : Expression
    {
        public string Message;
        public ErrorExpr(string message) => Message = message;
    }

    public static class ExpressionParser
    {
        private static List<string> _tokens;
        private static int _pos;

        /// <summary>
        /// Точка входа: разбирает строку как арифметическое выражение.
        /// Перед разбором вызывается CleanExpression, который удаляет завершающие запятые и лишние закрывающие скобки.
        /// </summary>
        public static Expression ParseExpressionNode(string input)
        {
            return Parse(CleanExpression(input));
        }

        public static Expression Parse(string input)
        {
            input = input.Trim();
            _tokens = Tokenize(input);
            _pos = 0;

            Expression expr = ParseExpr();
            if (expr == null)
                return new ErrorExpr("Парсер вернул null");

            // Пропускаем оставшиеся токены, если это только запятые или закрывающие скобки.
            while (_pos < _tokens.Count && (_tokens[_pos] == ")" || _tokens[_pos] == ","))
                _pos++;

            return expr;
        }

        private static List<string> Tokenize(string str)
        {
            str = str.Replace("^", " ^ ")
                     .Replace("(", " ( ").Replace(")", " ) ")
                     .Replace("+", " + ").Replace("-", " - ")
                     .Replace("*", " * ").Replace("/", " / ")
                     .Replace(",", " , ");
            return new List<string>(str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        // Expression ::= Term { ("+" | "-") Term }*
        private static Expression ParseExpr()
        {
            Expression left = ParseTerm();
            if (IsError(left)) return left;

            while (Match("+") || Match("-"))
            {
                string op = _tokens[_pos - 1];
                Expression right = ParseTerm();
                if (IsError(right)) return right;
                left = new BinaryExpr(op, left, right);
            }
            return left;
        }

        // Term ::= Factor { ("*" | "/") Factor }*
        private static Expression ParseTerm()
        {
            Expression left = ParseFactor();
            if (IsError(left)) return left;

            while (Match("*") || Match("/"))
            {
                string op = _tokens[_pos - 1];
                Expression right = ParseFactor();
                if (IsError(right)) return right;
                left = new BinaryExpr(op, left, right);
            }
            return left;
        }

        // Factor ::= Primary { "^" Factor }*
        private static Expression ParseFactor()
        {
            Expression left = ParsePrimary();
            if (IsError(left)) return left;

            while (Match("^"))
            {
                string op = _tokens[_pos - 1];
                Expression right = ParseFactor();
                if (IsError(right)) return right;
                left = new BinaryExpr(op, left, right);
            }
            return left;
        }

        // Primary ::= number | identifier | functionCall | parenthesized expression (или кортеж)
        private static Expression ParsePrimary()
        {
            string token = CurrentToken();
            if (token == null)
                return new ErrorExpr("Ожидалось число, переменная, функция или '(' — строка закончилась");

            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                _pos++;
                return new NumberExpr(val);
            }
            else if (Regex.IsMatch(token, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                _pos++;
                if (CurrentToken() == "(")
                    return ParseFunctionCall(token);
                return new VariableExpr(token);
            }
            else if (token == "(")
            {
                return ParseParenExpression();
            }
            else
            {
                return new ErrorExpr($"Неизвестный токен: {token}");
            }
        }

        /// <summary>
        /// Разбирает выражение в круглых скобках.
        /// Если внутри на внешнем уровне встречается запятая, считается, что это кортеж.
        /// </summary>
        private static Expression ParseParenExpression()
        {
            if (CurrentToken() != "(")
                return new ErrorExpr("Ожидалась открывающая скобка '('");
            _pos++; // пропускаем "("

            List<Expression> items = new List<Expression>();
            Expression expr = ParseExpr();
            if (IsError(expr)) return expr;
            items.Add(expr);

            while (CurrentToken() == ",")
            {
                _pos++; // пропускаем запятую
                Expression next = ParseExpr();
                if (IsError(next)) return next;
                items.Add(next);
            }

            if (CurrentToken() != ")")
                return new ErrorExpr("Ожидалась закрывающая скобка ')'");
            _pos++; // пропускаем ")"

            if (items.Count == 1)
                return items[0];
            else
                return new TupleExpr(items);
        }

        /// <summary>
        /// Разбирает вызов функции. Имя функции уже прочитано.
        /// Ожидается, что аргументы разделены запятыми.
        /// </summary>
        private static Expression ParseFunctionCall(string functionName)
        {
            if (CurrentToken() != "(")
                return new ErrorExpr("Ожидалась открывающая скобка '(' после имени функции");
            _pos++; // пропускаем "("

            List<Expression> args = new List<Expression>();
            if (CurrentToken() == ")")
            {
                _pos++;
                return new FunctionCallExpr(functionName, args);
            }

            while (true)
            {
                Expression arg = ParseExpr();
                if (IsError(arg))
                    return arg;
                args.Add(arg);

                if (CurrentToken() == ",")
                {
                    _pos++; // пропускаем запятую
                    continue;
                }
                else if (CurrentToken() == ")")
                {
                    _pos++; // пропускаем ")"
                    break;
                }
                else
                {
                    return new ErrorExpr("Ожидалась запятая или закрывающая скобка ')' в вызове функции");
                }
            }
            return new FunctionCallExpr(functionName, args);
        }

        private static string CurrentToken() => _pos < _tokens.Count ? _tokens[_pos] : null;

        private static bool Match(string s)
        {
            if (_pos < _tokens.Count && _tokens[_pos] == s)
            {
                _pos++;
                return true;
            }
            return false;
        }

        private static bool IsError(Expression expr) => expr is ErrorExpr;

        /// <summary>
        /// CleanExpression удаляет завершающие запятые и лишние закрывающие скобки,
        /// не удаляя корректно обрамлённые внешние скобки.
        /// </summary>
        public static string CleanExpression(string expr)
        {
            expr = expr.Trim();
            while (expr.EndsWith(","))
                expr = expr.Substring(0, expr.Length - 1).Trim();
            while (CountChar(expr, ')') > CountChar(expr, '(') && expr.EndsWith(")"))
                expr = expr.Substring(0, expr.Length - 1).Trim();
            return expr;
        }

        private static int CountChar(string s, char c)
        {
            int count = 0;
            foreach (char ch in s)
                if (ch == c)
                    count++;
            return count;
        }
    }
}
