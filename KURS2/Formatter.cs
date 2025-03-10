using System;
using System.Collections.Generic;
using System.Linq;

class Formatter
{ /*
    public List<Sentence> Format(List<Sentence> sentences)
    {
        // Объединяем переменные у объектов с одинаковыми списками модулей
        var mergedSentences = MergeVariables(sentences);

        // Сортируем предложения
        var sortedSentences = SortSentences(mergedSentences);

        return sortedSentences;
    } */

    // Объединяет переменные у объектов с одинаковыми списками модулей
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

    /* Сортирует предложения
    private List<Sentence> SortSentences(List<Sentence> sentences)
    {
        // Группируем предложения по наличию where
        var grouped = sentences.GroupBy(s => s is LetSentence let && let.HasWhere);

        var result = new List<Sentence>();

        foreach (var group in grouped)
        {
            if (group.Key) // Группа с where
            {
                // Сначала добавляем импорты, затем остальные
                result.AddRange(group.Where(s => s is ImportSentence).OrderBy(s => GetSentenceKey(s)));
                result.AddRange(group.Where(s => !(s is ImportSentence)));
            }
            else // Группа без where
            {
                // Сначала добавляем импорты, затем остальные
                result.AddRange(group.Where(s => s is ImportSentence).OrderBy(s => GetSentenceKey(s)));
                result.AddRange(group.Where(s => !(s is ImportSentence)));
            }
        }

        return result;
    } */
}