// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

/// <inheritdoc cref="OptionKey2"/>
[NonDefaultable]
public readonly record struct OptionKey
{
    /// <inheritdoc cref="OptionKey2.Option"/>
    public IOption Option { get; }

    /// <inheritdoc cref="OptionKey2.Language"/>
    public string? Language { get; }

    public OptionKey(IOption option, string? language = null)
    {
        if (option is null)
        {
            throw new ArgumentNullException(nameof(option));
        }

        if (language != null && !option.IsPerLanguage)
        {
            throw new ArgumentException(WorkspacesResources.A_language_name_cannot_be_specified_for_this_option);
        }

        if (language == null && option.IsPerLanguage)
        {
            throw new ArgumentNullException(WorkspacesResources.A_language_name_must_be_specified_for_this_option);
        }

        Option = option;
        Language = language;
    }

    public bool Equals(OptionKey other)
    {
        return OptionEqual(Option, other.Option) && Language == other.Language;

        static bool OptionEqual(IOption thisOption, IOption otherOption)
        {
            if (thisOption is not IOption2 thisOption2 ||
                otherOption is not IOption2 otherOption2)
            {
                // Third party definition of 'IOption'.
                return thisOption.Equals(otherOption);
            }

            return thisOption2.Equals(otherOption2);
        }
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
}
