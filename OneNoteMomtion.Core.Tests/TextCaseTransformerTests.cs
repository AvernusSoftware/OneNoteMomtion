using System.Globalization;
using NUnit.Framework;
using OneNoteMomtion.Core.Text;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class TextCaseTransformerTests
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        [TestCase(TextCaseMode.Upper, "Hello World", "HELLO WORLD")]
        [TestCase(TextCaseMode.Lower, "Hello World", "hello world")]
        [TestCase(TextCaseMode.Capitalize, "hello big world", "Hello Big World")]
        [TestCase(TextCaseMode.Capitalize, "HELLO BIG WORLD", "Hello Big World")]
        [TestCase(TextCaseMode.Sentence, "hello world. second one", "Hello world. Second one")]
        [TestCase(TextCaseMode.Toggle, "Hello World", "hELLO wORLD")]
        public void Apply_ProducesExpectedText(TextCaseMode mode, string input, string expected)
        {
            Assert.That(TextCaseTransformer.Apply(input, mode, Invariant), Is.EqualTo(expected));
        }

        [TestCase(TextCaseMode.Upper)]
        [TestCase(TextCaseMode.Lower)]
        [TestCase(TextCaseMode.Capitalize)]
        [TestCase(TextCaseMode.Sentence)]
        [TestCase(TextCaseMode.Toggle)]
        public void Apply_PreservesLength(TextCaseMode mode)
        {
            const string Input = "Strasse i Istanbul: ABC def 123!";

            Assert.That(TextCaseTransformer.Apply(Input, mode, Invariant), Has.Length.EqualTo(Input.Length));
        }

        [Test]
        public void Capitalize_TreatsApostropheAsPartOfWord()
        {
            Assert.That(TextCaseTransformer.Apply("don't stop", TextCaseMode.Capitalize, Invariant), Is.EqualTo("Don't Stop"));
        }

        [Test]
        public void Capitalize_StartsWordAfterOpeningQuote()
        {
            Assert.That(TextCaseTransformer.Apply("say 'hello'", TextCaseMode.Capitalize, Invariant), Is.EqualTo("Say 'Hello'"));
        }

        [Test]
        public void SentenceCase_StartsNewSentenceAfterQuestionAndExclamationMarks()
        {
            Assert.That(TextCaseTransformer.Apply("wHY? bECAUSE! dONE.", TextCaseMode.Sentence, Invariant), Is.EqualTo("Why? Because! Done."));
        }

        [Test]
        public void SentenceCase_SkipsOpeningQuoteWhenLookingForFirstLetter()
        {
            Assert.That(TextCaseTransformer.Apply("\"hello there", TextCaseMode.Sentence, Invariant), Is.EqualTo("\"Hello there"));
        }

        [Test]
        public void SentenceCase_TreatsLineBreakAsSentenceBoundary()
        {
            Assert.That(TextCaseTransformer.Apply("first\nsecond", TextCaseMode.Sentence, Invariant), Is.EqualTo("First\nSecond"));
        }

        [Test]
        public void Apply_ReturnsEmptyForNullInput()
        {
            Assert.That(TextCaseTransformer.Apply(null, TextCaseMode.Upper, Invariant), Is.Empty);
        }
    }
}
