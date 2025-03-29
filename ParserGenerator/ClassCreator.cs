using System.Text;
using System.Text.RegularExpressions;

namespace ParserRulesGenerator
{
    /// <summary>
    /// Хранит описание одного правила (правой части "RULE: ... ::= ...").
    /// </summary>
    public class GrammarRule
    {
        public string RuleName { get; set; }

        /// <summary>
        /// Список "слотов" по порядку, например ["identifier", "operator", ...].
        /// </summary>
        public List<string> Slots { get; set; } = new List<string>();

        /// <summary>
        /// Полный текст правила (после ::=),
        /// например: "func <identifier> ( <identifier> , <identifier> ) { <identifier> = <integer> <operator> <integer>; }"
        /// Нужен для построения RegEx в ParserGenerator.
        /// </summary>
        public string RuleBody { get; set; }
    }

    /// <summary>
    /// Класс, который читает файл и генерирует C#-код:
    /// 1) Для каждого TYPE: ... ::= <NetType> <pattern>
    /// 2) Для каждого RULE: ... ::= ...
    /// </summary>
    public class ClassCreator
    {
        // Ключ: название типа (например, "identifier")
        // Значение: (netType, pattern), например ("string", "[a-zA-Z_][a-zA-Z0-9_]*")
        public Dictionary<string, (string NetType, string Pattern)> KnownTypes { get; private set; }
            = new Dictionary<string, (string NetType, string Pattern)>(StringComparer.OrdinalIgnoreCase);

        // Список правил (конструкций)
        public List<GrammarRule> Rules { get; private set; } = new List<GrammarRule>();

        /// <summary>
        /// В конструкторе читаем файл, парсим TYPE: и RULE:
        /// </summary>
        public ClassCreator(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Не найден файл с правилами.", filePath);
            }

            var lines = File.ReadAllLines(filePath);
            ParseLines(lines);
        }

        /// <summary>
        /// Собирает строки, ищет TYPE: и RULE:, формируя KnownTypes и Rules.
        /// </summary>
        private void ParseLines(string[] lines)
        {
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                // Игнорируем пустые строки и комментарии.
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("TYPE:", StringComparison.OrdinalIgnoreCase))
                {
                    // Пример: TYPE: identifier ::= string [a-zA-Z_][a-zA-Z0-9_]*
                    var withoutPrefix = line.Substring("TYPE:".Length).Trim();
                    // Разделяем по "::="
                    var parts = withoutPrefix.Split(new[] { "::=" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var typeName = parts[0].Trim();       // "identifier"
                        var rightSide = parts[1].Trim();      // "string [a-zA-Z_][a-zA-Z0-9_]*"

                        // Правая часть = "<NetType> <pattern>"
                        // Например: "string [a-zA-Z_][a-zA-Z0-9_]*"
                        var match = Regex.Match(rightSide, @"^(\S+)\s+(.+)$");
                        if (match.Success)
                        {
                            var netType = match.Groups[1].Value;   // "string"
                            var pattern = match.Groups[2].Value;   // "[a-zA-Z_][a-zA-Z0-9_]*"

                            if (!KnownTypes.ContainsKey(typeName))
                            {
                                KnownTypes[typeName] = (netType, pattern);
                            }
                        }
                    }
                }
                else if (line.StartsWith("RULE:", StringComparison.OrdinalIgnoreCase))
                {
                    // Пример: RULE: FunctionDeclaration ::= func <identifier> ( <identifier> , <identifier> ) { <identifier> = <integer> <operator> <integer>; }
                    var withoutPrefix = line.Substring("RULE:".Length).Trim();
                    var parts = withoutPrefix.Split(new[] { "::=" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var ruleName = parts[0].Trim();   // "FunctionDeclaration"
                        var ruleBody = parts[1].Trim();   // "func <identifier> (...)"

                        // Ищем все <...>, запоминаем в порядке
                        var matches = Regex.Matches(ruleBody, @"<([^>]+)>");
                        var slots = new List<string>();
                        foreach (Match m in matches)
                        {
                            string inside = m.Groups[1].Value.Trim();

                            // Если там есть '|', бросаем исключение
                            if (inside.Contains("|"))
                            {
                                throw new NotSupportedException(
                                    $"В грамматике не поддерживаются альтернативы в одном слоте: <{inside}> (правило {ruleName})");
                            }

                            slots.Add(inside);
                        }

                        var rule = new GrammarRule
                        {
                            RuleName = ruleName,
                            Slots = slots,
                            RuleBody = ruleBody
                        };

                        Rules.Add(rule);
                    }
                }
            }
        }

        /// <summary>
        /// Генерация итогового C#-кода:
        /// 1) Классы для типов (TYPE:)
        /// 2) Классы для правил (RULE:)
        /// </summary>
        public string GenerateClasses()
        {
            var sb = new StringBuilder();

            // Генерация классов для типов
            foreach (var kvp in KnownTypes)
            {
                string originalTypeName = kvp.Key;                 // напр. "identifier"
                (string NetType, string Pattern) info = kvp.Value; // напр. ("string", "[a-zA-Z_][a-zA-Z0-9_]*")

                // Делаем имя класса в PascalCase
                string pascalType = ToPascalCase(originalTypeName);
                // Экранируем бэкслеши в паттерне
                string patternEscaped = info.Pattern.Replace("\\", "\\\\");

                sb.AppendLine($"public class {pascalType}");
                sb.AppendLine("{");
                sb.AppendLine($"    public {info.NetType} Value {{ get; set; }}");
                sb.AppendLine();
                sb.AppendLine("    public bool IsMatch(string input)");
                sb.AppendLine("    {");
                sb.AppendLine($"        return System.Text.RegularExpressions.Regex.IsMatch(input, \"{patternEscaped}\");");
                sb.AppendLine("    }");
                sb.AppendLine("}");
                sb.AppendLine();
            }

            // Генерация классов для правил
            foreach (var rule in Rules)
            {
                sb.AppendLine($"public class {rule.RuleName}");
                sb.AppendLine("{");

                var propertyLines = new List<string>();
                var constructorParams = new List<string>();
                var constructorAssigns = new List<string>();

                // Считаем, сколько раз встречается один и тот же тип
                var typeCountMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < rule.Slots.Count; i++)
                {
                    string slotTypeName = rule.Slots[i]; // напр. "identifier", "integer"

                    if (!KnownTypes.ContainsKey(slotTypeName))
                    {
                        // Неизвестный тип – считаем просто string
                        // (или можно выбросить исключение, если хотим строгую проверку)
                        string propName = $"UnknownType{i + 1}";
                        propertyLines.Add($"public string {propName} {{ get; }}");
                        constructorParams.Add($"string {propName.ToLower()}");
                        constructorAssigns.Add($"this.{propName} = {propName.ToLower()};");
                    }
                    else
                    {
                        // Тип известен
                        string pascalType = ToPascalCase(slotTypeName); // например, "Identifier", "Integer"
                        if (!typeCountMap.ContainsKey(slotTypeName))
                            typeCountMap[slotTypeName] = 1;
                        else
                            typeCountMap[slotTypeName]++;

                        int index = typeCountMap[slotTypeName];
                        // Именуем свойства как <PascalType><N>: Identifier1, Identifier2...
                        string propName = $"{pascalType}{index}";
                        propertyLines.Add($"public {pascalType} {propName} {{ get; }}");

                        // в конструкторе: (Identifier identifier1).
                        string paramName = propName.Substring(0, 1).ToLower() + propName.Substring(1);
                        constructorParams.Add($"{pascalType} {paramName}");
                        constructorAssigns.Add($"this.{propName} = {paramName};");
                    }
                }

                // Добавляем свойства в класс.
                foreach (var line in propertyLines)
                {
                    sb.AppendLine($"    {line}");
                }
                sb.AppendLine();

                // Генерируем конструктор.
                string ctorParamsJoined = string.Join(", ", constructorParams);
                sb.AppendLine($"    public {rule.RuleName}({ctorParamsJoined})");
                sb.AppendLine("    {");
                foreach (var assign in constructorAssigns)
                {
                    sb.AppendLine($"        {assign};");
                }
                sb.AppendLine("    }");

                sb.AppendLine("}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}
