abstract class Sentence
{
    public List<string> Name;
    public List<string> Variables { get; set; }
    public List<string> Operators { get; set; }
    public List<string> Functions { get; set; }
}

class ModuleSentence : Sentence
{
    public ModuleSentence(List<string> name, List<string> variables = null)
    {
        Name = name;
        Variables = variables;
        Operators = new List<string>();
        Functions = new List<string>();
    }
}

class ImportSentence : Sentence
{
    public string Alias { get; }

    public ImportSentence(List<string> name, List<string> variables = null, string alias = null)
    {
        Name = name;
        Variables = variables;
        Alias = alias;

        Operators = new List<string>();
        Functions = new List<string>();
    }
}

class LetSentence : Sentence
{
    public string Expression { get; }
    public bool HasWhere { get; }

    public LetSentence(List<string> name, List<string> variables, string expression, bool hasWhere)
    {
        Name = name;
        Variables = variables;
        Expression = expression;
        HasWhere = hasWhere;

        Operators = new List<string>();
        Functions = new List<string>();
    }
}
