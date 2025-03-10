using System.Text.RegularExpressions;

class Tokenizer
{
    private static readonly Regex patterns = new(
        @"^(\s*module\s+[a-zA-Z0-9_.]+(?:\.[a-zA-Z0-9_]+)*(?:\([^)]*\))?\s+where" +
        @"|\s*import\s+[a-zA-Z0-9_.]+(?:\.[a-zA-Z0-9_]+)*(?:\([^)]*\))?(\s+as\s+[a-zA-Z0-9_.]+)?" +
        @"|\s*let\s+([a-zA-Z0-9_.]+)(?:\(([^)]*)\))?\s*:=\s*(.+?)(?:\s+where)?)$",
        RegexOptions.Compiled
    );

    public List<string> Tokenize(string input)
    {
        if (!patterns.IsMatch(input))
            throw new ArgumentException($"Ошибка: Некорректная строка — {input}");

        var tokens = new List<string>();
        var tokenPattern = new Regex(@"(module|import|let|where|as|:=|\w+|[(),+*/-])", RegexOptions.Compiled);

        foreach (Match match in tokenPattern.Matches(input))
            tokens.Add(match.Value);

        return tokens;
    }
}