using NUnit.Framework;
using OneNoteMomtion.Core.Text;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class WhitespaceTransformerTests
    {
        private const char NoBreakSpace = (char)0x00A0;

        private static TextCleanupOptions Only(string rule)
        {
            TextCleanupOptions options = new()
            {
                TrimTrailing = false,
                CollapseSpaces = false,
                NormalizeListMarkers = false,
                SpaceAroundBrackets = false,
                NormalizeNonBreakingSpaces = false,
                NormalizeDashes = false,
                NormalizeQuotes = false,
            };

            switch (rule)
            {
                case nameof(TextCleanupOptions.TrimTrailing):
                    options.TrimTrailing = true;
                    break;
                case nameof(TextCleanupOptions.CollapseSpaces):
                    options.CollapseSpaces = true;
                    break;
                case nameof(TextCleanupOptions.NormalizeListMarkers):
                    options.NormalizeListMarkers = true;
                    break;
                case nameof(TextCleanupOptions.SpaceAroundBrackets):
                    options.SpaceAroundBrackets = true;
                    break;
                case nameof(TextCleanupOptions.NormalizeNonBreakingSpaces):
                    options.NormalizeNonBreakingSpaces = true;
                    break;
                case nameof(TextCleanupOptions.NormalizeDashes):
                    options.NormalizeDashes = true;
                    break;
                case nameof(TextCleanupOptions.NormalizeQuotes):
                    options.NormalizeQuotes = true;
                    break;
            }

            return options;
        }

        private static string Apply(string text, TextCleanupOptions options, bool isStart = true, bool isEnd = true, char? previous = null)
        {
            return WhitespaceTransformer.Apply(text, options, isStart, isEnd, previous);
        }

        [Test]
        public void TrimTrailing_RemovesSpacesAndTabsOnlyAtTheEndOfTheParagraph()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.TrimTrailing));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply("text  \t ", options), Is.EqualTo("text"));
                Assert.That(Apply("text  ", options, isEnd: false), Is.EqualTo("text  "));
            }
        }

        [Test]
        public void CollapseSpaces_SqueezesRunsButLeavesASingleSpaceAndASingleTabAlone()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.CollapseSpaces));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply("a    b", options), Is.EqualTo("a b"));
                Assert.That(Apply("a b", options), Is.EqualTo("a b"));
                Assert.That(Apply("a\tb", options), Is.EqualTo("a\tb"));
            }
        }

        [Test]
        public void CollapseSpaces_SeesTheSpaceTheStretchBeforeItAlreadyProduced()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.CollapseSpaces));

            Assert.That(Apply(" b", options, isStart: false, previous: ' '), Is.EqualTo("b"));
        }

        [Test]
        public void NormalizeListMarkers_LeavesASingleSpaceAfterTheMarker()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.NormalizeListMarkers));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply("*    text", options), Is.EqualTo("* text"));
                Assert.That(Apply("-\t\ttext", options), Is.EqualTo("- text"));
                Assert.That(Apply("1.   text", options), Is.EqualTo("1. text"));
                Assert.That(Apply("12)  text", options), Is.EqualTo("12) text"));
            }
        }

        [Test]
        public void NormalizeListMarkers_DropsTheIndentationBeforeAMarker()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.NormalizeListMarkers));

            Assert.That(Apply("   *  text", options), Is.EqualTo("* text"));
        }

        [Test]
        public void NormalizeListMarkers_IgnoresPunctuationThatOnlyLooksLikeAMarker()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.NormalizeListMarkers));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply("*emphasis*", options), Is.EqualTo("*emphasis*"));
                Assert.That(Apply("*    text", options, isStart: false), Is.EqualTo("*    text"));
            }
        }

        [Test]
        public void SpaceAroundBrackets_SeparatesTheAsideFromTheWordsAroundIt()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.SpaceAroundBrackets));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply("tekst(uwaga)tekst", options), Is.EqualTo("tekst (uwaga) tekst"));
                Assert.That(Apply("tekst[1]dalej", options), Is.EqualTo("tekst [1] dalej"));
                Assert.That(Apply("tekst{a}b", options), Is.EqualTo("tekst {a} b"));
            }
        }

        [Test]
        public void SpaceAroundBrackets_NeverPutsASpaceInsideThePair()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.SpaceAroundBrackets));

            Assert.That(Apply("a (uwaga) b", options), Is.EqualTo("a (uwaga) b"));
        }

        [Test]
        public void SpaceAroundBrackets_LeavesPunctuationAroundTheBracketsAlone()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.SpaceAroundBrackets));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply("koniec (uwaga).", options), Is.EqualTo("koniec (uwaga)."));
                Assert.That(Apply("lista (a), (b)!", options), Is.EqualTo("lista (a), (b)!"));
                Assert.That(Apply("cytat \"(uwaga)\"", options), Is.EqualTo("cytat \"(uwaga)\""));
            }
        }

        [Test]
        public void SpaceAroundBrackets_LeavesASadEmoticonIntact()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.SpaceAroundBrackets));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply("no dobrze :(", options), Is.EqualTo("no dobrze :("));
                Assert.That(Apply("swietnie :)", options), Is.EqualTo("swietnie :)"));
            }
        }

        [Test]
        public void SpaceAroundBrackets_TreatsAnEmptyPairAsPartOfTheNameBeforeIt()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.SpaceAroundBrackets));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply("wywolaj main()", options), Is.EqualTo("wywolaj main()"));
                Assert.That(Apply("metoda Foo()zwraca", options), Is.EqualTo("metoda Foo() zwraca"));
            }
        }

        [Test]
        public void SpaceAroundBrackets_SeparatesTwoPairsThatTouch()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.SpaceAroundBrackets));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply("(a)(b)", options), Is.EqualTo("(a) (b)"));
                Assert.That(Apply("((zagniezdzone))", options), Is.EqualTo("((zagniezdzone))"));
            }
        }

        [Test]
        public void SpaceAroundBrackets_SeesTheCharacterTheStretchBeforeItEndedWith()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.SpaceAroundBrackets));

            Assert.That(Apply("(uwaga)", options, isStart: false, previous: 't'), Is.EqualTo(" (uwaga)"));
        }

        [Test]
        public void NormalizeNonBreakingSpaces_TurnsThemIntoOrdinarySpaces()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.NormalizeNonBreakingSpaces));

            Assert.That(Apply("a" + NoBreakSpace + "b", options), Is.EqualTo("a b"));
        }

        [Test]
        public void NormalizeNonBreakingSpaces_LeavesThemWhenTheRuleIsOff()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.CollapseSpaces));

            Assert.That(Apply("a" + NoBreakSpace + NoBreakSpace + "b", options), Is.EqualTo("a" + NoBreakSpace + NoBreakSpace + "b"));
        }

        [Test]
        public void NormalizeDashes_ReplacesExactlyTwoHyphensAndSparesASeparatorLine()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.NormalizeDashes));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply("a -- b", options), Is.EqualTo("a – b"));
                Assert.That(Apply("-----", options), Is.EqualTo("-----"));
                Assert.That(Apply("well-known", options), Is.EqualTo("well-known"));
            }
        }

        [Test]
        public void NormalizeQuotes_PutsEveryTypographicQuoteBackToAStraightOne()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.NormalizeQuotes));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply("to jest „cytat” tutaj", options), Is.EqualTo("to jest \"cytat\" tutaj"));
                Assert.That(Apply("this is a “quote” here", options), Is.EqualTo("this is a \"quote\" here"));
                Assert.That(Apply("don’t", options), Is.EqualTo("don't"));
                Assert.That(Apply("‚single‘ and ‘other’", options), Is.EqualTo("'single' and 'other'"));
            }
        }

        [Test]
        public void NormalizeQuotes_LeavesStraightQuotesExactlyAsTheyAre()
        {
            TextCleanupOptions options = Only(nameof(TextCleanupOptions.NormalizeQuotes));

            Assert.That(Apply("a \"quote\" and don't", options), Is.EqualTo("a \"quote\" and don't"));
        }

        [Test]
        public void Apply_LeavesTextThatNeedsNothingUntouched()
        {
            TextCleanupOptions options = new();

            Assert.That(Apply("A tidy sentence.", options), Is.EqualTo("A tidy sentence."));
        }
    }
}
