static class AstPrinter
{
    /// <summary>
    /// Рекурсивно обходит дерево выражений и выводит структуру на консоль.
    /// </summary>
    /// <param name="node">Корень выражения.</param>
    /// <param name="indent">Отступ (для вложенных вызовов).</param>
    public static void PrintAst(ExpressionNode node, string indent = "")
    {
        // Защита от null
        if (node == null)
        {
            Console.WriteLine($"{indent}(null)");
            return;
        }

        // В зависимости от типа узла - своя логика вывода
        switch (node)
        {
            case BinaryExpressionNode bin:
                Console.WriteLine($"{indent}BinaryExpressionNode (Operator = '{bin.Operator}')");
                Console.WriteLine($"{indent}├─ Left:");
                PrintAst(bin.Left, indent + "│  ");
                Console.WriteLine($"{indent}└─ Right:");
                PrintAst(bin.Right, indent + "   ");
                break;

            case UnaryExpressionNode un:
                Console.WriteLine($"{indent}UnaryExpressionNode (Operator = '{un.Operator}')");
                Console.WriteLine($"{indent}└─ Operand:");
                PrintAst(un.Operand, indent + "   ");
                break;

            case NumberNode num:
                Console.WriteLine($"{indent}NumberNode (Value = {num.Value})");
                break;

            case VariableNode varNode:
                Console.WriteLine($"{indent}VariableNode (Name = '{varNode.Name}')");
                break;

            case FunctionCallNode func:
                Console.WriteLine($"{indent}FunctionCallNode (FunctionName = '{func.FunctionName}')");
                if (func.Arguments?.Count > 0)
                {
                    for (int i = 0; i < func.Arguments.Count; i++)
                    {
                        bool lastArg = (i == func.Arguments.Count - 1);
                        string marker = lastArg ? "└─" : "├─";

                        Console.WriteLine($"{indent}{marker} Arg[{i}]:");
                        // Немного другой отступ, чтобы дерево не сливалось
                        string deeperIndent = lastArg
                            ? indent + "   "
                            : indent + "│  ";
                        PrintAst(func.Arguments[i], deeperIndent);
                    }
                }
                else
                {
                    Console.WriteLine($"{indent}└─ (no arguments)");
                }
                break;

            default:
                // На случай, если появятся новые типы узлов
                Console.WriteLine($"{indent}{node.GetType().Name}");
                break;
        }
    }
}
