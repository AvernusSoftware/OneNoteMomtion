using NUnit.Framework;
using OneNoteMomtion.Core.Markdown;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class HashKeyTrackerTests
    {
        private static bool Type(HashKeyTracker tracker, string characters)
        {
            bool triggered = false;

            foreach (char character in characters)
            {
                triggered = tracker.OnCharacter(character);
            }

            return triggered;
        }

        [Test]
        public void Space_AfterAHash_Triggers()
        {
            Assert.That(Type(new HashKeyTracker(), "# "), Is.True);
        }

        [Test]
        public void Space_AfterSeveralHashes_Triggers()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Type(new HashKeyTracker(), "## "), Is.True);
                Assert.That(Type(new HashKeyTracker(), "### "), Is.True);
            }
        }

        [Test]
        public void Space_AfterFourHashes_StillTriggers()
        {
            Assert.That(Type(new HashKeyTracker(), "#### "), Is.True);
        }

        [Test]
        public void Space_AfterAHashGluedToOtherText_Triggers()
        {
            Assert.That(Type(new HashKeyTracker(), "C# "), Is.True);
        }

        [Test]
        public void Space_AfterAnyOtherCharacter_DoesNotTrigger()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Type(new HashKeyTracker(), "#a "), Is.False);
                Assert.That(Type(new HashKeyTracker(), "abc "), Is.False);
            }
        }

        [Test]
        public void SecondSpace_DoesNotTriggerAgain()
        {
            HashKeyTracker tracker = new();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Type(tracker, "# "), Is.True);
                Assert.That(tracker.OnCharacter(' '), Is.False);
            }
        }

        [Test]
        public void Reset_ClearsAPendingHash()
        {
            HashKeyTracker tracker = new();

            tracker.OnCharacter('#');
            Assert.That(tracker.ArmedByHash, Is.True);

            tracker.Reset();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tracker.ArmedByHash, Is.False);
                Assert.That(tracker.OnCharacter(' '), Is.False);
            }
        }
    }
}
