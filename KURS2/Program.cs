using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace FormatterWhile
{
    class Program
    {
        static void Main(string[] args)
        {
            string input = @"module MyLinalg where
   import Unused

   let solve(A, b  )   :=   LA.solve(A  ,  b  )

   import numpy.linalg as LA


let some_root(a , b  , c  ) :=   (  b +   math.sqrt(( discriminant(a, b, c ) ) )) / a where

  let discriminant(   a, b,c   ) := (b ^ 2) - 4 * (a * c)


  import math";

            List<string> result = new Formatting().Output(input);
            result = new Formatting().StringChanger(result);

            // Вывод результата
            Console.WriteLine("После форматирования:\n");

            foreach (var line in result)
            {
                Console.WriteLine(line);
            }
        }
    }
}