// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Roslyn.Utilities;

#if CODE_STYLE
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#endif

namespace Microsoft.CodeAnalysis.Options
{
    [NonDefaultable]
    internal readonly partial struct OptionKey2 : IEquatable<OptionKey2>
    {
        public IOption2 Option { get; }
        public string? Language { get; }

        public OptionKey2(IOption2 option, string? language = null)
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

        public override bool Equals(object? obj)
        {
            return obj is OptionKey2 key &&
                   Equals(key);
        }

        public bool Equals(OptionKey2 other)
            => Option.Equals(other.Option) && Language == other.Language;

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

        public static bool operator ==(OptionKey2 left, OptionKey2 right)
            => left.Equals(right);

        public static bool operator !=(OptionKey2 left, OptionKey2 right)
            => !left.Equals(right);
    }
}
