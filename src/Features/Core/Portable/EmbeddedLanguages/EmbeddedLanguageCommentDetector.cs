// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages
{
    /// <summary>
    /// Helps match patterns of the form: language=name,option1,option2,option3
    /// <para/>
    /// All matching is case insensitive, with spaces allowed between the punctuation. Option values are
    /// returned as strings.
    /// <para/>
    /// Option names are the values from the TOptions enum.
    /// </summary>
    internal readonly struct EmbeddedLanguageCommentDetector
    {
        private readonly Regex _regex;

        private const int MaxRegexCacheSize = 11;
        private static readonly Dictionary<string, Regex> s_regexes = new(MaxRegexCacheSize);
        private static readonly object s_lock = new();

        public EmbeddedLanguageCommentDetector(ImmutableArray<string> identifiers)
        {
            var namePortion = string.Join("|", identifiers.Select(n => $"({Regex.Escape(n)})"));

            lock (s_lock)
            {
                if (!s_regexes.TryGetValue(namePortion, out var regex))
                {
                    // Cap number of cached regular expressions to ensure we don't have unbounded growth
                    if (s_regexes.Count == MaxRegexCacheSize)
                        s_regexes.Clear();

                    regex = new Regex($@"^((//)|(')|(/\*))\s*lang(uage)?\s*=\s*(?<identifier>{namePortion})\b((\s*,\s*)(?<option>[a-zA-Z]+))*",
                        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    s_regexes.Add(namePortion, regex);
                }

                _regex = regex;
            }
        }

        public bool TryMatch(
            string text,
            [NotNullWhen(true)] out string? identifier,
            [NotNullWhen(true)] out IEnumerable<string>? options)
        {
            var match = _regex.Match(text);
            identifier = null;
            options = null;
            if (!match.Success)
                return false;

            identifier = match.Groups["identifier"].Value;

            var optionGroup = match.Groups["option"];
#if NETCOREAPP
            options = optionGroup.Captures.Select(c => c.Value);
#else
            options = optionGroup.Captures.OfType<Capture>().Select(c => c.Value);
#endif
            return true;
        }
    }
}
