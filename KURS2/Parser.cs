using System.Text.RegularExpressions;

class Parser
{
    private static readonly Regex modulePattern = new(
        @"^\s*module\s+([a-zA-Z0-9_.]+(?:\.[a-zA-Z0-9_]+)*)(?:\s*\(([^)]*)\))?\s+where$",
        RegexOptions.Compiled
    );

    private static readonly Regex importPattern = new(
        @"^\s*import\s+([a-zA-Z0-9_.]+(?:\.[a-zA-Z0-9_]+)*)(?:\s*\(([^)]*)\))?(?:\s+as\s+([a-zA-Z0-9_.]+))?$",
        RegexOptions.Compiled
    );

    private static readonly Regex letPattern = new(
        @"^\s*let\s+([a-zA-Z0-9_.]+)(?:\s*\(([^)]*)\))?\s*:=\s*(.+?)(?:\s+where)?$",
        RegexOptions.Compiled
    );

    public Sentence Parse(string input)
    {
        if (modulePattern.IsMatch(input))
        {
            var match = modulePattern.Match(input);
            var name = SplitName(match.Groups[1].Value);
            var variables = ExtractVariables(match.Groups[2].Value);

            return new ModuleSentence(name, variables);
        }
        if (importPattern.IsMatch(input))
        {
            var match = importPattern.Match(input);
            var name = SplitName(match.Groups[1].Value);
            var variables = ExtractVariables(match.Groups[2].Value);
            var alias = match.Groups[3].Success ? match.Groups[3].Value : null;

            return new ImportSentence(name, variables, alias);
        }
        if (letPattern.IsMatch(input))
        {
            var match = letPattern.Match(input);
            var name = SplitName(match.Groups[1].Value);
            var variables = ExtractVariables(match.Groups[2].Value);
            var parameters = match.Groups[2].Value;
            var expression = match.Groups[3].Value;
            var hasWhere = input.EndsWith(" where");

            return new LetSentence(name, variables, parameters, expression, hasWhere);
        }

        throw new ArgumentException($"Ошибка: Некорректная строка — {input}");
    }

    // Разбивает имя на части по точкам
    private List<string> SplitName(string name)
    {
        return name.Split('.').ToList();
    }

    // Извлекает переменные из скобок
    private List<string> ExtractVariables(string variables)
    {
        if (string.IsNullOrEmpty(variables))
            return null;

        return variables.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    // Метод для удаления ненужных символов
    public string StringChanger(string text)
    {
        List<string> lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

        for (int i = 0; i < lines.Count; i++)
        {
            // Оставляем только "одинарные" пробелы
            lines[i] = Regex.Replace(lines[i].Trim(), @"\s+", " ");

            // Заменяем "( " на "("
            lines[i] = Regex.Replace(lines[i], @"\(\s", "(");

            // Добавляем пробел после запятой
            lines[i] = Regex.Replace(lines[i], @",(?=\S)", ", ");

            // Заменяем " )" на ")"
            lines[i] = Regex.Replace(lines[i], @"\s\)", ")");

            // Заменяем " , " на ", "
            lines[i] = Regex.Replace(lines[i], @"\s,\s", ", ");
        }

        return string.Join(Environment.NewLine, lines);
    }
}