using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

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
        /// создаются статические поля Regex и if-блоки.
        /// - Обычные RULE: -> if(...) { ... return new RuleName(...); }
        /// - ERROR: -> if(...) { throw new Exception("Пользовательская ошибка: ..."); }
        /// Порядок: сначала обрабатываем ERROR-правила, затем обычные.
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

                // Создаём поле Regex (используем verbatim literal)
                patternFields.Add(
                    $"    private static readonly Regex {fieldName} = new(@\"{regexPattern}\", RegexOptions.Compiled);"
                );

                // Если правило-ошибка
                if (rule.IsErrorRule)
                {
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
                    string block = BuildIfBlock(rule, fieldName);
                    normalIfBlocks.Add(block);
                }
            }

            foreach (var line in patternFields)
                sb.AppendLine(line);

            sb.AppendLine();
            sb.AppendLine("    public object Parse(string input)");
            sb.AppendLine("    {");

            foreach (var errBlock in errorIfBlocks)
                sb.AppendLine(errBlock);

            foreach (var ruleBlock in normalIfBlocks)
                sb.AppendLine(ruleBlock);

            sb.AppendLine("        throw new ArgumentException($\"Ни один шаблон не подошел для строки: {input}\");");
            sb.AppendLine("    }");

            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Заменяет каждый слот в RuleBody соответствующим Regex-паттерном.
        /// Если слот равен "Expression", генерирует (.+?) (и ожидается, что разбор выражения осуществляется рекурсивным спуском).
        /// </summary>
        private string BuildRegexPattern(string ruleBody)
        {
            var sb = new StringBuilder();
            int curIndex = 0;
            var matches = Regex.Matches(ruleBody, @"<([^>]+)>");
            foreach (Match m in matches)
            {
                string literal = ruleBody.Substring(curIndex, m.Index - curIndex);
                sb.Append(Regex.Escape(literal));

                string slot = m.Groups[1].Value.Trim();
                if (string.Equals(slot, "Expression", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("(.+?)");
                }
                else if (_knownTypes.TryGetValue(slot, out var info))
                {
                    string pattern = info.Pattern.Trim();
                    if (pattern.StartsWith("(") && pattern.EndsWith(")"))
                        pattern = pattern.Substring(1, pattern.Length - 2);
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
        /// Формирует блок if для проверки соответствия шаблона и создания объекта правила.
        /// Для слота, равного "Expression", вызывается ExpressionParser.ParseExpressionNode.
        /// Если слот ссылается на правило (не найден в _knownTypes, но есть среди _rules), то используется raw-значение.
        /// Иначе, обрабатывается как известный тип.
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

                rawVars.Add($"var {rawVarName} = match.Groups[{groupIndex}].Value;");

                if (string.Equals(slotName, "Expression", StringComparison.OrdinalIgnoreCase))
                {
                    // Разбор выражения через рекурсивный спуск
                    typedVars.Add($"var {typedVarName} = ParserRulesGenerator.ExpressionParser.ParseExpressionNode({rawVarName});");
                    constructorArgs.Add(typedVarName);
                }
                else if (!_knownTypes.ContainsKey(slotName) && _rules.Any(r => r.RuleName.Equals(slotName, StringComparison.OrdinalIgnoreCase)))
                {
                    // Если слот ссылается на правило, используем raw-значение (fallback как строка)
                    typedVars.Add($"var {typedVarName} = {rawVarName};");
                    constructorArgs.Add(rawVarName);
                }
                else
                {
                    string netType = "string";
                    if (_knownTypes.TryGetValue(slotName, out var info))
                        netType = info.NetType;

                    string pascalClassName = ToPascalCase(slotName);
                    string parseCode = GetParseExpression(netType, rawVarName);
                    typedVars.Add($"var {typedVarName} = new {pascalClassName} {{ Value = {parseCode} }};");
                    constructorArgs.Add(typedVarName);
                }
            }

            foreach (var r in rawVars)
                sb.AppendLine("            " + r);
            foreach (var t in typedVars)
                sb.AppendLine("            " + t);

            sb.AppendLine($"            return new {rule.RuleName}({string.Join(", ", constructorArgs)});");
            sb.AppendLine("        }");
            return sb.ToString();
        }

        private string GetParseExpression(string netType, string rawVarName)
        {
            switch (netType.ToLower())
            {
                case "int": return $"int.Parse({rawVarName})";
                case "bool": return $"bool.Parse({rawVarName})";
                case "double": return $"double.Parse({rawVarName}, CultureInfo.InvariantCulture)";
                default: return rawVarName;
            }
        }

        private string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}
