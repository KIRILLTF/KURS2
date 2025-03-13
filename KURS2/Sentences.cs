abstract class Sentence
{
    public List<string> Name;
    public List<string> Variables { get; set; }
}

class ModuleSentence : Sentence
{

    public ModuleSentence(List<string> name, List<string> variables = null)
    {
        Name = name;
        Variables = variables;
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
    }
}