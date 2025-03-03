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

        // Выбираем строки, содержащие import с помощью регулярного выражения
        List<string> imports = nonEmpty.Where(l => Regex.IsMatch(l, @"\bimport\b")).ToList();
        // Остальные строки
        List<string> others = nonEmpty.Where(l => !Regex.IsMatch(l, @"\bimport\b")).ToList();

        // Итоговый блок: сначала строки с import, затем остальные
        List<string> processed = new List<string>();
        processed.AddRange(imports);
        processed.AddRange(others);

        // Если требуется отступ, добавляем табуляцию перед каждой строкой
        if (indent)
        {
            processed = processed.Select(l => "\t" + l).ToList();
        }
        return processed;
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