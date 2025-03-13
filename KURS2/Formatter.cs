class Formatter
{
    public List<Sentence> Format(List<Sentence> sentences)
    {
        var mergedSentences = MergeVariables(sentences);
        var sortedSentences = SortSentences(mergedSentences);

        return sortedSentences;
    }

    public List<Sentence> MergeVariables(List<Sentence> sentences)
    {
        for (int i = 0; i < sentences.Count; i++)
        {
            for (int j = i + 1; j < sentences.Count; j++)
            {
                if (sentences[i].Name.SequenceEqual(sentences[j].Name))
                {
                    sentences[i].Variables.AddRange(sentences[j].Variables);
                    sentences.RemoveAt(j);
                    j--;
                }
            }
        }

        return sentences;
    }

    public List<Sentence> SortSentences(List<Sentence> sentences)
    {
        var result = new List<Sentence>();
        var block = new List<Sentence>();

        foreach (var sentence in sentences)
        {
            if (sentence.Variables != null)
                sentence.Variables = sentence.Variables.OrderBy(s => s).ToList();

            if (sentence is ModuleSentence || (sentence is LetSentence let && let.HasWhere))
            {
                result.AddRange(SortImports(block));
                block.Clear();
                result.Add(sentence);
            }
            else
            {
                block.Add(sentence);
            }
        }
        result.AddRange(SortImports(block));

        return result;
    }

    private IEnumerable<Sentence> SortImports(List<Sentence> sentences)
    {
        var imports = sentences.OfType<ImportSentence>().OrderBy(x => string.Join(".", x.Name));
        var others = sentences.Except(sentences.OfType<ImportSentence>());

        return imports.Concat(others);
    }
}