namespace FormatterWhile
{
    class Program
    {
        static void Main(string[] args)
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

  let discriminant(   a, b,c   ) := (b ^ 2) - 4 * (a * c)


  import math 

let my_fun(x, y, z) := x + y + z where

let my_constant := my_fun(1, 2, 3)
import mlf( x1, 4)

";

            str = new Parser().StringChanger(str);
            str = new Parser().SplitImports(str);

            List<string> inputs = str.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<Sentence> Sentences = new List<Sentence>();

            foreach (var input in inputs)
            {
                try
                {
                    var tokens = new Tokenizer().Tokenize(input);

                    Sentences.Add(new Parser().Parse(input));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Sentences = new Formatter().Format(Sentences);

            new Output().PrintFormattedSentences(Sentences);
        }
    }
}