// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal record class SyntaxFormattingOptions
{
    /// <summary>
    /// Language agnostic defaults.
    /// </summary>
    internal static readonly SyntaxFormattingOptions CommonDefaults = new();

    [DataMember] public LineFormattingOptions LineFormatting { get; init; } = LineFormattingOptions.Default;
    [DataMember] public bool SeparateImportDirectiveGroups { get; init; } = false;
    [DataMember] public AccessibilityModifiersRequired AccessibilityModifiersRequired { get; init; } = AccessibilityModifiersRequired.ForNonInterfaceMembers;
    [DataMember] public int WrappingColumn { get; init; } = 120;
    [DataMember] public int ConditionalExpressionWrappingLength { get; init; } = 120;

    private protected SyntaxFormattingOptions()
    {
    }

    private protected SyntaxFormattingOptions(IOptionsReader options, string language)
    {
        LineFormatting = new LineFormattingOptions(options, language);
        SeparateImportDirectiveGroups = options.GetOption(GenerationOptions.SeparateImportDirectiveGroups, language);
        AccessibilityModifiersRequired = options.GetOptionValue(CodeStyleOptions2.AccessibilityModifiersRequired, language);
        WrappingColumn = options.GetOption(FormattingOptions2.WrappingColumn, language);
        ConditionalExpressionWrappingLength = options.GetOption(FormattingOptions2.ConditionalExpressionWrappingLength, language);
    }

    public bool UseTabs => LineFormatting.UseTabs;
    public int TabSize => LineFormatting.TabSize;
    public int IndentationSize => LineFormatting.IndentationSize;
    public string NewLine => LineFormatting.NewLine;

#if !CODE_STYLE
    public static SyntaxFormattingOptions GetDefault(Host.LanguageServices languageServices)
        => languageServices.GetRequiredService<ISyntaxFormattingService>().DefaultOptions;
#endif
}
