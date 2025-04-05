class Output
{
    public void PrintFormattedSentences(List<Sentence> sentences)
    {
        int whereQuantity = 0;

        foreach (var sentence in sentences)
        {
            if (sentence is ModuleSentence module)
            {
                // Перед первым module (и вообще после предыдущих конструкций) можно печатать пустую строку
                if (whereQuantity > 0) Console.WriteLine();

                Console.WriteLine($"module {string.Join(".", module.Name)} where");
                whereQuantity++;
            }
            else if (sentence is ImportSentence import)
            {
                Console.Write($"    import {string.Join(".", import.Name)}");
                if (import.Variables != null && import.Variables.Count > 0)
                {
                    Console.Write($"({string.Join(", ", import.Variables)})");
                }
                if (!string.IsNullOrEmpty(import.Alias))
                {
                    Console.Write($" as {import.Alias}");
                }
                Console.WriteLine();
            }
            else if (sentence is LetSentence let)
            {
                if (let.HasWhere)
                {
                    if (whereQuantity > 0) Console.WriteLine();
                    whereQuantity++;
                }
                else
                {
                    Console.Write("    ");
                }

                Console.Write($"let {string.Join(".", let.Name)}");

                if (let.Variables != null && let.Variables.Count > 0)
                {
                    Console.Write($"({string.Join(", ", let.Variables)})");
                }
                Console.Write($" := {let.Expression}");

                if (let.HasWhere)
                {
                    Console.Write(" where");
                }
                Console.WriteLine();
            }
        }
    }
}