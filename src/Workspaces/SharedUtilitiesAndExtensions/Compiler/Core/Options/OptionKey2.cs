// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

#if CODE_STYLE
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#endif

namespace Microsoft.CodeAnalysis.Options;

[NonDefaultable]
internal readonly partial record struct OptionKey2
{
    public IOption2 Option { get; }
    public string? Language { get; }

    public OptionKey2(IOption2 option, string? language)
    {
        Debug.Assert(option.IsPerLanguage == language is not null);

        Option = option;
        Language = language;
    }

    public OptionKey2(IPerLanguageValuedOption option, string language)
    {
        Debug.Assert(option.IsPerLanguage);
        if (language == null)
        {
            throw new ArgumentNullException(CompilerExtensionsResources.A_language_name_must_be_specified_for_this_option);
        }

        this.Option = option ?? throw new ArgumentNullException(nameof(option));
        this.Language = language;
    }

    public OptionKey2(ISingleValuedOption option)
    {
        Debug.Assert(!option.IsPerLanguage);
        this.Option = option ?? throw new ArgumentNullException(nameof(option));
        this.Language = null;
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
