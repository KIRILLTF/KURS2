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
        /// Генерирует код класса Parser в виде одной большой строки.
        /// - Метод Parse возвращает object (не Sentence).
        /// - Для каждого RULE создаются:
        ///    private static readonly Regex <ruleName>Pattern = new(@"^...$", RegexOptions.Compiled);
        /// - if(...) { ... return new RuleName(...); }
        /// </summary>
        public string GenerateParserClass()
        {
            var sb = new StringBuilder();

            // Можно подкорректировать namespace/using под свой проект
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Text.RegularExpressions;");
            sb.AppendLine("using System.Globalization;");
            sb.AppendLine();
            sb.AppendLine("public class Parser");
            sb.AppendLine("{");

            var patternFields = new List<string>();
            var ifStatements = new List<string>();

            foreach (var rule in _rules)
            {
                // Генерируем поле Regex
                string fieldName = char.ToLower(rule.RuleName[0]) + rule.RuleName.Substring(1) + "Pattern";
                string regexPattern = BuildRegexPattern(rule.RuleBody);

                patternFields.Add(
                    $"    private static readonly Regex {fieldName} = new(@\"{regexPattern}\", RegexOptions.Compiled);"
                );

                // Генерируем if-блок внутри метода Parse
                string ifBlock = BuildIfBlock(rule, fieldName);
                ifStatements.Add(ifBlock);
            }

            // 1) Добавим все поля
            foreach (var line in patternFields)
                sb.AppendLine(line);
            sb.AppendLine();

            // 2) Метод Parse
            sb.AppendLine("    public object Parse(string input)");
            sb.AppendLine("    {");

            foreach (var ifPart in ifStatements)
            {
                sb.AppendLine(ifPart);
            }

            sb.AppendLine("        throw new ArgumentException($\"Ни один шаблон не подошел для строки: {input}\");");
            sb.AppendLine("    }");

            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Превращаем RuleBody (например: "while ( <varName> <comparisonOperator> <intValue> )")
        /// в паттерн вида "^\\s*while\\s*\\(\\s*([a-zA-Z_][a-zA-Z0-9_]*)\\s*(==|!=|<|>|<=|>=)\\s*([0-9]+)\\s*\\)\\s*$"
        /// используя info.Pattern из _knownTypes.
        /// </summary>
        private string BuildRegexPattern(string ruleBody)
        {
            var sb = new StringBuilder();
            int curIndex = 0;

            var matches = Regex.Matches(ruleBody, @"<([^>]+)>");

            foreach (Match m in matches)
            {
                // Литерал до текущего <...>
                string literal = ruleBody.Substring(curIndex, m.Index - curIndex);
                sb.Append(Regex.Escape(literal));

                string slot = m.Groups[1].Value.Trim();
                if (slot.Contains("|"))
                {
                    slot = slot.Split('|')[0].Trim();
                }

                if (_knownTypes.TryGetValue(slot, out var info))
                {
                    string pattern = info.Pattern.Trim();

                    // Удалим внешние скобки, если они есть
                    if (pattern.StartsWith("(") && pattern.EndsWith(")"))
                    {
                        pattern = pattern.Substring(1, pattern.Length - 2);
                    }

                    // Теперь безопасно оборачиваем в одну пару скобок
                    sb.Append("(" + pattern + ")");
                }
                else
                {
                    sb.Append("(.+?)");
                }

                curIndex = m.Index + m.Length;
            }

            if (curIndex < ruleBody.Length)
            {
                string tail = ruleBody.Substring(curIndex);
                sb.Append(Regex.Escape(tail));
            }

            return "^\\s*" + sb.ToString() + "\\s*$";
        }

        /// <summary>
        /// Генерирует блок вида:
        /// if (someRulePattern.IsMatch(input))
        /// {
        ///     var match = someRulePattern.Match(input);
        ///     var raw1 = match.Groups[1].Value;
        ///     var slot1 = new VarName { Value = raw1 };
        ///     ...
        ///     return new SomeRuleName(slot1, slot2, ...);
        /// }
        /// </summary>
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

                // var raw1 = match.Groups[1].Value;
                rawVars.Add($"var {rawVarName} = match.Groups[{groupIndex}].Value;");

                // Определяем .NET-тип
                string netType = "string";
                if (_knownTypes.TryGetValue(slotName, out var info))
                {
                    netType = info.NetType; // "int", "bool", "double", "string"...
                }

                // класс (PascalCase). Напр. "VarName", "ComparisonOperator"
                string pascalClassName = ToPascalCase(slotName);

                // Пример: "Value = int.Parse(raw1)" или "Value = raw1"
                string parseCode = GetParseExpression(netType, rawVarName);

                // var slot1 = new VarName { Value = int.Parse(raw1) };
                typedVars.Add($"var {typedVarName} = new {pascalClassName} {{ Value = {parseCode} }};");

                constructorArgs.Add(typedVarName);
            }

            // Сформируем выход
            foreach (var r in rawVars)
                sb.AppendLine("            " + r);
            foreach (var t in typedVars)
                sb.AppendLine("            " + t);

            string argsJoined = string.Join(", ", constructorArgs);
            sb.AppendLine($"            return new {rule.RuleName}({argsJoined});");
            sb.AppendLine("        }");

            return sb.ToString();
        }

        /// <summary>
        /// Возвращает выражение для "Value = ???" внутри сгенерированного кода.
        /// </summary>
        private string GetParseExpression(string netType, string rawVarName)
        {
            // Если netType = "string", мы оставляем строку.
            // Если "int" -> int.Parse(...)
            // Если "bool" -> bool.Parse(...)
            // Если "double" -> double.Parse(..., InvariantCulture)
            switch (netType.ToLower())
            {
                case "int":
                    return $"int.Parse({rawVarName})";
                case "bool":
                    return $"bool.Parse({rawVarName})";
                case "double":
                    return $"double.Parse({rawVarName}, CultureInfo.InvariantCulture)";
                default:
                    // всё остальное => string
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
