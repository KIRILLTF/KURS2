﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ParserRulesGenerator;  // Чтобы видеть ClassCreator, ParserGenerator, Expression, ExpressionPrinter, Fuzzer

namespace MyConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1) Вводим путь до файла с грамматикой
            Console.WriteLine("Введите путь до файла с грамматикой (например, C:\\Temp\\Test.txt):");
            string grammarPath = Console.ReadLine().Trim();
            if (!File.Exists(grammarPath))
            {
                Console.WriteLine("Файл не найден: " + grammarPath);
                return;
            }

            try
            {
                // 2) Генерируем код из грамматики
                var classCreator = new ClassCreator(grammarPath);

                // Проверка грамматики
                var validator = new GrammarValidator(classCreator.KnownTypes, classCreator.Rules);
                validator.Validate();

                // Генерация кода (типы + парсер)
                string generatedClassesCode = classCreator.GenerateClasses();
                string generatedParserCode =
                    new ParserGenerator(classCreator.KnownTypes, classCreator.Rules)
                    .GenerateParserClass();

                // 2.1) Тела классов и парсера (без using)
                string classesBody = classCreator.GenerateClasses();
                string parserBody = new ParserGenerator(classCreator.KnownTypes,
                                                         classCreator.Rules)
                                     .GenerateParserClass();

                // 2.2) Формируем версии для сохранения на рабочем столе
                const string fileHeader = "using ParserRulesGenerator;" + "\n";

                bool HasHeader(string text) =>
                    text.TrimStart().StartsWith("using ParserRulesGenerator;", StringComparison.Ordinal);

                string generatedClassesFile = HasHeader(classesBody)
                                              ? classesBody
                                              : fileHeader + classesBody;

                string generatedParserFile = HasHeader(parserBody)
                                              ? parserBody
                                              : fileHeader + parserBody;

                // 3) Сохраняем файлы на рабочем столе
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string classesPath = Path.Combine(desktop, "GeneratedClasses.cs");
                string parserPath = Path.Combine(desktop, "GeneratedParser.cs");

                File.WriteAllText(classesPath, generatedClassesFile);
                File.WriteAllText(parserPath, generatedParserFile);

                Console.WriteLine("Сгенерированы файлы:");
                Console.WriteLine("  " + classesPath);
                Console.WriteLine("  " + parserPath);

                // 4) Формируем единый код для Roslyn
                string fullCode = @$"
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using ParserRulesGenerator; // Чтобы видеть Expression и другие классы

namespace Generated
{{
    {classesBody}

    {parserBody}
}}";
                // 5) Компиляция в памяти через Roslyn
                var syntaxTree = CSharpSyntaxTree.ParseText(fullCode);
                var references = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                    .Select(a => MetadataReference.CreateFromFile(a.Location))
                    .ToList();
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

                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());

                    // 6) Создаём объект Parser из сгенерированного кода
                    // В fullCode namespace = Generated, а класс = Parser
                    var parserType = assembly.GetType("Generated.Parser");
                    object parserInstance = Activator.CreateInstance(parserType);

                    Console.WriteLine("\nСгенерированный Parser готов к работе!");

                    // 7) Режим фаззинга
                    Console.WriteLine("\nЗапустить фаззер для тестирования парсера? (y/n):");
                    string runFuzzer = Console.ReadLine().Trim().ToLower();
                    if (runFuzzer == "y" || runFuzzer == "yes")
                    {
                        Console.WriteLine("\nФаззер: тестовые строки");
                        foreach (var rule in classCreator.Rules)
                        {
                            // Пропускаем правила-ошибки
                            if (rule.IsErrorRule)
                                continue;

                            // Генерируем тестовую строку для текущего правила
                            string testString = Fuzzer.FuzzRule(rule, classCreator.KnownTypes);
                            Console.WriteLine($"\nПравило '{rule.RuleName}': {testString}");

                            try
                            {
                                var parseMethod = parserType.GetMethod("Parse");
                                object resultObj = parseMethod.Invoke(parserInstance, new object[] { testString });

                                Console.WriteLine("Результат парсинга:");
                                PrintResultObject(resultObj);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Ошибка при парсинге: " + (ex.InnerException?.Message ?? ex.Message));
                            }
                            Console.WriteLine(new string('-', 40));
                        }
                    }

                    // 8) Интерфейс для ручного ввода
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
                            var parseMethod = parserType.GetMethod("Parse");
                            object resultObj = parseMethod.Invoke(parserInstance, new object[] { inputLine });

                            Console.WriteLine("\n=== РЕЗУЛЬТАТ ПАРСИНГА ===");
                            Console.WriteLine("Тип: " + resultObj.GetType().Name);
                            PrintResultObject(resultObj);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ошибка при парсинге: " + (ex.InnerException?.Message ?? ex.Message));
                        }
                    }
                }
            }
            catch (TargetInvocationException ex)
            {
                Console.WriteLine("Ошибка при парсинге: " + (ex.InnerException?.Message ?? ex.Message));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при парсинге: " + ex.Message);
            }

            Console.WriteLine("\nПрограмма завершена. Нажмите Enter...");
            Console.ReadLine();
        }

        /// <summary>
        /// Рекурсивно печатает свойства распарсенного объекта.
        /// Если объект является Expression, используется ExpressionPrinter.
        /// </summary>
        static void PrintResultObject(object resultObj, string indent = "")
        {
            if (resultObj is Expression expr)
            {
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
