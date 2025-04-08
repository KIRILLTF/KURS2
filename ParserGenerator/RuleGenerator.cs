using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ParserRulesGenerator
{
    public static class Fuzzer
    {
        private static Random _rand = new Random();

        /// <summary>
        /// Генерирует тестовую строку для заданного правила грамматики.
        /// Строка строится из буквальных частей правила и случайно сгенерированных значений для слотов.
        /// </summary>
        public static string FuzzRule(GrammarRule rule, Dictionary<string, (string NetType, string Pattern)> knownTypes)
        {
            var sb = new StringBuilder();
            int lastIndex = 0;
            // Ищем слоты, обрамлённые <...>
            var matches = Regex.Matches(rule.RuleBody, @"<([^>]+)>");
            foreach (Match match in matches)
            {
                int index = match.Index;
                // Добавляем буквальную часть до слота
                sb.Append(rule.RuleBody.Substring(lastIndex, index - lastIndex));

                string slot = match.Groups[1].Value.Trim();
                string fuzzedValue = GenerateValueForSlot(slot, knownTypes);
                sb.Append(fuzzedValue);

                lastIndex = index + match.Length;
            }
            // Добавляем остаток строки (если есть)
            if (lastIndex < rule.RuleBody.Length)
            {
                sb.Append(rule.RuleBody.Substring(lastIndex));
            }
            return sb.ToString();
        }

        private static string GenerateValueForSlot(string slot, Dictionary<string, (string NetType, string Pattern)> knownTypes)
        {
            if (slot.Equals("Expression", StringComparison.OrdinalIgnoreCase))
            {
                // Для слота Expression генерируем случайное арифметическое выражение.
                return GenerateRandomExpression();
            }
            else if (knownTypes.ContainsKey(slot))
            {
                var (netType, _) = knownTypes[slot];
                return GenerateValueForKnownType(netType);
            }
            else
            {
                // Если слот ссылается на правило или неизвестный тип – возвращаем типичное значение.
                return "test";
            }
        }

        private static string GenerateValueForKnownType(string netType)
        {
            switch (netType.ToLower())
            {
                case "int":
                    return _rand.Next(0, 100).ToString();
                case "double":
                    return (_rand.NextDouble() * 100).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                case "bool":
                    return (_rand.Next(0, 2) == 0 ? "true" : "false");
                case "string":
                    return GenerateRandomString(_rand.Next(3, 10));
                default:
                    return GenerateRandomString(_rand.Next(3, 10));
            }
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[_rand.Next(chars.Length)]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Рекурсивно генерирует простое арифметическое выражение.
        /// Ограничение по глубине позволяет избежать слишком длинных выражений.
        /// </summary>
        private static string GenerateRandomExpression(int depth = 0)
        {
            // Ограничиваем глубину рекурсии
            if (depth > 2)
            {
                return _rand.Next(0, 100).ToString();
            }

            // Случайно выбираем тип выражения: число, бинарное выражение или выражение в скобках
            int choice = _rand.Next(0, 3);
            switch (choice)
            {
                case 0:
                    return _rand.Next(0, 100).ToString();
                case 1:
                    string left = GenerateRandomExpression(depth + 1);
                    string right = GenerateRandomExpression(depth + 1);
                    string op = RandomOperator();
                    return $"{left} {op} {right}";
                case 2:
                    string inner = GenerateRandomExpression(depth + 1);
                    return $"({inner})";
                default:
                    return _rand.Next(0, 100).ToString();
            }
        }

        private static string RandomOperator()
        {
            string[] ops = { "+", "-", "*", "/", "^" };
            return ops[_rand.Next(ops.Length)];
        }
    }
}
