// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal abstract class SyntaxFormattingOptions
{
    [DataMember(Order = 0)]
    public readonly LineFormattingOptions LineFormatting;

    [DataMember(Order = 1)]
    public readonly bool SeparateImportDirectiveGroups;

    protected const int BaseMemberCount = 2;

    protected SyntaxFormattingOptions(
        LineFormattingOptions? lineFormatting,
        bool separateImportDirectiveGroups)
    {
        LineFormatting = lineFormatting ?? LineFormattingOptions.Default;
        SeparateImportDirectiveGroups = separateImportDirectiveGroups;
    }

    public abstract SyntaxFormattingOptions With(LineFormattingOptions lineFormatting);

    public bool UseTabs => LineFormatting.UseTabs;
    public int TabSize => LineFormatting.TabSize;
    public int IndentationSize => LineFormatting.IndentationSize;
    public string NewLine => LineFormatting.NewLine;

#if !CODE_STYLE
    public static SyntaxFormattingOptions GetDefault(HostLanguageServices languageServices)
        => languageServices.GetRequiredService<ISyntaxFormattingService>().DefaultOptions;

    public static SyntaxFormattingOptions Create(OptionSet options, SyntaxFormattingOptions? fallbackOptions, HostLanguageServices languageServices)
    {
        var formattingService = languageServices.GetRequiredService<ISyntaxFormattingService>();
        var configOptions = options.AsAnalyzerConfigOptions(languageServices.WorkspaceServices.GetRequiredService<IOptionService>(), languageServices.Language);
        return formattingService.GetFormattingOptions(configOptions, fallbackOptions);
    }
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
        var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
        return SyntaxFormattingOptions.Create(documentOptions, fallbackOptions, document.Project.LanguageServices);
    }

    public static async ValueTask<SyntaxFormattingOptions> GetSyntaxFormattingOptionsAsync(this Document document, SyntaxFormattingOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetSyntaxFormattingOptionsAsync(document, await ((OptionsProvider<SyntaxFormattingOptions>)fallbackOptionsProvider).GetOptionsAsync(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
}
#endif
