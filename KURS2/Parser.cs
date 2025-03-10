using System;
using System.Collections.Generic;
using System.Linq;
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

/*
// Компоратор, определяющий порядок импортов.
private class ModuleComparer : IComparer<string[]>
{
    public int Compare(string[] x, string[] y)
    {
        int leng = Math.Min(x.Length, y.Length);

        for (int i = 0; i < leng; i++)
        {
            int comp = string.Compare(x[i], y[i], StringComparison.Ordinal);

            if (comp != 0) return comp;
        }

        return x.Length.CompareTo(y.Length);
    }
}

// Метод, комбинирующий и сортирующий модули и переменные.
private List<string> ImportsFormatter(List<string> imports)
{
    var importDict = new Dictionary<string, SortedSet<string>>();

    foreach (string import in imports)
    {
        var match = Regex.Match(import, @"import\s+([\w\.]+)(?:\s+\(([^)]+)\))?");

        if (!match.Success) continue;

        string modules = match.Groups[1].Value;
        string variables = match.Groups[2].Success ? match.Groups[2].Value : null;

        if (!importDict.ContainsKey(modules)) importDict[modules] = new SortedSet<string>();

        if (variables != null)
        {
            foreach (var variable in variables.Split(',').Select(x => x.Trim()))
            {
                importDict[modules].Add(variable);
            }
        }
    }

    return importDict
        .OrderBy(mod => mod.Key.Split('.'), new ModuleComparer())
        .Select(mod => mod.Value.Count > 0
            ? $"import {mod.Key} ({string.Join(", ", mod.Value)})"
            : $"import {mod.Key}")
        .ToList();
}

// Метод, разделяющий строчку с несколькими импортами.
List<string> SplitImports(string input)
{
    var result = new List<string>();
    var matches = Regex.Matches(input, @"(?<=\bimport)\s+[\w\.]+(?:\s*\([^)]+\))?");

    foreach (Match match in matches)
    {
        result.Add("import " + match.Value.Trim());
    }

    return result;
}

// Метод, собирающий все строчки с импортами.
List<string> importsChecker(List<string> imports)
{
    List<string> imprts = new List<string>();

    foreach (string imp in imports)
    {
        if (Regex.Matches(imp, @"\bimport\b").Count > 1)
        {
            List<string> imps = SplitImports(imp);

            foreach (string im in imps) imprts.Add(im);
        }
        else
        {
            imprts.Add(imp);
        }
    }

    return imprts;
}

// Метод для форматирования текста.
public List<string> TextFormatter(List<string> usersText, bool indent)
{
    // Отбор строк, содержащих import.
    List<string> importLines = importsChecker(usersText.Where(l => Regex.IsMatch(l, @"\bimport\b")).ToList());

    // Обработка строк, не содержащих импорт.
    List<string> otherLines = usersText.Where(l => !Regex.IsMatch(l, @"\bimport\b")).ToList();

    // Обработкса и сортировка строчек с импортами.
    List<string> sortedImports = ImportsFormatter(importLines);

    // Итоговый блок.
    List<string> processed = new List<string>();
    processed.AddRange(sortedImports);
    processed.AddRange(otherLines);

    // Добавляем отступы.
    if (indent)
    {
        processed = processed.Select(l => "  " + l).ToList();
    }

    return processed;
}

// Метод для сборки текста.
public List<string> Output(string input)
{
    // Удаление пустых строк и пробелов по краям.
    List<string> lines = input
        .Split(new[] { "\n" }, StringSplitOptions.None).ToList()
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .Select(l => l.Trim())
        .ToList();

    if (!TextChecker(lines)) throw new Exception();

    List<string> result = new List<string>();

    // Индекс начала промежутка (один where).
    int blockStart = 0;
    int i = 0;

    while (i < lines.Count)
    {
        if (lines[i].Contains("where"))
        {
            List<string> block = lines.GetRange(blockStart, i - blockStart);
            List<string> processedBlock = TextFormatter(block, indent: false);
            result.AddRange(processedBlock);

            // Добавляем строку с where – обрезав лишние пробелы.
            result.Add(lines[i].Trim());
            i++; // переходим к следующей строке после where.

            // Собираем блок, принадлежащий данному where, до следующей строки, содержащей where,
            // или до конца файла.
            blockStart = i;

            while (i < lines.Count && !lines[i].Contains("where"))
            {
                i++;
            }

            List<string> whereBlock = lines.GetRange(blockStart, i - blockStart);
            // Передаем параметр indent:true – все строки блока получат табуляцию.
            List<string> processedWhereBlock = TextFormatter(whereBlock, indent: true);
            result.AddRange(processedWhereBlock);

            // Новый блок начинается с текущей позиции.
            blockStart = i;
        }
        else
        {
            i++;
        }
    }

    // Если после последнего where остались строки – обрабатываем их (без дополнительного отступа).
    if (blockStart < lines.Count)
    {
        List<string> block = lines.GetRange(blockStart, lines.Count - blockStart);
        List<string> processedBlock = TextFormatter(block, indent: false);
        result.AddRange(processedBlock);
    }

    return result;
}

// Метод для удаления ненужных символов и добавления отступов.
public List<string> StringChanger(List<string> lines)
{
    int whereQuantity = 0;

    for (int i = 0; i < lines.Count; i++)
    {
        if (i + 1 < lines.Count && lines[i + 1].Contains("where") && whereQuantity > 0) lines[i] += "\n";

        if (lines[i].Contains("where")) whereQuantity += 1;

        // Оставляем только "одинарные" пробелы.
        lines[i] = Regex.Replace(lines[i], @"(?<=\S)(  +)", " ");

        // Добавляем пробел после запятой.
        lines[i] = Regex.Replace(lines[i], @",(?=\S)", ", ");

        // Заменяем "( " на "(".
        lines[i] = Regex.Replace(lines[i], @"\(\s", "(");

        // Заменяем " )" на ")".
        lines[i] = Regex.Replace(lines[i], @"\s\)", ")");

        // Заменяем " , " на ", ".
        lines[i] = Regex.Replace(lines[i], @"\s,\s", ", ");
    }

    return lines;
}

// Метод, собирающий строчки в одну строку.
public string Combine(List<string> lines)
{
    string text = "";

    for (int i = 0; i < lines.Count(); i++)
    {
        text += lines[i] + "\n";
    }

    return text;
}

// Метод, проверяющий корректность грамматики.
public bool TextChecker(List<string> lines)
{
    foreach (string line in lines)
    {
        if (string.IsNullOrWhiteSpace(line)) continue;

        if (!Regex.IsMatch(line, @"^\s*(let|import|module)\b")) return false;

    }

    return true;
}  */