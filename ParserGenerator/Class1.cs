using System.Text.RegularExpressions;

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

public class BooleanAssignment
{
    public VarName VarName1 { get; }
    public BoolValue BoolValue1 { get; }

    public BooleanAssignment(VarName varName1, BoolValue boolValue1)
    {
        this.VarName1 = varName1; ;
        this.BoolValue1 = boolValue1; ;
    }
}

public class Parser
{
    private static readonly Regex variableDeclarationPattern = new(@"^\s*let\ ([a-zA-Z_][a-zA-Z0-9_]*)\ =\ ([0-9]+)\ ;\s*$", RegexOptions.Compiled);
    private static readonly Regex booleanAssignmentPattern = new(@"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\ =\ (true|false)\ ;\s*$", RegexOptions.Compiled);
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
        if (variableDeclarationPattern.IsMatch(input))
        {
            var match = variableDeclarationPattern.Match(input);
            var raw1 = match.Groups[1].Value;
            var raw2 = match.Groups[2].Value;
            var slot1 = new VarName { Value = raw1 };
            var slot2 = new IntValue { Value = int.Parse(raw2) };
            return new VariableDeclaration(slot1, slot2);
        }

        if (booleanAssignmentPattern.IsMatch(input))
        {
            var match = booleanAssignmentPattern.Match(input);
            var raw1 = match.Groups[1].Value;
            var raw2 = match.Groups[2].Value;
            var slot1 = new VarName { Value = raw1 };
            var slot2 = new BoolValue { Value = bool.Parse(raw2) };
            return new BooleanAssignment(slot1, slot2);
        }

        throw new ArgumentException($"Ни один шаблон не подошел для строки: {input}");
    }
}
