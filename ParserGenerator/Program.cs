namespace ParserRulesGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = "C:\\Users\\User\\OneDrive\\Desktop\\Test.txt";

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Ошибка: Файл {filePath} не найден.");
                return;
            }

            try
            {
                var classCreator = new ClassCreator(filePath);

                var validator = new GrammarValidator(classCreator.KnownTypes, classCreator.Rules);
                validator.Validate();
                Console.WriteLine("Грамматика прошла проверку.");

                string generatedClassesCode = classCreator.GenerateClasses();
                File.WriteAllText("C:\\Users\\User\\OneDrive\\Desktop\\GeneratedClasses.cs", generatedClassesCode);
                Console.WriteLine("Сгенерированы классы типов и правил (GeneratedClasses.cs).");

                var parserGenerator = new ParserGenerator(classCreator.KnownTypes, classCreator.Rules);
                string generatedParserCode = parserGenerator.GenerateParserClass();
                File.WriteAllText("C:\\Users\\User\\OneDrive\\Desktop\\GeneratedParser.cs", generatedParserCode);
                Console.WriteLine("Сгенерирован класс Parser (GeneratedParser.cs).");

                Console.WriteLine("\nВведите строку для парсинга (введите 'exit' для выхода):");
                
                // Создаём экземпляр сгенерированного парсера
                var parser = new Parser();

                while (true)
                {
                    Console.Write("\n> ");
                    string inputLine = Console.ReadLine();
                    if (string.Equals(inputLine, "exit", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(inputLine, "выход", StringComparison.OrdinalIgnoreCase))
                        break;

                    try
                    {
                        // Попытка распарсить
                        object resultObject = parser.Parse(inputLine);

                        Console.WriteLine("\n=== РЕЗУЛЬТАТ ПАРСИНГА ===");
                        Console.WriteLine($"Тип: {resultObject.GetType().Name}");
                        Console.WriteLine("Свойства:");

                        var props = resultObject.GetType().GetProperties();
                        foreach (var prop in props)
                        {
                            var value = prop.GetValue(resultObject);

                            // 1) Если это алгебраическое Expression – печатаем дерево
                            if (value is Expression expr)
                            {
                                Console.WriteLine($"  {prop.Name}: (Expression AST)");
                                ExpressionPrinter.Print(expr, "    ");
                            }
                            // 2) Если есть вложенное поле Value (например, для varName с int/string/etc.)
                            else
                            {
                                // пытаемся найти свойство "Value"
                                var innerProp = value?.GetType().GetProperty("Value");
                                if (innerProp != null)
                                {
                                    var innerValue = innerProp.GetValue(value);
                                    Console.WriteLine($"  {prop.Name}: {innerValue}");
                                }
                                else
                                {
                                    Console.WriteLine($"  {prop.Name}: {value}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при парсинге: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine("\nПрограмма завершена. Нажмите Enter...");
            Console.ReadLine();
        }
    }
}
