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
            Print str = new Print(@"module MyLinalg where
   import Unused
import M1 (f4, f2, f6)

   let solve(A, b  )   :=   LA.solve(A  ,  b  )

  import numpy.linalg as LA

import M1.M2 (f3, f5)

import M1 (f1)


let some_root(a , b  , c  ) :=   (  b +   math.sqrt(( discriminant(a, b, c ) ) )) / a where

  let discriminant(   a, b,c   ) := (b ^ 2) - 4 * (a * c)


  import math");

            str.PrintText();
        }
    }
}