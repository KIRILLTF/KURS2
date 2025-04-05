using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ParserRulesGenerator
{
    public class GrammarRule
    {
        public string RuleName { get; set; }
        public List<string> Slots { get; set; } = new List<string>();
        public string RuleBody { get; set; }

        /// <summary>
        /// Признак, что это правило – правило-ошибка (ERROR:).
        /// Если true, оно не генерирует полноценный класс, а генерирует блок в парсере, выбрасывающий ошибку.
        /// </summary>
        public bool IsErrorRule { get; set; }
    }

    public class ClassCreator
    {
        public Dictionary<string, (string NetType, string Pattern)> KnownTypes { get; private set; }
            = new Dictionary<string, (string NetType, string Pattern)>(StringComparer.OrdinalIgnoreCase);

        public List<GrammarRule> Rules { get; private set; } = new List<GrammarRule>();

        public ClassCreator(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Не найден файл: {filePath}", filePath);

            var lines = File.ReadAllLines(filePath);
            ParseLines(lines);
        }

        private void ParseLines(string[] lines)
        {
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("TYPE:", StringComparison.OrdinalIgnoreCase))
                {
                    // Пример: TYPE: varName ::= string [a-zA-Z_][a-zA-Z0-9_]*
                    var rest = line.Substring("TYPE:".Length).Trim();
                    var parts = rest.Split(new[] { "::=" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var typeName = parts[0].Trim();
                        var rightSide = parts[1].Trim();
                        var match = Regex.Match(rightSide, @"^(\S+)\s+(.+)$");
                        if (match.Success)
                        {
                            var netType = match.Groups[1].Value;
                            var pattern = match.Groups[2].Value;
                            KnownTypes[typeName] = (netType, pattern);
                        }
                    }
                }
                else if (line.StartsWith("RULE:", StringComparison.OrdinalIgnoreCase)
                      || line.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
                {
                    // Отличаемся только флагом IsErrorRule
                    bool isErrorRule = line.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase);

                    // Отрежем "RULE:" или "ERROR:"
                    var prefixToRemove = isErrorRule ? "ERROR:" : "RULE:";
                    var rest = line.Substring(prefixToRemove.Length).Trim();
                    var parts = rest.Split(new[] { "::=" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var ruleName = parts[0].Trim();   // "MissingVariable" или "WhileLoop"
                        var ruleBody = parts[1].Trim();   // "let = <intValue> ;" и т.д.

                        var matches = Regex.Matches(ruleBody, @"<([^>]+)>");
                        var slots = new List<string>();
                        foreach (Match m in matches)
                        {
                            string inside = m.Groups[1].Value;
                            // Запрещаем '|'
                            if (inside.Contains("|"))
                            {
                                throw new NotSupportedException($"Альтернативы <{inside}> не поддерживаются.");
                            }

                            slots.Add(inside.Trim());
                        }

                        var rule = new GrammarRule
                        {
                            RuleName = ruleName,
                            RuleBody = ruleBody,
                            Slots = slots,
                            IsErrorRule = isErrorRule
                        };

                        Rules.Add(rule);
                    }
                }
            }
        }

        /// <summary>
        /// Генерируем код для:
        /// 1) Классов типов (TYPE:)
        /// 2) Классов для обычных правил (RULE:), но не для ERROR:
        /// </summary>
        public string GenerateClasses()
        {
            var sb = new StringBuilder();

            // 1) Типы
            foreach (var kvp in KnownTypes)
            {
                string typeName = kvp.Key;
                var (netType, pattern) = kvp.Value;

                string className = ToPascalCase(typeName);
                string escaped = pattern.Replace("\\", "\\\\");
                sb.AppendLine($"public class {className}");
                sb.AppendLine("{");
                sb.AppendLine($"    public {netType} Value {{ get; set; }}");
                sb.AppendLine();
                sb.AppendLine("    public bool IsMatch(string input)");
                sb.AppendLine("    {");
                sb.AppendLine($"        return System.Text.RegularExpressions.Regex.IsMatch(input, @\"{escaped}\");");
                sb.AppendLine("    }");
                sb.AppendLine("}");
                sb.AppendLine();
            }

            // 2) Обычные правила
            foreach (var rule in Rules.Where(r => !r.IsErrorRule))
            {
                sb.AppendLine($"public class {rule.RuleName}");
                sb.AppendLine("{");

                var propertyLines = new List<string>();
                var ctorParams = new List<string>();
                var ctorAssigns = new List<string>();

                var typeCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < rule.Slots.Count; i++)
                {
                    string slotType = rule.Slots[i];
                    if (!KnownTypes.ContainsKey(slotType))
                    {
                        // fallback
                        string propName = $"UnknownType{i + 1}";
                        propertyLines.Add($"public string {propName} {{ get; }}");
                        ctorParams.Add($"string {propName.ToLower()}");
                        ctorAssigns.Add($"this.{propName} = {propName.ToLower()};");
                    }
                    else
                    {
                        string pascalSlot = ToPascalCase(slotType);
                        if (!typeCount.ContainsKey(slotType)) typeCount[slotType] = 1;
                        else typeCount[slotType]++;

                        int index = typeCount[slotType];
                        string propName = $"{pascalSlot}{index}";
                        propertyLines.Add($"public {pascalSlot} {propName} {{ get; }}");

                        string paramName = char.ToLower(propName[0]) + propName.Substring(1);
                        ctorParams.Add($"{pascalSlot} {paramName}");
                        ctorAssigns.Add($"this.{propName} = {paramName};");
                    }
                }

                foreach (var line in propertyLines)
                {
                    sb.AppendLine($"    {line}");
                }
                sb.AppendLine();

                sb.AppendLine($"    public {rule.RuleName}({string.Join(", ", ctorParams)})");
                sb.AppendLine("    {");
                foreach (var assign in ctorAssigns)
                    sb.AppendLine($"        {assign};");
                sb.AppendLine("    }");

                sb.AppendLine("}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}
