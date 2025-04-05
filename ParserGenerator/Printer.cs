using System;
using ParserRulesGenerator; 

public static class ExpressionPrinter
{
    public static void Print(Expression expr, string indent = "")
    {
        switch (expr)
        {
            case BinaryExpr bin:
                Console.WriteLine($"{indent}BinaryExpr \"{bin.Op}\"");
                Console.WriteLine($"{indent}  Left:");
                Print(bin.Left, indent + "    ");
                Console.WriteLine($"{indent}  Right:");
                Print(bin.Right, indent + "    ");
                break;

            case NumberExpr num:
                Console.WriteLine($"{indent}NumberExpr {num.Value}");
                break;

            default:
                Console.WriteLine($"{indent}Unknown expression type: {expr.GetType().Name}");
                break;
        }
    }
}
