namespace ParserRulesGenerator
{
    /// <summary>
    /// Класс для проверки корректности описанной грамматики:
    /// - проверяем, что все ссылки на типы существуют
    /// - проверяем прямую левую рекурсию
    /// - (опционально) можно добавить проверки косвенной рекурсии и т.д.
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
        /// Если что-то не так, бросает Exception (или GrammarException).
        /// </summary>
        public void Validate()
        {
            CheckMissingRuleNames();
            CheckSlotReferences();
            CheckDirectLeftRecursion();
            // Здесь можно добавить и другие проверки
        }

        /// <summary>
        /// Проверяем, что у каждого RULE есть непустое имя
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
        /// Проверяем, что все <slots> ссылаются либо на известные TYPE:, либо на другие RULE: (если это нужно).
        /// </summary>
        private void CheckSlotReferences()
        {
            foreach (var rule in _rules)
            {
                foreach (var slot in rule.Slots)
                {
                    // Слот может быть varName|intValue, split по '|'
                    var variants = slot.Split('|');
                    foreach (var variant in variants)
                    {
                        var trimmed = variant.Trim();
                        // Есть ли такой TYPE: ?
                        bool isKnownType = _knownTypes.ContainsKey(trimmed);
                        // Или есть ли такое RULE: (если в грамматике разрешены нетерминалы как отдельные RULE)
                        bool isRule = _rules.Any(r => r.RuleName.Equals(trimmed, StringComparison.OrdinalIgnoreCase));

                        if (!isKnownType && !isRule)
                        {
                            throw new Exception(
                                $"Слот <{variant}> не найден ни среди TYPE:, ни среди RULE:. " +
                                $"(используется в {rule.RuleName})");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Упрощённая проверка на прямую левую рекурсию:
        /// "RULE: Expr ::= Expr + Term" - если первый слот = Expr
        /// </summary>
        private void CheckDirectLeftRecursion()
        {
            foreach (var rule in _rules)
            {
                if (!rule.Slots.Any())
                {
                    // если вдруг нет слотов, пропустим
                    continue;
                }

                // первый слот
                var firstSlot = rule.Slots.First();
                // Если правило называется Expr, и первый слот = Expr => прямая левая рекурсия
                if (string.Equals(firstSlot, rule.RuleName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"Левая рекурсия в правиле {rule.RuleName}: " +
                                        $"первый слот совпадает с именем правила");
                }
            }
        }
    }
}
