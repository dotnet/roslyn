// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    internal struct LanguageCommentDetector<TOptions>
    {
        private readonly Regex _regex;
        private readonly ImmutableArray<string> _allowedOptions;
        private readonly Func<string, (bool, TOptions)> _tryGetOptionValue;
        private readonly Func<TOptions, TOptions, TOptions> _combineOptions;

        public LanguageCommentDetector(
            IEnumerable<string> languageNames, IEnumerable<string> allowedOptions,
            Func<string, (bool, TOptions)> tryGetOptionValue, Func<TOptions, TOptions, TOptions> combineOptions)
        {
            var namePortion = string.Join("|", languageNames.Select(Regex.Escape));

            _regex = new Regex($@"\blang(uage)?\s*=\s*({namePortion})\b((\s*,\s*)(?<option>[a-zA-Z]+))*",
                RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            _allowedOptions = allowedOptions.ToImmutableArray();
            _tryGetOptionValue = tryGetOptionValue;
            _combineOptions = combineOptions;
        }

        public (bool success, TOptions options) TryMatch(string text)
        {
            var match = _regex.Match(text);
            if (!match.Success)
            {
                return default;
            }

            var options = default(TOptions);
            var optionGroup = match.Groups["option"];
            foreach (Capture capture in optionGroup.Captures)
            {
                var (succeeded, specificOption) = _tryGetOptionValue(capture.Value);
                if (!succeeded)
                {
                    // hit something we don't understand.  bail out.  that will help ensure
                    // users don't have weird behavior just because they misspelled something.
                    // instead, they will know they need to fix it up.
                    return default;
                }

                options = _combineOptions(options, specificOption);
            }

            return (true, options);
        }
    }
}
