using System.Globalization;

namespace ParserRulesGenerator
{
    public abstract class Expression { }

    public class BinaryExpr : Expression
    {
        public string Op;        // "+", "-", "*", "/"
        public Expression Left;
        public Expression Right;

        public BinaryExpr(string op, Expression left, Expression right)
        {
            Op = op;
            Left = left;
            Right = right;
        }
    }

    public class NumberExpr : Expression
    {
        public double Value;
        public NumberExpr(double value) => Value = value;
    }

    public static class ExpressionParser
    {
        private static List<string> _tokens;
        private static int _pos;

        public static Expression Parse(string input)
        {
            _tokens = Tokenize(input);
            _pos = 0;
            return ParseExpr();
        }

        private static List<string> Tokenize(string str)
        {
            str = str.Replace("(", " ( ").Replace(")", " ) ")
                     .Replace("+", " + ").Replace("-", " - ")
                     .Replace("*", " * ").Replace("/", " / ");
            return str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        // expr ::= term (("+"|"-") term)*
        private static Expression ParseExpr()
        {
            Expression left = ParseTerm();
            while (Match("+") || Match("-"))
            {
                string op = _tokens[_pos - 1];
                Expression right = ParseTerm();
                left = new BinaryExpr(op, left, right);
            }
            return left;
        }

        // term ::= factor (("*"|"/") factor)*
        private static Expression ParseTerm()
        {
            Expression left = ParseFactor();
            while (Match("*") || Match("/"))
            {
                string op = _tokens[_pos - 1];
                Expression right = ParseFactor();
                left = new BinaryExpr(op, left, right);
            }
            return left;
        }

        // factor ::= number | "(" expr ")"
        private static Expression ParseFactor()
        {
            string token = CurrentToken();
            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                _pos++;
                return new NumberExpr(val);
            }
            else if (token == "(")
            {
                _pos++; // пропускаем "("
                Expression node = ParseExpr();
                if (CurrentToken() != ")")
                    throw new Exception("Ожидалась закрывающая скобка ')'");
                _pos++;
                return node;
            }

            throw new Exception($"Неизвестный токен в выражении: {token}");
        }

        private static bool Match(string s)
        {
            if (_pos < _tokens.Count && _tokens[_pos] == s)
            {
                _pos++;
                return true;
            }
            return false;
        }

        private static string CurrentToken()
        {
            if (_pos >= _tokens.Count) return null;
            return _tokens[_pos];
        }
    }
}
