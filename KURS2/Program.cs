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

 import M3.M2 (f1, a3)

import M2.M3.M4 (f3, f7)

import A2 import X4
import M1.M2 (f3, f5)

import M1 (f1)

     import M2.M3.M4 (x6)

let some_root(a , b  , c  ) :=   (  b +   math.sqrt(( discriminant(a, b, c ) ) )) / a where

  let discriminant(   a, b,c   ) := (b ^ 2) - 4 * (a * c)


  import math 

let my_fun(x, y, z) := x + y + z
let my_constant := my_fun(1, 2, 3)");

            str.PrintText();
        }
    }
}