using System;
using System.Text.RegularExpressions;

namespace OneNoteMomtion.Core.Highlighting.Detection
{
    public sealed class LanguageSignatureRule
    {
        public Regex Pattern { get; }

        public double Weight { get; }

        public LanguageSignatureRule(string pattern, double weight, RegexOptions options = RegexOptions.None)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException("A signature rule needs a pattern.", nameof(pattern));
            }

            Pattern = new Regex(pattern, options | RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            Weight = weight;
        }

        public double Contribution(string code)
        {
            return Pattern.IsMatch(code) ? Weight : 0.0;
        }
    }
}
