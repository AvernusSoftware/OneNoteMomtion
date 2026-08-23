namespace OneNoteMomtion.Core.Highlighting.Detection
{
    public sealed class LanguageDetectionResult
    {
        private LanguageDetectionResult(string? languageId, double score, double margin)
        {
            LanguageId = languageId;
            Score = score;
            Margin = margin;
        }

        public string? LanguageId { get; }

        public double Score { get; }

        public double Margin { get; }

        public bool Recognised => LanguageId != null;

        public static LanguageDetectionResult Match(string languageId, double score, double margin)
        {
            return new LanguageDetectionResult(languageId, score, margin);
        }

        public static LanguageDetectionResult Unknown(double score = 0.0, double margin = 0.0)
        {
            return new LanguageDetectionResult(null, score, margin);
        }
    }
}