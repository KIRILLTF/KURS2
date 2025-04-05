using System.Text.RegularExpressions;

namespace ParserRulesGenerator
{
    public class VarName
    {
        public string Value { get; set; }

        public bool IsMatch(string input)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(input, @"[a-zA-Z_][a-zA-Z0-9_]*");
        }
    }

    public class IntValue
    {
        public int Value { get; set; }

        public bool IsMatch(string input)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(input, @"[0-9]+");
        }
    }

    public class BoolValue
    {
        public bool Value { get; set; }

        public bool IsMatch(string input)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(input, @"(true|false)");
        }
    }

    public class ComparisonOp
    {
        public string Value { get; set; }

        public bool IsMatch(string input)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(input, @"(==|!=|>|>=|<|<=)");
        }
    }

    public class PrintExpr
    {
        public Expression Expression1 { get; }

        public PrintExpr(Expression expression1)
        {
            this.Expression1 = expression1; ;
        }
    }

    public class AssignExpr
    {
        public VarName VarName1 { get; }
        public Expression Expression1 { get; }

        public AssignExpr(VarName varName1, Expression expression1)
        {
            this.VarName1 = varName1; ;
            this.Expression1 = expression1; ;
        }
    }

    public class WhileLoop
    {
        public VarName VarName1 { get; }
        public ComparisonOp ComparisonOp1 { get; }
        public IntValue IntValue1 { get; }
        public Expression Expression1 { get; }

        public WhileLoop(VarName varName1, ComparisonOp comparisonOp1, IntValue intValue1, Expression expression1)
        {
            this.VarName1 = varName1; ;
            this.ComparisonOp1 = comparisonOp1; ;
            this.IntValue1 = intValue1; ;
            this.Expression1 = expression1; ;
        }
    }

    public class BooleanAssign
    {
        public VarName VarName1 { get; }
        public BoolValue BoolValue1 { get; }

        public BooleanAssign(VarName varName1, BoolValue boolValue1)
        {
            this.VarName1 = varName1; ;
            this.BoolValue1 = boolValue1; ;
        }
    }

    public class Parser
    {
        private static readonly Regex printExprPattern = new(@"^\s*print\ (.+?)\ ;\s*$", RegexOptions.Compiled);
        private static readonly Regex assignExprPattern = new(@"^\s*let\ ([a-zA-Z_][a-zA-Z0-9_]*)\ =\ (.+?)\ ;\s*$", RegexOptions.Compiled);
        private static readonly Regex whileLoopPattern = new(@"^\s*while\ \(\ ([a-zA-Z_][a-zA-Z0-9_]*)\ (==|!=|>|>=|<|<=)\ ([0-9]+)\ \)\ do\ (.+?)\ endWhile\s*$", RegexOptions.Compiled);
        private static readonly Regex booleanAssignPattern = new(@"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\ :=\ (true|false)\ ;\s*$", RegexOptions.Compiled);
        private static readonly Regex missingVariablePattern = new(@"^\s*let\ =\ ([0-9]+)\ ;\s*$", RegexOptions.Compiled);
        private static readonly Regex invalidBoolAssignmentPattern = new(@"^\s*=\ (true|false)\ ;\s*$", RegexOptions.Compiled);

        public object Parse(string input)
        {

            // Error rule: MissingVariable
            if (missingVariablePattern.IsMatch(input))
            {
                throw new Exception("Пользовательская ошибка: MissingVariable");
            }

            // Error rule: InvalidBoolAssignment
            if (invalidBoolAssignmentPattern.IsMatch(input))
            {
                throw new Exception("Пользовательская ошибка: InvalidBoolAssignment");
            }
            if (printExprPattern.IsMatch(input))
            {
                var match = printExprPattern.Match(input);
                var raw1 = match.Groups[1].Value;
                var slot1 = ParserRulesGenerator.ExpressionParser.Parse(raw1);
                return new PrintExpr(slot1);
            }

            if (assignExprPattern.IsMatch(input))
            {
                var match = assignExprPattern.Match(input);
                var raw1 = match.Groups[1].Value;
                var raw2 = match.Groups[2].Value;
                var slot1 = new VarName { Value = raw1 };
                var slot2 = ParserRulesGenerator.ExpressionParser.Parse(raw2);
                return new AssignExpr(slot1, slot2);
            }

            if (whileLoopPattern.IsMatch(input))
            {
                var match = whileLoopPattern.Match(input);
                var raw1 = match.Groups[1].Value;
                var raw2 = match.Groups[2].Value;
                var raw3 = match.Groups[3].Value;
                var raw4 = match.Groups[4].Value;
                var slot1 = new VarName { Value = raw1 };
                var slot2 = new ComparisonOp { Value = raw2 };
                var slot3 = new IntValue { Value = int.Parse(raw3) };
                var slot4 = ParserRulesGenerator.ExpressionParser.Parse(raw4);
                return new WhileLoop(slot1, slot2, slot3, slot4);
            }

            if (booleanAssignPattern.IsMatch(input))
            {
                var match = booleanAssignPattern.Match(input);
                var raw1 = match.Groups[1].Value;
                var raw2 = match.Groups[2].Value;
                var slot1 = new VarName { Value = raw1 };
                var slot2 = new BoolValue { Value = bool.Parse(raw2) };
                return new BooleanAssign(slot1, slot2);
            }

            throw new ArgumentException($"Ни один шаблон не подошел для строки: {input}");
        }
    }
}