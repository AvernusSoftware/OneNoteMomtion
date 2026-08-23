using NUnit.Framework;
using OneNoteMomtion.Core.Input;

namespace OneNoteMomtion.Core.Tests
{
    [TestFixture]
    public class HotkeyComboTests
    {
        [TestCase("Ctrl+Space", HotkeyModifiers.Control, 0x20u)]
        [TestCase("ctrl+space", HotkeyModifiers.Control, 0x20u)]
        [TestCase("Ctrl+Alt+P", HotkeyModifiers.Control | HotkeyModifiers.Alt, (uint)'P')]
        [TestCase("Shift+F5", HotkeyModifiers.Shift, 0x74u)]
        [TestCase("Win+Ctrl+Shift+9", HotkeyModifiers.Win | HotkeyModifiers.Control | HotkeyModifiers.Shift, (uint)'9')]
        public void TryParse_AcceptsValidCombinations(string text, HotkeyModifiers modifiers, uint virtualKey)
        {
            bool parsed = HotkeyCombo.TryParse(text, out HotkeyCombo combo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.True);
                Assert.That(combo.Modifiers, Is.EqualTo(modifiers));
                Assert.That(combo.VirtualKey, Is.EqualTo(virtualKey));
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("Space")]
        [TestCase("Ctrl")]
        [TestCase("Ctrl+")]
        [TestCase("Ctrl+Space+P")]
        [TestCase("Ctrl+Nonsense")]
        public void TryParse_RejectsInvalidCombinations(string? text)
        {
            bool parsed = HotkeyCombo.TryParse(text, out HotkeyCombo combo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.False);
                Assert.That(combo, Is.Default);
            }
        }

        [Test]
        public void ToString_RebuildsAReadableCombination()
        {
            HotkeyCombo combo = new(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x20);

            Assert.That(combo.ToString(), Is.EqualTo("Ctrl+Alt+Space"));
        }

        [Test]
        public void TryParse_IsCaseInsensitiveForKeyNames()
        {
            bool parsed = HotkeyCombo.TryParse("CTRL+SPACE", out HotkeyCombo combo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.True);
                Assert.That(combo.Modifiers, Is.EqualTo(HotkeyModifiers.Control));
                Assert.That(combo.VirtualKey, Is.EqualTo(0x20u));
            }
        }
    }
}
