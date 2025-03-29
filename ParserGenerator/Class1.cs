using System.Globalization;
using System.Text.RegularExpressions;

public class VarName
{
    public string Value { get; set; }

    public bool IsMatch(string input)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(input, "[a-zA-Z_][a-zA-Z0-9_]*");
    }
}

public class IntValue
{
    public int Value { get; set; }

    public bool IsMatch(string input)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(input, "[0-9]+");
    }
}

public class FloatValue
{
    public double Value { get; set; }

    public bool IsMatch(string input)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(input, "[0-9]+\\.[0-9]+");
    }
}

public class BoolValue
{
    public bool Value { get; set; }

    public bool IsMatch(string input)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(input, "(true|false)");
    }
}

public class MathOperator
{
    public string Value { get; set; }

    public bool IsMatch(string input)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(input, "(\\+|\\-|\\*|\\/)");
    }
}

public class ComparisonOperator
{
    public string Value { get; set; }

    public bool IsMatch(string input)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(input, "(==|!=|<|>|<=|>=)");
    }
}

public class LogicalOperator
{
    public string Value { get; set; }

    public bool IsMatch(string input)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(input, "(&&|\\|\\|)");
    }
}

public class VariableDeclaration
{
    public VarName VarName1 { get; }
    public IntValue IntValue1 { get; }

    public VariableDeclaration(VarName varName1, IntValue intValue1)
    {
        this.VarName1 = varName1; ;
        this.IntValue1 = intValue1; ;
    }
}

public class FloatAssignment
{
    public VarName VarName1 { get; }
    public FloatValue FloatValue1 { get; }

    public FloatAssignment(VarName varName1, FloatValue floatValue1)
    {
        this.VarName1 = varName1; ;
        this.FloatValue1 = floatValue1; ;
    }
}

public class BooleanCheck
{
    public VarName VarName1 { get; }
    public ComparisonOperator ComparisonOperator1 { get; }
    public IntValue IntValue1 { get; }
    public VarName VarName2 { get; }
    public BoolValue BoolValue1 { get; }

    public BooleanCheck(VarName varName1, ComparisonOperator comparisonOperator1, IntValue intValue1, VarName varName2, BoolValue boolValue1)
    {
        this.VarName1 = varName1; ;
        this.ComparisonOperator1 = comparisonOperator1; ;
        this.IntValue1 = intValue1; ;
        this.VarName2 = varName2; ;
        this.BoolValue1 = boolValue1; ;
    }
}

public class WhileLoop
{
    public VarName VarName1 { get; }
    public ComparisonOperator ComparisonOperator1 { get; }
    public IntValue IntValue1 { get; }
    public VarName VarName2 { get; }
    public VarName VarName3 { get; }
    public MathOperator MathOperator1 { get; }
    public IntValue IntValue2 { get; }

    public WhileLoop(VarName varName1, ComparisonOperator comparisonOperator1, IntValue intValue1, VarName varName2, VarName varName3, MathOperator mathOperator1, IntValue intValue2)
    {
        this.VarName1 = varName1; ;
        this.ComparisonOperator1 = comparisonOperator1; ;
        this.IntValue1 = intValue1; ;
        this.VarName2 = varName2; ;
        this.VarName3 = varName3; ;
        this.MathOperator1 = mathOperator1; ;
        this.IntValue2 = intValue2; ;
    }
}

public class FunctionCall
{
    public VarName VarName1 { get; }
    public VarName VarName2 { get; }
    public FloatValue FloatValue1 { get; }

    public FunctionCall(VarName varName1, VarName varName2, FloatValue floatValue1)
    {
        this.VarName1 = varName1; ;
        this.VarName2 = varName2; ;
        this.FloatValue1 = floatValue1; ;
    }
}

public class ComplexCondition
{
    public VarName VarName1 { get; }
    public ComparisonOperator ComparisonOperator1 { get; }
    public VarName VarName2 { get; }
    public LogicalOperator LogicalOperator1 { get; }
    public VarName VarName3 { get; }
    public ComparisonOperator ComparisonOperator2 { get; }
    public VarName VarName4 { get; }
    public VarName VarName5 { get; }
    public BoolValue BoolValue1 { get; }

    public ComplexCondition(VarName varName1, ComparisonOperator comparisonOperator1, VarName varName2, LogicalOperator logicalOperator1, VarName varName3, ComparisonOperator comparisonOperator2, VarName varName4, VarName varName5, BoolValue boolValue1)
    {
        this.VarName1 = varName1; ;
        this.ComparisonOperator1 = comparisonOperator1; ;
        this.VarName2 = varName2; ;
        this.LogicalOperator1 = logicalOperator1; ;
        this.VarName3 = varName3; ;
        this.ComparisonOperator2 = comparisonOperator2; ;
        this.VarName4 = varName4; ;
        this.VarName5 = varName5; ;
        this.BoolValue1 = boolValue1; ;
    }
}

public class Parser
{
    private static readonly Regex variableDeclarationPattern = new(@"^\s*let\ ([a-zA-Z_][a-zA-Z0-9_]*)\ =\ ([0-9]+)\ ;\s*$", RegexOptions.Compiled);
    private static readonly Regex floatAssignmentPattern = new(@"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\ =\ ([0-9]+\.[0-9]+)\ ;\s*$", RegexOptions.Compiled);
    private static readonly Regex booleanCheckPattern = new(@"^\s*if\ \(\ ([a-zA-Z_][a-zA-Z0-9_]*)\ (==|!=|<|>|<=|>=)\ ([0-9]+)\ \)\ \{\ ([a-zA-Z_][a-zA-Z0-9_]*)\ =\ (true|false)\ ;\ }\s*$", RegexOptions.Compiled);
    private static readonly Regex whileLoopPattern = new(@"^\s*while\ \(\ ([a-zA-Z_][a-zA-Z0-9_]*)\ (==|!=|<|>|<=|>=)\ ([0-9]+)\ \)\ \{\ ([a-zA-Z_][a-zA-Z0-9_]*)\ =\ ([a-zA-Z_][a-zA-Z0-9_]*)\ (\+|\-|\*|\/)\ ([0-9]+)\ ;\ }\s*$", RegexOptions.Compiled);
    private static readonly Regex functionCallPattern = new(@"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\ \(\ ([a-zA-Z_][a-zA-Z0-9_]*)\ ,\ ([0-9]+\.[0-9]+)\ \)\ ;\s*$", RegexOptions.Compiled);
    private static readonly Regex complexConditionPattern = new(@"^\s*if\ \(\ ([a-zA-Z_][a-zA-Z0-9_]*)\ (==|!=|<|>|<=|>=)\ ([a-zA-Z_][a-zA-Z0-9_]*)\ (&&|\|\|)\ ([a-zA-Z_][a-zA-Z0-9_]*)\ (==|!=|<|>|<=|>=)\ ([a-zA-Z_][a-zA-Z0-9_]*)\ \)\ \{\ ([a-zA-Z_][a-zA-Z0-9_]*)\ =\ (true|false)\ ;\ }\s*$", RegexOptions.Compiled);

    public object Parse(string input)
    {
        if (variableDeclarationPattern.IsMatch(input))
        {
            var match = variableDeclarationPattern.Match(input);
            var raw1 = match.Groups[1].Value;
            var raw2 = match.Groups[2].Value;
            var slot1 = new VarName { Value = raw1 };
            var slot2 = new IntValue { Value = int.Parse(raw2) };
            return new VariableDeclaration(slot1, slot2);
        }

        if (floatAssignmentPattern.IsMatch(input))
        {
            var match = floatAssignmentPattern.Match(input);
            var raw1 = match.Groups[1].Value;
            var raw2 = match.Groups[2].Value;
            var slot1 = new VarName { Value = raw1 };
            var slot2 = new FloatValue { Value = double.Parse(raw2, CultureInfo.InvariantCulture) };
            return new FloatAssignment(slot1, slot2);
        }

        if (booleanCheckPattern.IsMatch(input))
        {
            var match = booleanCheckPattern.Match(input);
            var raw1 = match.Groups[1].Value;
            var raw2 = match.Groups[2].Value;
            var raw3 = match.Groups[3].Value;
            var raw4 = match.Groups[4].Value;
            var raw5 = match.Groups[5].Value;
            var slot1 = new VarName { Value = raw1 };
            var slot2 = new ComparisonOperator { Value = raw2 };
            var slot3 = new IntValue { Value = int.Parse(raw3) };
            var slot4 = new VarName { Value = raw4 };
            var slot5 = new BoolValue { Value = bool.Parse(raw5) };
            return new BooleanCheck(slot1, slot2, slot3, slot4, slot5);
        }

        if (whileLoopPattern.IsMatch(input))
        {
            var match = whileLoopPattern.Match(input);
            var raw1 = match.Groups[1].Value;
            var raw2 = match.Groups[2].Value;
            var raw3 = match.Groups[3].Value;
            var raw4 = match.Groups[4].Value;
            var raw5 = match.Groups[5].Value;
            var raw6 = match.Groups[6].Value;
            var raw7 = match.Groups[7].Value;
            var slot1 = new VarName { Value = raw1 };
            var slot2 = new ComparisonOperator { Value = raw2 };
            var slot3 = new IntValue { Value = int.Parse(raw3) };
            var slot4 = new VarName { Value = raw4 };
            var slot5 = new VarName { Value = raw5 };
            var slot6 = new MathOperator { Value = raw6 };
            var slot7 = new IntValue { Value = int.Parse(raw7) };
            return new WhileLoop(slot1, slot2, slot3, slot4, slot5, slot6, slot7);
        }

        if (functionCallPattern.IsMatch(input))
        {
            var match = functionCallPattern.Match(input);
            var raw1 = match.Groups[1].Value;
            var raw2 = match.Groups[2].Value;
            var raw3 = match.Groups[3].Value;
            var slot1 = new VarName { Value = raw1 };
            var slot2 = new VarName { Value = raw2 };
            var slot3 = new FloatValue { Value = double.Parse(raw3, CultureInfo.InvariantCulture) };
            return new FunctionCall(slot1, slot2, slot3);
        }

        if (complexConditionPattern.IsMatch(input))
        {
            var match = complexConditionPattern.Match(input);
            var raw1 = match.Groups[1].Value;
            var raw2 = match.Groups[2].Value;
            var raw3 = match.Groups[3].Value;
            var raw4 = match.Groups[4].Value;
            var raw5 = match.Groups[5].Value;
            var raw6 = match.Groups[6].Value;
            var raw7 = match.Groups[7].Value;
            var raw8 = match.Groups[8].Value;
            var raw9 = match.Groups[9].Value;
            var slot1 = new VarName { Value = raw1 };
            var slot2 = new ComparisonOperator { Value = raw2 };
            var slot3 = new VarName { Value = raw3 };
            var slot4 = new LogicalOperator { Value = raw4 };
            var slot5 = new VarName { Value = raw5 };
            var slot6 = new ComparisonOperator { Value = raw6 };
            var slot7 = new VarName { Value = raw7 };
            var slot8 = new VarName { Value = raw8 };
            var slot9 = new BoolValue { Value = bool.Parse(raw9) };
            return new ComplexCondition(slot1, slot2, slot3, slot4, slot5, slot6, slot7, slot8, slot9);
        }

        throw new ArgumentException($"Ни один шаблон не подошел для строки: {input}");
    }
}
