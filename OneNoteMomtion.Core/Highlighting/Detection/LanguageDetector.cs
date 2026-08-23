using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OneNoteMomtion.Core.Highlighting.Detection
{
    public sealed class LanguageDetector
    {
        private const RegexOptions Loose = RegexOptions.IgnoreCase | RegexOptions.Multiline;
        private const RegexOptions Lines = RegexOptions.Multiline;

        private readonly IReadOnlyList<LanguageSignature> _signatures;

        public LanguageDetector() : this(CreateDefaultSignatures())
        {
        }

        public LanguageDetector(IReadOnlyList<LanguageSignature> signatures)
        {
            _signatures = signatures ?? throw new ArgumentNullException(nameof(signatures));
        }

        public LanguageDetectionResult Detect(string? code)
        {
            return Detect(code, new CodeDetectionOptions());
        }

        public LanguageDetectionResult Detect(string? code, CodeDetectionOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(code))
            {
                return LanguageDetectionResult.Unknown();
            }

            string best = string.Empty;
            double bestScore = 0.0;
            double runnerUpScore = 0.0;

            foreach (LanguageSignature signature in _signatures)
            {
                double score = signature.Score(code!);

                if (score > bestScore)
                {
                    runnerUpScore = bestScore;
                    bestScore = score;
                    best = signature.LanguageId;
                }
                else if (score > runnerUpScore)
                {
                    runnerUpScore = score;
                }
            }

            double margin = bestScore - runnerUpScore;

            if (best.Length == 0 || bestScore < options.MinScore || margin < options.MinMargin)
            {
                return LanguageDetectionResult.Unknown(bestScore, margin);
            }

            return LanguageDetectionResult.Match(best, bestScore, margin);
        }

        public static IReadOnlyList<LanguageSignature> CreateDefaultSignatures()
        {
            return
            [
                new LanguageSignature("json", CreateJsonRules(), LooksLikeJson, 4.0),
                new LanguageSignature("xml", CreateXmlRules(), LooksLikeXml, 4.0),
                new LanguageSignature("sql", CreateSqlRules()),
                new LanguageSignature("powershell", CreatePowerShellRules()),
                new LanguageSignature("csharp", CreateCSharpRules()),
                new LanguageSignature("java", CreateJavaRules()),
                new LanguageSignature("groovy", CreateGroovyRules()),
            ];
        }

        private static IReadOnlyList<LanguageSignatureRule> CreateJsonRules()
        {
            return
            [
                new LanguageSignatureRule("^\\s*[\\{\\[]", 1.0, Lines),
                new LanguageSignatureRule("\"[^\"]+\"\\s*:\\s*", 1.0),
                new LanguageSignatureRule(";\\s*$", -1.0, Lines),
            ];
        }

        private static IReadOnlyList<LanguageSignatureRule> CreateXmlRules()
        {
            return
            [
                new LanguageSignatureRule("^\\s*<\\?xml", 2.0, Lines),
                new LanguageSignatureRule("</\\w[\\w:.-]*>", 1.5),
                new LanguageSignatureRule("<\\w[\\w:.-]*\\s+[\\w:.-]+\\s*=\\s*\"", 1.0),
            ];
        }

        private static IReadOnlyList<LanguageSignatureRule> CreateSqlRules()
        {
            return
            [
                new LanguageSignatureRule("\\bSELECT\\b[\\s\\S]{0,400}?\\bFROM\\b", 2.5, Loose),
                new LanguageSignatureRule("\\b(INSERT\\s+INTO|UPDATE\\s+\\w+\\s+SET|DELETE\\s+FROM)\\b", 2.0, Loose),
                new LanguageSignatureRule("\\bCREATE\\s+(TABLE|VIEW|INDEX|PROCEDURE|FUNCTION)\\b", 2.0, Loose),
                new LanguageSignatureRule("\\b(INNER|LEFT|RIGHT|FULL|CROSS)\\s+JOIN\\b", 1.0, Loose),
                new LanguageSignatureRule("\\bWHERE\\b", 0.5, Loose),
                new LanguageSignatureRule("\\b(GROUP|ORDER)\\s+BY\\b", 1.0, Loose),
            ];
        }

        private static IReadOnlyList<LanguageSignatureRule> CreatePowerShellRules()
        {
            return
            [
                new LanguageSignatureRule("\\b(Get|Set|New|Remove|Write|Import|Export|Invoke|Test|Start|Stop|Select|Where|ForEach)-[A-Z]\\w+", 2.0),
                new LanguageSignatureRule("\\$\\w+\\s*=", 1.5),
                new LanguageSignatureRule("\\bparam\\s*\\(", 1.5, RegexOptions.IgnoreCase),
                new LanguageSignatureRule("\\s-(eq|ne|gt|lt|ge|le|like|match|notlike|contains)\\b", 1.5, RegexOptions.IgnoreCase),
                new LanguageSignatureRule("\\$_\\.", 1.0),
                new LanguageSignatureRule("\\busing\\s+System\\b", -1.5),
            ];
        }

        private static IReadOnlyList<LanguageSignatureRule> CreateCSharpRules()
        {
            return
            [
                new LanguageSignatureRule("\\busing\\s+System(\\.[\\w.]+)?\\s*;", 2.0),
                new LanguageSignatureRule("\\bnamespace\\s+[A-Za-z_][\\w.]*", 1.5),
                new LanguageSignatureRule("\\bConsole\\.(WriteLine|Write|ReadLine)\\b", 1.5),
                new LanguageSignatureRule("\\basync\\s+Task\\b", 1.5),
                new LanguageSignatureRule("\\bvar\\s+\\w+\\s*=", 1.0),
                new LanguageSignatureRule("=>", 0.5),
                new LanguageSignatureRule("\\b(public|private|protected|internal)\\s+(static\\s+)?(readonly\\s+)?string\\b", 1.0),
                new LanguageSignatureRule("\\bSystem\\.out\\.print", -2.0),
                new LanguageSignatureRule("\\bpublic\\s+static\\s+void\\s+main\\s*\\(", -2.0),
                new LanguageSignatureRule("^\\s*import\\s+javax?\\.", -1.5, Lines),
                new LanguageSignatureRule("\\bdef\\s+\\w+\\s*[=(]", -1.5),
            ];
        }

        private static IReadOnlyList<LanguageSignatureRule> CreateJavaRules()
        {
            return
            [
                new LanguageSignatureRule("^\\s*import\\s+javax?\\.", 2.0, Lines),
                new LanguageSignatureRule("\\bSystem\\.out\\.print(ln)?\\b", 2.0),
                new LanguageSignatureRule("\\bpublic\\s+static\\s+void\\s+main\\s*\\(", 2.0),
                new LanguageSignatureRule("@Override\\b", 1.5),
                new LanguageSignatureRule("^\\s*package\\s+[a-z][\\w.]*\\s*;", 1.5, Lines),
                new LanguageSignatureRule("\\bnew\\s+(ArrayList|HashMap|StringBuilder)\\s*<", 1.0),
                new LanguageSignatureRule("\\bString(\\[\\])?\\s+\\w+\\s*=", 1.0),
                new LanguageSignatureRule("\\busing\\s+System(\\.[\\w.]+)?\\s*;", -2.0),
                new LanguageSignatureRule("\\bConsole\\.WriteLine\\b", -1.5),
                new LanguageSignatureRule("\\bdef\\s+\\w+\\s*[=(]", -1.5),
            ];
        }

        private static IReadOnlyList<LanguageSignatureRule> CreateGroovyRules()
        {
            return
            [
                new LanguageSignatureRule("\\bdef\\s+\\w+\\s*[=(]", 2.0),
                new LanguageSignatureRule("@Grab\\b", 2.0),
                new LanguageSignatureRule("\\{\\s*(\\w+\\s*->|it\\b)", 1.5),
                new LanguageSignatureRule("^\\s*println\\b", 1.5, Lines),
                new LanguageSignatureRule("\\.each\\s*\\{", 1.0),
                new LanguageSignatureRule("\\$\\{\\w+", 1.0),
                new LanguageSignatureRule("\\bSystem\\.out\\.print", -1.5),
                new LanguageSignatureRule("\\busing\\s+System(\\.[\\w.]+)?\\s*;", -1.5),
            ];
        }

        private static bool LooksLikeJson(string code)
        {
            string trimmed = code.Trim();

            if (trimmed.Length < 2 || (trimmed[0] != '{' && trimmed[0] != '['))
            {
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(trimmed, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool LooksLikeXml(string code)
        {
            string trimmed = code.Trim();

            if (trimmed.Length < 3 || trimmed[0] != '<')
            {
                return false;
            }

            try
            {
                XDocument.Parse(trimmed, LoadOptions.PreserveWhitespace);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
