using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ParserRulesGenerator
{
    // Печатает Expression-дерево (включая ошибки и вызовы функций)
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

    // Бинарная операция (например, x + y, x * y, x ^ y)
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

    // Числовое значение
    public class NumberExpr : Expression
    {
        public double Value;
        public NumberExpr(double value) => Value = value;
    }

    // Переменная (например, a, x)
    public class VariableExpr : Expression
    {
        public string Name;
        public VariableExpr(string name) => Name = name;
    }

    // Узел вызова функции
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

    // Узел-ошибка, позволяющий продолжить выполнение без выброса исключения.
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
        /// Точка входа в разбор строки как арифметического выражения.
        /// Если после разбора остаются лишние токены, возвращается ErrorExpr.
        /// </summary>
        public static Expression ParseExpressionNode(string input)
        {
            return Parse(input);
        }

        public static Expression Parse(string input)
        {
            _tokens = Tokenize(input);
            _pos = 0;

            Expression expr = ParseExpr();

            // Если после разбора остались токены, это ошибка
            if (_pos < _tokens.Count)
            {
                return new ErrorExpr($"Лишние токены после выражения: {_tokens[_pos]}");
            }

            if (expr == null)
                return new ErrorExpr("Парсер вернул null");

            return expr;
        }

        // Токенизирует строку, выделяя операторы, скобки, числа, идентификаторы и символ "^"
        private static List<string> Tokenize(string str)
        {
            str = str.Replace("^", " ^ ")
                     .Replace("(", " ( ").Replace(")", " ) ")
                     .Replace("+", " + ").Replace("-", " - ")
                     .Replace("*", " * ").Replace("/", " / ")
                     .Replace(",", " , ");
            return str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        // expr ::= Term { ("+" | "-") Term }*
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

        // term ::= Factor { ("*" | "/") Factor }*
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
        // Реализовано с правой ассоциативностью: a ^ b ^ c = a ^ (b ^ c)
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

        // Primary ::= number | variable | functionCall | "(" Expression ")"
        private static Expression ParsePrimary()
        {
            string token = CurrentToken();
            if (token == null)
            {
                return new ErrorExpr("Ожидалось число, переменная, функция или '(' — но строка закончилась");
            }

            // Если токен – число
            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                _pos++;
                return new NumberExpr(val);
            }
            // Если токен – идентификатор: может быть переменной или вызовом функции
            else if (Regex.IsMatch(token, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                _pos++;
                // Если после идентификатора сразу идёт "(", это вызов функции
                if (CurrentToken() == "(")
                {
                    return ParseFunctionCall(token);
                }
                else
                {
                    return new VariableExpr(token);
                }
            }
            // Если токен – открывающая скобка
            else if (token == "(")
            {
                _pos++; // пропускаем "("
                Expression expr = ParseExpr();
                if (IsError(expr)) return expr;
                if (CurrentToken() != ")")
                {
                    return new ErrorExpr("Ожидалась закрывающая скобка ')'");
                }
                _pos++; // пропускаем ")"
                return expr;
            }
            else
            {
                return new ErrorExpr($"Неизвестный токен в выражении: {token}");
            }
        }

        // Разбирает вызов функции; имя функции уже прочитано (functionName)
        // Ожидается список аргументов, разделённых запятыми, внутри круглых скобок.
        private static Expression ParseFunctionCall(string functionName)
        {
            // Текущий токен – "("; пропускаем его
            if (CurrentToken() != "(")
            {
                return new ErrorExpr("Ожидалась открывающая скобка '(' после имени функции");
            }
            _pos++; // пропускаем "("

            List<Expression> args = new List<Expression>();

            // Если список аргументов не пуст (следующий токен не ")")
            if (CurrentToken() != ")")
            {
                while (true)
                {
                    Expression arg = ParseExpr();
                    if (IsError(arg))
                        return arg;
                    args.Add(arg);

                    // Если текущий токен – запятая, пропускаем её и продолжаем разбор следующего аргумента
                    if (CurrentToken() == ",")
                    {
                        _pos++; // пропускаем запятую
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // После аргументов должен идти ")" 
            if (CurrentToken() != ")")
            {
                return new ErrorExpr("Ожидалась закрывающая скобка ')' в вызове функции");
            }
            _pos++; // пропускаем ")"

            return new FunctionCallExpr(functionName, args);
        }

        // Вспомогательная функция: проверяет, является ли узел ошибкой
        private static bool IsError(Expression expr) => expr is ErrorExpr;

        // Если текущий токен равен s, сдвигаем позицию и возвращаем true.
        private static bool Match(string s)
        {
            if (_pos < _tokens.Count && _tokens[_pos] == s)
            {
                _pos++;
                return true;
            }
            return false;
        }

        // Возвращает текущий токен или null, если токены закончились.
        private static string CurrentToken()
        {
            if (_pos >= _tokens.Count) return null;
            return _tokens[_pos];
        }
    }
}
