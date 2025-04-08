using System;
using System.Collections.Generic;
using System.Linq;

namespace ParserRulesGenerator
{
    /// <summary>
    /// Класс для проверки корректности описанной грамматики:
    /// - проверяем, что все ссылки на типы существуют
    /// - проверяем прямую левую рекурсию
    /// - запрещаем определять тип или правило с именем "Expression" (reserved)
    /// </summary>
    public class GrammarValidator
    {
        private readonly Dictionary<string, (string NetType, string Pattern)> _knownTypes;
        private readonly List<GrammarRule> _rules;

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
        }

        /// <summary>
        /// Проверяем, что у каждого RULE есть непустое имя.
        /// </summary>
        private void CheckMissingRuleNames()
        {
            foreach (var rule in _rules)
            {
                if (string.IsNullOrWhiteSpace(rule.RuleName))
                {
                    throw new Exception($"Правило без имени: {rule.RuleBody}");
                }
            }
        }

        /// <summary>
        /// Проверяем, что тип или правило с именем "Expression" не определены,
        /// так как этот слот зарезервирован для обработки выражений.
        /// </summary>
        private void CheckReservedExpressionType()
        {
            if (_knownTypes.ContainsKey("Expression"))
            {
                throw new Exception("Тип 'Expression' зарезервирован и не может быть определен в разделе TYPE:. Используйте специальный слот <Expression> вместо этого.");
            }
            if (_rules.Any(r => r.RuleName.Equals("Expression", StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception("Правило с именем 'Expression' зарезервировано и не может быть определено. Используйте специальный слот <Expression> вместо этого.");
            }
        }

        /// <summary>
        /// Проверяем, что все <slots> ссылаются либо на известные TYPE:, либо на другие RULE:,
        /// за исключением спецслота <Expression>, который игнорируем.
        /// </summary>
        private void CheckSlotReferences()
        {
            foreach (var rule in _rules)
            {
                foreach (var slot in rule.Slots)
                {
                    // Слот может быть, например, "varName|intValue".
                    // Здесь проверяем каждую альтернативу.
                    var variants = slot.Split('|');
                    foreach (var variant in variants)
                    {
                        var trimmed = variant.Trim();

                        // Спецслот <Expression> разрешён.
                        if (trimmed.Equals("Expression", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Проверяем, определён ли такой тип в TYPE: или RULE:
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
        /// Упрощённая проверка на прямую левую рекурсию:
        /// если правило называется Expr, а первый слот равен Expr, то это левая рекурсия.
        /// </summary>
        private void CheckDirectLeftRecursion()
        {
            foreach (var rule in _rules)
            {
                if (!rule.Slots.Any())
                {
                    continue;
                }
                var firstSlot = rule.Slots.First();
                if (string.Equals(firstSlot, rule.RuleName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"Левая рекурсия в правиле {rule.RuleName}: первый слот совпадает с именем правила");
                }
            }
        }
    }
}
