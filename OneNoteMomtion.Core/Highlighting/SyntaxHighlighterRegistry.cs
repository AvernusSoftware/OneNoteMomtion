using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using OneNoteMomtion.Core.Highlighting.Providers;

namespace OneNoteMomtion.Core.Highlighting
{
    public sealed class SyntaxHighlighterRegistry
    {
        private readonly Lock _gate = new();
        private readonly Dictionary<string, ISyntaxHighlightProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

        private readonly List<ISyntaxHighlightProvider> _order = [];

        public static SyntaxHighlighterRegistry CreateDefault()
        {
            SyntaxHighlighterRegistry registry = new();
            registry.Register(new CSharpHighlightProvider());
            registry.Register(new JavaHighlightProvider());
            registry.Register(new GroovyHighlightProvider());
            registry.Register(new SqlHighlightProvider());
            registry.Register(new JsonHighlightProvider());
            registry.Register(new XmlHighlightProvider());
            registry.Register(new PowerShellHighlightProvider());
            registry.Register(new PlainTextHighlightProvider());
            return registry;
        }

        public void Register(ISyntaxHighlightProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            if (string.IsNullOrWhiteSpace(provider.LanguageId))
            {
                throw new ArgumentException("A language must have a non-empty LanguageId.", nameof(provider));
            }

            lock (_gate)
            {
                if (_providers.TryGetValue(provider.LanguageId, out ISyntaxHighlightProvider? existing))
                {
                    _order[_order.IndexOf(existing)] = provider;
                }
                else
                {
                    _order.Add(provider);
                }

                _providers[provider.LanguageId] = provider;
            }
        }

        public bool TryGet(string? languageId, [NotNullWhen(true)] out ISyntaxHighlightProvider? provider)
        {
            provider = null;
            if (string.IsNullOrWhiteSpace(languageId))
            {
                return false;
            }

            lock (_gate)
            {
                return _providers.TryGetValue(languageId!.Trim(), out provider);
            }
        }

        public bool TryGetByDisplayName(string? displayName, [NotNullWhen(true)] out ISyntaxHighlightProvider? provider)
        {
            provider = null;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return false;
            }

            string wanted = displayName!.Trim();

            lock (_gate)
            {
                provider = _order.FirstOrDefault(candidate => string.Equals(candidate.DisplayName, wanted, StringComparison.OrdinalIgnoreCase));
            }

            return provider != null;
        }

        public IReadOnlyList<ISyntaxHighlightProvider> All()
        {
            lock (_gate)
            {
                return [.. _order];
            }
        }

        public IReadOnlyList<ISyntaxHighlightProvider> Selectable()
        {
            lock (_gate)
            {
                return [.. _order.Where(provider => !string.Equals(provider.LanguageId, PlainTextHighlightProvider.Id, StringComparison.OrdinalIgnoreCase))];
            }
        }
    }
}
