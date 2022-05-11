// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal abstract class SyntaxFormattingOptions
{
    [DataMember(Order = 0)]
    public LineFormattingOptions LineFormatting { get; init; } = LineFormattingOptions.Default;

    [DataMember(Order = 1)]
    public bool SeparateImportDirectiveGroups { get; init; } = false;

    [DataMember(Order = 2)]
    public AccessibilityModifiersRequired AccessibilityModifiersRequired { get; init; } = DefaultAccessibilityModifiersRequired;

    protected const int BaseMemberCount = 3;

    public const bool DefaultSeparateImportDirectiveGroups = false;
    public const AccessibilityModifiersRequired DefaultAccessibilityModifiersRequired = AccessibilityModifiersRequired.ForNonInterfaceMembers;

    public abstract SyntaxFormattingOptions With(LineFormattingOptions lineFormatting);

    public bool UseTabs => LineFormatting.UseTabs;
    public int TabSize => LineFormatting.TabSize;
    public int IndentationSize => LineFormatting.IndentationSize;
    public string NewLine => LineFormatting.NewLine;

#if !CODE_STYLE
    public static SyntaxFormattingOptions GetDefault(HostLanguageServices languageServices)
        => languageServices.GetRequiredService<ISyntaxFormattingService>().DefaultOptions;
#endif
}

internal interface SyntaxFormattingOptionsProvider :
#if !CODE_STYLE
    OptionsProvider<SyntaxFormattingOptions>,
#endif
    LineFormattingOptionsProvider
{
}

#if !CODE_STYLE
internal static partial class SyntaxFormattingOptionsProviders
{
    public static async ValueTask<SyntaxFormattingOptions> GetSyntaxFormattingOptionsAsync(this Document document, SyntaxFormattingOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        var formattingService = document.Project.LanguageServices.GetRequiredService<ISyntaxFormattingService>();
        return formattingService.GetFormattingOptions(configOptions, fallbackOptions);
    }

    public static async ValueTask<SyntaxFormattingOptions> GetSyntaxFormattingOptionsAsync(this Document document, SyntaxFormattingOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetSyntaxFormattingOptionsAsync(document, await ((OptionsProvider<SyntaxFormattingOptions>)fallbackOptionsProvider).GetOptionsAsync(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
}
#endif
