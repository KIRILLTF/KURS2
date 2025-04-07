using System.Text;
using System.Text.RegularExpressions;

namespace ParserRulesGenerator
{
    public class ParserGenerator
    {
        private readonly Dictionary<string, (string NetType, string Pattern)> _knownTypes;
        private readonly List<GrammarRule> _rules;

        public ParserGenerator(
            Dictionary<string, (string NetType, string Pattern)> knownTypes,
            List<GrammarRule> rules)
        {
            _knownTypes = knownTypes;
            _rules = rules;
        }

        /// <summary>
        /// Генерирует класс Parser, в котором для каждого RULE: и ERROR:
        /// создаём статические поля Regex и if-блоки.
        ///
        /// - Обычные RULE: -> if(...) { ... return new RuleName(...); }
        /// - ERROR: -> if(...) { throw new Exception("..."); }
        ///
        /// Порядок: Сначала обрабатываем ERROR-правила, затем обычные.
        /// </summary>
        public string GenerateParserClass()
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("public class Parser");
            sb.AppendLine("{");

            // ПОЛЯ для Regex
            var patternFields = new List<string>();
            var errorIfBlocks = new List<string>();
            var normalIfBlocks = new List<string>();

            foreach (var rule in _rules)
            {
                string fieldName = char.ToLower(rule.RuleName[0]) + rule.RuleName.Substring(1) + "Pattern";
                // Соберём паттерн
                string regexPattern = BuildRegexPattern(rule.RuleBody);

                // Поле Regex
                patternFields.Add(
                    $"    private static readonly Regex {fieldName} = new(@\"{regexPattern}\", RegexOptions.Compiled);"
                );

                // if-блок
                if (rule.IsErrorRule)
                {
                    // Ошибочное правило
                    string block = $@"
     // Error rule: {rule.RuleName}
     if ({fieldName}.IsMatch(input))
     {{
         throw new Exception(""Пользовательская ошибка: {rule.RuleName}"");
     }}";
                    errorIfBlocks.Add(block);
                }
                else
                {
                    // Обычное правило
                    string block = BuildIfBlock(rule, fieldName);
                    normalIfBlocks.Add(block);
                }
            }

            // Выводим поля
            foreach (var line in patternFields)
                sb.AppendLine(line);

            sb.AppendLine();
            sb.AppendLine("    public object Parse(string input)");
            sb.AppendLine("    {");

            // Сначала ERROR
            foreach (var errBlock in errorIfBlocks)
                sb.AppendLine(errBlock);

            // Потом нормальные правила
            foreach (var ruleBlock in normalIfBlocks)
                sb.AppendLine(ruleBlock);

            sb.AppendLine("        throw new ArgumentException($\"Ни один шаблон не подошел для строки: {input}\");");
            sb.AppendLine("    }");

            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Построить Regex-паттерн, заменяя <Type> на (паттерн) или (.+?) для Expression
        /// </summary>
        private string BuildRegexPattern(string ruleBody)
        {
            var sb = new StringBuilder();
            int curIndex = 0;

            var matches = Regex.Matches(ruleBody, @"<([^>]+)>");
            foreach (Match m in matches)
            {
                // Текст до <...>
                string literal = ruleBody.Substring(curIndex, m.Index - curIndex);
                sb.Append(Regex.Escape(literal));

                // Вырезаем имя слота
                string slot = m.Groups[1].Value.Trim();

                // Особый случай: Expression
                if (string.Equals(slot, "Expression", StringComparison.OrdinalIgnoreCase))
                {
                    // Парсим всё что угодно до следующего разделителя
                    sb.Append("(.+?)");
                }
                else if (_knownTypes.TryGetValue(slot, out var info))
                {
                    string pattern = info.Pattern.Trim();
                    if (pattern.StartsWith("(") && pattern.EndsWith(")"))
                    {
                        // Уберём внешние скобки
                        pattern = pattern.Substring(1, pattern.Length - 2);
                    }
                    sb.Append("(" + pattern + ")");
                }
                else
                {
                    // fallback
                    sb.Append("(.+?)");
                }

                curIndex = m.Index + m.Length;
            }

            // Хвост
            if (curIndex < ruleBody.Length)
            {
                string tail = ruleBody.Substring(curIndex);
                sb.Append(Regex.Escape(tail));
            }

            // Добавим ^\s*...\s*$
            return "^\\s*" + sb.ToString() + "\\s*$";
        }

        private string BuildIfBlock(GrammarRule rule, string fieldName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"        if ({fieldName}.IsMatch(input))");
            sb.AppendLine("        {");
            sb.AppendLine($"            var match = {fieldName}.Match(input);");

            var rawVars = new List<string>();
            var typedVars = new List<string>();
            var constructorArgs = new List<string>();

            for (int i = 0; i < rule.Slots.Count; i++)
            {
                int groupIndex = i + 1;
                string slotName = rule.Slots[i];
                string rawVarName = $"raw{i + 1}";
                string typedVarName = $"slot{i + 1}";

                // Считываем строку
                rawVars.Add($"var {rawVarName} = match.Groups[{groupIndex}].Value;");

                if (string.Equals(slotName, "Expression", StringComparison.OrdinalIgnoreCase))
                {
                    // Вызываем ExpressionParser
                    typedVars.Add($"var {typedVarName} = ParserRulesGenerator.ExpressionParser.Parse({rawVarName});");
                    constructorArgs.Add(typedVarName);
                }
                else
                {
                    // Обычный слот, возможно известного типа
                    string netType = "string";
                    if (_knownTypes.TryGetValue(slotName, out var info))
                        netType = info.NetType;

                    string pascalClassName = ToPascalCase(slotName);
                    string parseCode = GetParseExpression(netType, rawVarName);

                    // Пример: var slot1 = new VarName { Value = raw1 };
                    typedVars.Add($"var {typedVarName} = new {pascalClassName} {{ Value = {parseCode} }};");
                    constructorArgs.Add(typedVarName);
                }
            }

            foreach (var rv in rawVars)
                sb.AppendLine("            " + rv);
            foreach (var tv in typedVars)
                sb.AppendLine("            " + tv);

            sb.AppendLine($"            return new {rule.RuleName}({string.Join(", ", constructorArgs)});");
            sb.AppendLine("        }");
            return sb.ToString();
        }

        private string GetParseExpression(string netType, string rawVarName)
        {
            switch (netType.ToLower())
            {
                case "int":
                    return $"int.Parse({rawVarName})";
                case "bool":
                    return $"bool.Parse({rawVarName})";
                case "double":
                    return $"double.Parse({rawVarName}, CultureInfo.InvariantCulture)";
                default:
                    return rawVarName;
            }
        }

        private string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}
