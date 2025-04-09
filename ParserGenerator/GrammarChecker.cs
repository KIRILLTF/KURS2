using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ParserRulesGenerator
{
    /// <summary>
    /// Класс для проверки корректности описанной грамматики:
    /// - проверяем, что все ссылки на типы существуют,
    /// - проверяем прямую левую рекурсию,
    /// - запрещаем определять тип или правило с именем "Expression" (зарезервировано),
    /// - запрещаем алгебраические выражения между двумя слотами <Expression> 
    ///   (то есть, если между двумя <Expression> находится символ из набора "+", "-", "*", "/", "^").
    /// </summary>
    public class GrammarValidator
    {
        private readonly Dictionary<string, (string NetType, string Pattern)> _knownTypes;
        private readonly List<GrammarRule> _rules;
        // Массив операторов, которые считаем алгебраическими.
        private readonly char[] algebraicOperators = new char[] { '+', '-', '*', '/', '^' };

        public GrammarValidator(
            Dictionary<string, (string NetType, string Pattern)> knownTypes,
            List<GrammarRule> rules)
        {
            _knownTypes = knownTypes;
            _rules = rules;
        }

        /// <summary>
        /// Запускает все проверки.
        /// Если что-то не так, бросает Exception.
        /// </summary>
        public void Validate()
        {
            CheckMissingRuleNames();
            CheckReservedExpressionType();
            CheckSlotReferences();
            CheckDirectLeftRecursion();
            CheckAlgebraicExpressionsBetweenExpressions();
        }

        /// <summary>
        /// Проверяем, что у каждого RULE есть непустое имя.
        /// </summary>
        private void CheckMissingRuleNames()
        {
            foreach (var rule in _rules)
            {
                if (string.IsNullOrWhiteSpace(rule.RuleName))
                    throw new Exception($"Правило без имени: {rule.RuleBody}");
            }
        }

        /// <summary>
        /// Запрещаем определять тип или правило с именем "Expression".
        /// </summary>
        private void CheckReservedExpressionType()
        {
            if (_knownTypes.ContainsKey("Expression"))
                throw new Exception("Тип 'Expression' зарезервирован и не может быть определён в разделе TYPE:. Используйте слот <Expression>.");

            if (_rules.Any(r => r.RuleName.Equals("Expression", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Правило с именем 'Expression' зарезервировано и не может быть определено. Используйте слот <Expression> вместо этого.");
        }

        /// <summary>
        /// Проверяем, что все <slots> ссылаются либо на известные типы, либо на существующие правила.
        /// Спецслот <Expression> игнорируется.
        /// </summary>
        private void CheckSlotReferences()
        {
            foreach (var rule in _rules)
            {
                foreach (var slot in rule.Slots)
                {
                    var variants = slot.Split('|');
                    foreach (var variant in variants)
                    {
                        var trimmed = variant.Trim();
                        if (trimmed.Equals("Expression", StringComparison.OrdinalIgnoreCase))
                            continue;

                        bool isKnownType = _knownTypes.ContainsKey(trimmed);
                        bool isRule = _rules.Any(r => r.RuleName.Equals(trimmed, StringComparison.OrdinalIgnoreCase));

                        if (!isKnownType && !isRule)
                        {
                            throw new Exception(
                                $"Слот <{variant}> не найден ни среди TYPE:, ни среди RULE:. (используется в {rule.RuleName})");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Проверка прямой левой рекурсии.
        /// Если первое вхождение слота совпадает с именем правила, выдаём ошибку.
        /// </summary>
        private void CheckDirectLeftRecursion()
        {
            foreach (var rule in _rules)
            {
                if (!rule.Slots.Any())
                    continue;
                var firstSlot = rule.Slots.First();
                if (string.Equals(firstSlot, rule.RuleName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"Левая рекурсия в правиле {rule.RuleName}: первый слот совпадает с именем правила");
                }
            }
        }

        /// <summary>
        /// Проверяет, что если в буквальных частях между слотами <Expression> встречаются алгебраические операторы,
        /// то правило не содержит более одного вхождения <Expression>.
        /// Если между двумя <Expression> присутствует хотя бы один оператор из: +, -, *, /, ^, то это запрещено.
        /// </summary>
        private void CheckAlgebraicExpressionsBetweenExpressions()
        {
            foreach (var rule in _rules)
            {
                // Получаем все индексы вхождения слота "<...>"
                var matches = Regex.Matches(rule.RuleBody, @"<([^>]+)>");
                // Собираем номера (индексы) для тех, что равны "Expression" (игнорируя регистр)
                List<(int Start, int End)> exprPositions = new List<(int, int)>();
                foreach (Match m in matches)
                {
                    string slot = m.Groups[1].Value.Trim();
                    if (slot.Equals("Expression", StringComparison.OrdinalIgnoreCase))
                    {
                        exprPositions.Add((m.Index, m.Index + m.Length));
                    }
                }

                // Если менее двух, ничего проверять не надо.
                if (exprPositions.Count < 2)
                    continue;

                // Для каждой пары последовательных вхождений проверяем текст между ними.
                for (int i = 0; i < exprPositions.Count - 1; i++)
                {
                    int start = exprPositions[i].End;
                    int end = exprPositions[i + 1].Start;
                    string between = rule.RuleBody.Substring(start, end - start);
                    // Если между двумя <Expression> встречается алгебраический оператор, выдаём ошибку.
                    if (between.IndexOfAny(algebraicOperators) >= 0)
                    {
                        throw new Exception($"Правило '{rule.RuleName}' содержит алгебраическую операцию между двумя <Expression>. Это запрещено.");
                    }
                }
            }
        }
    }
}
