using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using ParserRulesGenerator;  // <- Где лежит ClassCreator, ParserGenerator, Expression, ExpressionPrinter

namespace MyConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // ============= 1) Вводим путь до файла с грамматикой =============
            Console.WriteLine("Введите путь до файла с грамматикой (например, C:\\Temp\\Test.txt):");
            string grammarPath = Console.ReadLine().Trim();
            if (!File.Exists(grammarPath))
            {
                Console.WriteLine("Файл не найден: " + grammarPath);
                return;
            }

            try
            {
                // ============= 2) Генерируем код из грамматики =============
                var classCreator = new ClassCreator(grammarPath);

                // Проверка грамматики
                var validator = new GrammarValidator(classCreator.KnownTypes, classCreator.Rules);
                validator.Validate();

                // Генерация кода (типы + парсер)
                string generatedClassesCode = classCreator.GenerateClasses();
                string generatedParserCode =
                    new ParserGenerator(classCreator.KnownTypes, classCreator.Rules)
                    .GenerateParserClass();

                // ============= 3) Сохраняем эти файлы на рабочий стол =============
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                string classesPath = Path.Combine(desktop, "GeneratedClasses.cs");
                string parserPath = Path.Combine(desktop, "GeneratedParser.cs");

                File.WriteAllText(classesPath, generatedClassesCode);
                File.WriteAllText(parserPath, generatedParserCode);

                Console.WriteLine("Сгенерированы файлы:");
                Console.WriteLine("  " + classesPath);
                Console.WriteLine("  " + parserPath);

                // ============= 4) Формируем единый код для Roslyn =============
                // *Важно:* using сверху, потом namespace, потом код классов/парсера
                string fullCode = @$"
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using ParserRulesGenerator; // Чтобы видеть Expression и другие классы

namespace Generated
{{
    {generatedClassesCode}

    {generatedParserCode}
}}
";

                // ============= 5) Компиляция в памяти через Roslyn =============
                var syntaxTree = CSharpSyntaxTree.ParseText(fullCode);

                // Собираем References. Вариант «взять все сборки из текущего AppDomain»
                var references = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                    .Select(a => MetadataReference.CreateFromFile(a.Location))
                    .ToList();

                // Если Expression лежит в ParserRulesGenerator.dll, уже должно быть внутри AppDomain,
                // но на всякий случай можно добавить:
                references.Add(MetadataReference.CreateFromFile(typeof(Expression).Assembly.Location));

                var compilation = CSharpCompilation.Create(
                    assemblyName: "GeneratedParserAssembly",
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );

                using (var ms = new MemoryStream())
                {
                    var result = compilation.Emit(ms);
                    if (!result.Success)
                    {
                        Console.WriteLine("Компиляция сгенерированного кода не удалась.");
                        foreach (var diag in result.Diagnostics)
                            Console.WriteLine(diag.ToString());
                        return;
                    }

                    // Загружаем сборку в память
                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());

                    // ============= 6) Создаём объект Parser из сгенерированного кода =============
                    // В fullCode namespace = Generated, а класс = Parser
                    var parserType = assembly.GetType("Generated.Parser");
                    object parserInstance = Activator.CreateInstance(parserType);

                    Console.WriteLine("\n=== Сгенерированный Parser готов к работе! ===");

                    // ============= 7) Входим в цикл, где парсим ввод =============
                    while (true)
                    {
                        Console.WriteLine("\nВведите строку для парсинга (exit/выход для выхода):");
                        Console.Write("> ");
                        string inputLine = Console.ReadLine();
                        if (string.Equals(inputLine, "exit", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(inputLine, "выход", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        try
                        {
                            // Вызовем Parse
                            var parseMethod = parserType.GetMethod("Parse");
                            object resultObj = parseMethod.Invoke(parserInstance, new object[] { inputLine });

                            Console.WriteLine("\n=== РЕЗУЛЬТАТ ПАРСИНГА ===");
                            // Печатаем тип
                            Console.WriteLine("Тип: " + resultObj.GetType().Name);

                            // 8) Печатаем содержимое
                            PrintResultObject(resultObj);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ошибка при парсинге: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }

            Console.WriteLine("\nПрограмма завершена. Нажмите Enter...");
            Console.ReadLine();
        }

        /// <summary>
        /// Печатает информацию о распаршенном объекте (пробует найти Value, Expression и т.д.)
        /// </summary>
        static void PrintResultObject(object resultObj, string indent = "")
        {
            if (resultObj is Expression expr)
            {
                // Если это алгебраическое выражение, используем наш ExpressionPrinter
                ExpressionPrinter.Print(expr, indent);
                return;
            }

            // В остальных случаях смотрим свойства
            var props = resultObj.GetType().GetProperties();
            if (props.Length == 0)
            {
                Console.WriteLine(indent + "(нет свойств)");
                return;
            }

            foreach (var p in props)
            {
                var value = p.GetValue(resultObj);

                // Если значение == null
                if (value == null)
                {
                    Console.WriteLine($"{indent}{p.Name} = null");
                    continue;
                }

                // Если это тоже Expression -> печатаем дерево
                if (value is Expression exprProp)
                {
                    Console.WriteLine($"{indent}{p.Name} = Expression AST:");
                    ExpressionPrinter.Print(exprProp, indent + "  ");
                }
                else
                {
                    // Пробуем найти свойство Value
                    var valProp = value.GetType().GetProperty("Value");
                    if (valProp != null)
                    {
                        // Например, VarName.Value = "x"
                        var innerVal = valProp.GetValue(value);
                        Console.WriteLine($"{indent}{p.Name} = {innerVal}");
                    }
                    else
                    {
                        if (value.GetType().Namespace == "Generated"
                            || value.GetType().Namespace == "ParserRulesGenerator")
                        {
                            // Рекурсивно печатаем
                            Console.WriteLine($"{indent}{p.Name} ->:");
                            PrintResultObject(value, indent + "  ");
                        }
                        else
                        {
                            // Иначе просто печатаем ToString()
                            Console.WriteLine($"{indent}{p.Name} = {value}");
                        }
                    }
                }
            }
        }
    }
}