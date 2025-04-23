using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

class Tokenizer
{
    private static readonly Regex patterns = new Regex(
        @"^\s*(?:" +
          @"module\s+[A-Za-z0-9_.]+(?:\.[A-Za-z0-9_]+)*(?:\s*\([^)]*\))?\s+where" +
        @"|import\s+[A-Za-z0-9_.]+(?:\.[A-Za-z0-9_]+)*(?:\s*\(\s*[A-Za-z0-9_]+(?:\s*,\s*[A-Za-z0-9_]+)*\s*\))?" +
        @"|let\s+[A-Za-z0-9_.]+(?:\s*\([^)]*\))?\s*:=\s*.+?(?:\s+where)?" +
        @")\s*$",
        RegexOptions.Compiled
    );

    private static readonly Regex tokenPattern = new Regex(
        @"(module|import|let|where|as|:=|\w+|[(),+*/=-])",
        RegexOptions.Compiled
    );

    public List<string> Tokenize(string input)
    {
        if (!patterns.IsMatch(input))
            throw new ArgumentException($"Ошибка: Некорректная строка — {input}");

        var tokens = new List<string>();
        foreach (Match match in tokenPattern.Matches(input))
            tokens.Add(match.Value);

        return tokens;
    }
}
