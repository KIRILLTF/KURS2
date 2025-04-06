// -----------------------------------------------------
//  Классы для AST
// -----------------------------------------------------

/// <summary>
/// Абстрактный узел выражения
/// </summary>
abstract class ExpressionNode
{
}

/// <summary>
/// Узел для бинарных операций: +, -, *, /, ^
/// </summary>
class BinaryExpressionNode : ExpressionNode
{
    public ExpressionNode Left { get; }
    public ExpressionNode Right { get; }
    public string Operator { get; }

    public BinaryExpressionNode(ExpressionNode left, string op, ExpressionNode right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
}

/// <summary>
/// Узел для унарных операций: +, -
/// </summary>
class UnaryExpressionNode : ExpressionNode
{
    public string Operator { get; }
    public ExpressionNode Operand { get; }

    public UnaryExpressionNode(string op, ExpressionNode operand)
    {
        Operator = op;
        Operand = operand;
    }
}

/// <summary>
/// Узел для числового литерала
/// </summary>
class NumberNode : ExpressionNode
{
    public double Value { get; }

    public NumberNode(double value)
    {
        Value = value;
    }
}

/// <summary>
/// Узел для переменной (идентификатора)
/// </summary>
class VariableNode : ExpressionNode
{
    public string Name { get; }

    public VariableNode(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Узел для вызова функции: f(a, b, c)
/// </summary>
class FunctionCallNode : ExpressionNode
{
    public string FunctionName { get; }
    public List<ExpressionNode> Arguments { get; }

    public FunctionCallNode(string functionName, List<ExpressionNode> args)
    {
        FunctionName = functionName;
        Arguments = args;
    }
}

// -----------------------------------------------------
//  Лексер
// -----------------------------------------------------
enum TokenType
{
    Number,
    Identifier,
    Plus,
    Minus,
    Multiply,
    Divide,
    Power,
    LParen,
    RParen,
    Comma,   // <-- добавили запятую
    End
}

class Token
{
    public TokenType Type { get; }
    public string Value { get; }

    public Token(TokenType type, string value = null)
    {
        Type = type;
        Value = value;
    }
}

class Lexer
{
    private readonly string _text;
    private int _pos;

    public Lexer(string text)
    {
        _text = text;
        _pos = 0;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (_pos < _text.Length)
        {
            char current = _text[_pos];

            if (char.IsWhiteSpace(current))
            {
                _pos++;
                continue;
            }

            // Число (включая десятичную точку)
            if (char.IsDigit(current))
            {
                tokens.Add(ReadNumber());
                continue;
            }

            // Идентификатор (разрешаем буквы, цифры, '_', а также '.')
            if (char.IsLetter(current) || current == '_' || current == '.')
            {
                tokens.Add(ReadIdentifier());
                continue;
            }

            switch (current)
            {
                case '+':
                    tokens.Add(new Token(TokenType.Plus, "+"));
                    _pos++;
                    break;
                case '-':
                    tokens.Add(new Token(TokenType.Minus, "-"));
                    _pos++;
                    break;
                case '*':
                    tokens.Add(new Token(TokenType.Multiply, "*"));
                    _pos++;
                    break;
                case '/':
                    tokens.Add(new Token(TokenType.Divide, "/"));
                    _pos++;
                    break;
                case '^':
                    tokens.Add(new Token(TokenType.Power, "^"));
                    _pos++;
                    break;
                case '(':
                    tokens.Add(new Token(TokenType.LParen, "("));
                    _pos++;
                    break;
                case ')':
                    tokens.Add(new Token(TokenType.RParen, ")"));
                    _pos++;
                    break;
                case ',':
                    tokens.Add(new Token(TokenType.Comma, ","));
                    _pos++;
                    break;
                default:
                    throw new ArgumentException($"Неизвестный символ: '{current}'");
            }
        }

        // В конце добавляем токен End
        tokens.Add(new Token(TokenType.End));
        return tokens;
    }

    private Token ReadNumber()
    {
        int startPos = _pos;
        bool dotEncountered = false;

        while (_pos < _text.Length)
        {
            char c = _text[_pos];
            if (char.IsDigit(c))
            {
                _pos++;
            }
            else if (c == '.' && !dotEncountered)
            {
                dotEncountered = true;
                _pos++;
            }
            else
            {
                break;
            }
        }

        string numberStr = _text.Substring(startPos, _pos - startPos);
        return new Token(TokenType.Number, numberStr);
    }

    /// <summary>
    /// Разрешаем буквы, цифры, '_', а также '.'
    /// чтобы парсить math.sqrt как один идентификатор.
    /// </summary>
    private Token ReadIdentifier()
    {
        int startPos = _pos;
        while (_pos < _text.Length)
        {
            char c = _text[_pos];
            if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
            {
                _pos++;
            }
            else
            {
                break;
            }
        }
        string ident = _text.Substring(startPos, _pos - startPos);
        return new Token(TokenType.Identifier, ident);
    }
}