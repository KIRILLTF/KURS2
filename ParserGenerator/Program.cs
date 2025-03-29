
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
                // 1) Создаём генератор классов (ClassCreator).
                var classCreator = new ClassCreator(filePath);

                // 2) Проверка грамматики перед генерацией.
                var validator = new GrammarValidator(classCreator.KnownTypes, classCreator.Rules);
                validator.Validate();
                Console.WriteLine("Грамматика прошла проверку.");

                // 3) Генерируем код для классов (types + rules).
                string generatedClassesCode = classCreator.GenerateClasses();
                File.WriteAllText("C:\\Users\\User\\OneDrive\\Desktop\\GeneratedClasses.cs", generatedClassesCode);
                Console.WriteLine("Сгенерированы классы типов и правил (GeneratedClasses.cs).");

                // 4) Генерируем парсер.
                var parserGenerator = new ParserGenerator(classCreator.KnownTypes, classCreator.Rules);
                string generatedParserCode = parserGenerator.GenerateParserClass();
                File.WriteAllText("C:\\Users\\User\\OneDrive\\Desktop\\GeneratedParser.cs", generatedParserCode);
                Console.WriteLine("Сгенерирован класс Parser (GeneratedParser.cs).");
                
                // 5) Демонстрация парсинга.
                Console.WriteLine("\nВведите строку для парсинга (соответствующую правилам):");
                string inputLine = Console.ReadLine();

                var parser = new Parser();

                try
                {
                    object resultObject = parser.Parse(inputLine);

                    Console.WriteLine($"\n=== РЕЗУЛЬТАТ ПАРСИНГА ===");
                    Console.WriteLine($"Тип: {resultObject.GetType().Name}");
                    Console.WriteLine("Свойства:");

                    var props = resultObject.GetType().GetProperties();
                    foreach (var prop in props)
                    {
                        var value = prop.GetValue(resultObject);

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
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при парсинге: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine("\nНажмите Enter для выхода...");
            Console.ReadLine();
        }
    }
}