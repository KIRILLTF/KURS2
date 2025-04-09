using System;
using System.Collections.Generic;
using System.Linq;

namespace FormatterWhile
{
    class Program
    {
        static void Main()
        {
            string str = @"module MyLinalg where
       import Unused
    import M1(f4, f2, f6)
    
       let solve(A, b  )   :=   LA.solve(A  ,  b  )
    
      import numpy.linalg as LA
    
     import M3.M2(f1, a3) 
    import M1(f3, f7)
    
    import A2 import X4 import y6
    import M1.M2(f3, f5)
    
    import M1(f1)
    
         import M2.M3.M4(a5)
    
    let some_root(a , b  , c  ) := (b + math.sqrt(discriminant(a, b, c))) / a where
    
      let discriminant(a, b, c) := (b ^ 2) - 4 * (a * d) * 9
    
      import math 
    
    let my_fun(x, y, z) := x + y + z where
    
    let my_constant := my_fun(1, 2, 3)
";

            // Применяем преобразования строки, если они нужны
            Parser parser = new Parser();
            str = parser.StringChanger(str);
            str = parser.SplitImports(str);

            // Разбиваем исходную строку на отдельные строки (предложения)
            List<string> inputs = str
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // Создаём экземпляр токенизатора
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

            // Парсинг строк с использованием вашего существующего парсера
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
                    Console.WriteLine(ex.Message);
                }
            }

            // Применение форматирования
            Sentences = new Formatter().Format(Sentences);

            // Вывод форматированного кода
            Console.WriteLine("== Форматированный код ==\n");
            new Output().PrintFormattedSentences(Sentences);

            // Вывод AST для каждого LetSentence
            Console.WriteLine("\n== AST выражений ==\n");
            foreach (var s in Sentences)
            {
                if (s is LetSentence let)
                {
                    Console.WriteLine($"LET {string.Join(".", let.Name)}(...) := {let.ExpressionString}");
                    Console.WriteLine("Дерево выражения:");
                    AstPrinter.PrintAst(let.ExpressionAst);  // Вызов метода печати AST
                    Console.WriteLine();
                }
            }
        }
    }
}
