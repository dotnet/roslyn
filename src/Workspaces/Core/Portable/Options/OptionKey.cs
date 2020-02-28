// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if CODE_STYLE
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#endif

namespace Microsoft.CodeAnalysis.Options
{
    public readonly struct OptionKey : IEquatable<OptionKey>
    {
        public IOption Option { get; }
        public string Language { get; }

        public OptionKey(IOption option, string language = null)
        {
            if (language != null && !option.IsPerLanguage)
            {
                throw new ArgumentException(WorkspacesResources.A_language_name_cannot_be_specified_for_this_option);
            }
            else if (language == null && option.IsPerLanguage)
            {
                throw new ArgumentNullException(WorkspacesResources.A_language_name_must_be_specified_for_this_option);
            }

            this.Option = option ?? throw new ArgumentNullException(nameof(option));
            this.Language = language;
        }

        public override bool Equals(object obj)
        {
            return obj is OptionKey key &&
                   Equals(key);
        }

        public bool Equals(OptionKey other)
        {
            return Option == other.Option && Language == other.Language;
        }

        public override int GetHashCode()
        {
            var hash = Option?.GetHashCode() ?? 0;

            if (Language != null)
            {
                hash = unchecked((hash * (int)0xA5555529) + Language.GetHashCode());
            }

            return hash;
        }

        public override string ToString()
        {
            if (Option is null)
            {
                return "";
            }

            var languageDisplay = Option.IsPerLanguage
                ? $"({Language}) "
                : string.Empty;

            return languageDisplay + Option.ToString();
        }

        public static bool operator ==(OptionKey left, OptionKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OptionKey left, OptionKey right)
        {
            return !left.Equals(right);
        }
    }
}
