using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FormatterWhile
{
    class Program
    {
        static void Main()
        {
            // Ввод исходного кода с консоли.
            Console.WriteLine("Введите исходный код (для завершения ввода наберите END):");
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                string line = Console.ReadLine();
                if (line.Trim().Equals("END", StringComparison.OrdinalIgnoreCase))
                    break;
                sb.AppendLine(line);
            }
            string str = sb.ToString();

            // Применяем преобразования строки, если они нужны.
            Parser parser = new Parser();
            str = parser.StringChanger(str);
            str = parser.SplitImports(str);

            // Разбиваем исходную строку на отдельные строки (предложения).
            List<string> inputs = str
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // Создаём экземпляр токенизатора.
            Tokenizer tokenizer = new Tokenizer();

            Console.WriteLine("== Токенизация входных строк ==\n");
            foreach (string input in inputs)
            {
                try
                {
                    List<string> tokens = tokenizer.Tokenize(input);
                    Console.WriteLine("Строка: " + input);
                    Console.WriteLine("Токены: " + string.Join(", ", tokens));
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка токенизации: " + ex.Message);
                }
            }

            // Парсинг строк с использованием существующего парсера.
            List<Sentence> Sentences = new List<Sentence>();

            foreach (var input in inputs)
            {
                try
                {
                    var sentence = parser.Parse(input);
                    Sentences.Add(sentence);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка парсинга: " + ex.Message);
                }
            }

            // Применение форматирования.
            Sentences = new Formatter().Format(Sentences);

            // Вывод форматированного кода.
            Console.WriteLine("== Форматированный код ==\n");
            new Output().PrintFormattedSentences(Sentences);

            // Вывод AST для каждого LetSentence.
            Console.WriteLine("\n== AST выражений ==\n");
            foreach (var s in Sentences)
            {
                if (s is LetSentence let)
                {
                    Console.WriteLine($"LET {string.Join(".", let.Name)}(...) := {let.ExpressionString}");
                    Console.WriteLine("Дерево выражения:");
                    AstPrinter.PrintAst(let.ExpressionAst);
                    Console.WriteLine();
                }
            }
        }
    }
}
