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
                
                let some_root(a , b  , c  ) :=   (  b +   math.sqrt(( discriminant(a, b, c ) ) )) / a where
                
                  let discriminant(   a, b,c   ) := (b ^ 2) - 4 * (a * d) 9
                
                
                  import math 
                
                let my_fun(x, y, z) := x + y + z where
                
                let my_constant := my_fun(1, 2, 3)
            ";

            Parser parser = new Parser();
            str = parser.StringChanger(str);
            str = parser.SplitImports(str);

            List<string> inputs = str.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
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

            Sentences = new Formatter().Format(Sentences);

            // Вывод отформатированных строк
            Console.WriteLine("== Форматированный код ==\n");
            new Output().PrintFormattedSentences(Sentences);

            // Отладочная информация: переменные, операторы, функции
            Console.WriteLine("\n== Детали предложений ==\n");
            foreach (var s in Sentences)
            {
                Console.WriteLine($"→ {string.Join(".", s.Name)}");

                if (s.Variables != null && s.Variables.Count > 0)
                    Console.WriteLine($"  Переменные: {string.Join(", ", s.Variables)}");

                if (s.Operators != null && s.Operators.Count > 0)
                    Console.WriteLine($"  Операторы: {string.Join(", ", s.Operators)}");

                if (s.Functions != null && s.Functions.Count > 0)
                    Console.WriteLine($"  Функции: {string.Join(", ", s.Functions)}");

                Console.WriteLine();
            }
        }
    }
}
