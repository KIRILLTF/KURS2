using System.Text.RegularExpressions;

class Formatting
{
    public List<string> ProcessBlock(List<string> block, bool indent)
    {
        // Убираем пустые строки и обрезаем пробелы по краям
        List<string> nonEmpty = block
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToList();

        // Выбираем строки с import
        List<string> imports = nonEmpty.Where(l => Regex.IsMatch(l, @"\bimport\b")).ToList();
        List<string> others = nonEmpty.Where(l => !Regex.IsMatch(l, @"\bimport\b")).ToList();

        // Обрабатываем и сортируем импорты
        List<string> sortedImports = ProcessImports(imports);

        // Итоговый блок
        List<string> processed = new List<string>();
        processed.AddRange(sortedImports);
        processed.AddRange(others);

        // Добавляем отступы
        if (indent)
        {
            processed = processed.Select(l => "\t" + l).ToList();
        }
        return processed;
    }

    private List<string> ProcessImports(List<string> imports)
    {
        var importGroups = new Dictionary<string, SortedSet<string>>();

        foreach (string import in imports)
        {
            var match = Regex.Match(import, @"import\s+([\w\.]+)(?:\s+\(([^)]+)\))?");
            if (!match.Success) continue;

            string module = match.Groups[1].Value;
            string entities = match.Groups[2].Success ? match.Groups[2].Value : null;

            if (!importGroups.ContainsKey(module))
                importGroups[module] = new SortedSet<string>();

            if (entities != null)
            {
                foreach (var entity in entities.Split(',').Select(e => e.Trim()))
                {
                    importGroups[module].Add(entity);
                }
            }
        }

        return importGroups
            .OrderBy(kv => kv.Key.Split('.'), new ModuleComparer())
            .Select(kv => kv.Value.Count > 0
                ? $"import {kv.Key} ({string.Join(", ", kv.Value)})"
                : $"import {kv.Key}")
            .ToList();
    }

    private class ModuleComparer : IComparer<string[]>
    {
        public int Compare(string[] x, string[] y)
        {
            int len = Math.Min(x.Length, y.Length);
            for (int i = 0; i < len; i++)
            {
                int cmp = string.Compare(x[i], y[i], StringComparison.Ordinal);
                if (cmp != 0) return cmp;
            }
            return x.Length.CompareTo(y.Length);
        }
    }

    public List<string> Output(string input)
    {
        List<string> lines = input.Split(new[] { "\n" }, StringSplitOptions.None).ToList();

        List<string> result = new List<string>();

        // Индекс начала промежутка (один where)
        int blockStart = 0;
        int i = 0;

        while (i < lines.Count)
        {
            if (lines[i].Contains("where"))
            {
                List<string> block = lines.GetRange(blockStart, i - blockStart);
                List<string> processedBlock = ProcessBlock(block, indent: false);
                result.AddRange(processedBlock);

                // Добавляем строку с where – обрезав лишние пробелы
                result.Add(lines[i].Trim());
                i++; // переходим к следующей строке после where

                // Собираем блок, принадлежащий данному where, до следующей строки, содержащей where,
                // или до конца файла.
                blockStart = i;

                while (i < lines.Count && !lines[i].Contains("where"))
                {
                    i++;
                }

                List<string> whereBlock = lines.GetRange(blockStart, i - blockStart);
                // Передаем параметр indent:true – все строки блока получат табуляцию
                List<string> processedWhereBlock = ProcessBlock(whereBlock, indent: true);
                result.AddRange(processedWhereBlock);

                // Новый блок начинается с текущей позиции
                blockStart = i;
            }
            else
            {
                i++;
            }
        }

        // Если после последнего where остались строки – обрабатываем их (без дополнительного отступа)
        if (blockStart < lines.Count)
        {
            List<string> block = lines.GetRange(blockStart, lines.Count - blockStart);
            List<string> processedBlock = ProcessBlock(block, indent: false);
            result.AddRange(processedBlock);
        }

        return result;
    }

    public List<string> StringChanger(List<string> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            // Оставляем только "одинарные" пробелы.
            lines[i] = Regex.Replace(lines[i], @"\s{2,}", " ");

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

    public string Combine(List<string> lines)
    {
        string text = "";

        for (int i = 0; i < lines.Count(); i++)
        {
            text += lines[i] + "\n";
        }

        return text;
    }
}