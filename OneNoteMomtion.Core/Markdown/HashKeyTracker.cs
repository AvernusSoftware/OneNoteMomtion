namespace OneNoteMomtion.Core.Markdown
{
    public sealed class HashKeyTracker
    {
        public const char Hash = '#';

        private bool _lastWasHash;

        public bool ArmedByHash => _lastWasHash;

        public bool OnCharacter(char character)
        {
            if (character == Hash)
            {
                _lastWasHash = true;
                return false;
            }

            bool triggered = _lastWasHash && character == ' ';
            _lastWasHash = false;
            return triggered;
        }

        public void Reset()
        {
            _lastWasHash = false;
        }
    }
}
