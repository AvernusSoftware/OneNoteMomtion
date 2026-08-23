using System;
using System.Collections.Generic;

namespace OneNoteMomtion.Core.Highlighting.Detection
{
    public sealed class LanguageSignature
    {
        private const double Saturation = 3.0;

        private readonly IReadOnlyList<LanguageSignatureRule> _rules;
        private readonly Func<string, bool>? _structureTest;
        private readonly double _structureWeight;

        public string LanguageId { get; }

        public LanguageSignature(string languageId, IReadOnlyList<LanguageSignatureRule> rules, Func<string, bool>? structureTest = null, double structureWeight = 0.0)
        {
            if (string.IsNullOrWhiteSpace(languageId))
            {
                throw new ArgumentException("A signature needs a language id.", nameof(languageId));
            }

            LanguageId = languageId;
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _structureTest = structureTest;
            _structureWeight = structureWeight;
        }

        public double Score(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return 0.0;
            }

            double weight = 0.0;

            foreach (LanguageSignatureRule rule in _rules)
            {
                weight += rule.Contribution(code);
            }

            if (_structureTest != null && _structureTest(code))
            {
                weight += _structureWeight;
            }

            if (weight <= 0.0)
            {
                return 0.0;
            }

            return Math.Min(1.0, weight / Saturation);
        }
    }
}