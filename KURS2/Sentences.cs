using System;
using System.Collections.Generic;

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

/// <summary>
/// Предложение let: теперь хранит и строку выражения, и синтактическое дерево
/// </summary>
class LetSentence : Sentence
{
    public string ExpressionString { get; }
    public bool HasWhere { get; }

    /// <summary>
    /// Синтактическое дерево (AST) для выражения
    /// </summary>
    public ExpressionNode ExpressionAst { get; }

    public LetSentence(
        List<string> name,
        List<string> variables,
        string expressionString,
        bool hasWhere,
        ExpressionNode expressionAst)
    {
        Name = name;
        Variables = variables;
        ExpressionString = expressionString;
        HasWhere = hasWhere;
        ExpressionAst = expressionAst;
    }
}
