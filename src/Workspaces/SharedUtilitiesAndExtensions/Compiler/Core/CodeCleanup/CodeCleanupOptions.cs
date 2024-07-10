// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.OrganizeImports;
#endif

namespace Microsoft.CodeAnalysis.CodeCleanup;

[DataContract]
internal sealed record class CodeCleanupOptions
{
    [DataMember] public required SyntaxFormattingOptions FormattingOptions { get; init; }
    [DataMember] public required SimplifierOptions SimplifierOptions { get; init; }
    [DataMember] public AddImportPlacementOptions AddImportOptions { get; init; } = AddImportPlacementOptions.Default;
    [DataMember] public DocumentFormattingOptions DocumentFormattingOptions { get; init; } = DocumentFormattingOptions.Default;

#if !CODE_STYLE
    public static CodeCleanupOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            FormattingOptions = SyntaxFormattingOptions.GetDefault(languageServices),
            SimplifierOptions = SimplifierOptions.GetDefault(languageServices)
        };

    public OrganizeImportsOptions GetOrganizeImportsOptions()
        => new()
        {
            SeparateImportDirectiveGroups = FormattingOptions.SeparateImportDirectiveGroups,
            PlaceSystemNamespaceFirst = AddImportOptions.PlaceSystemNamespaceFirst,
            NewLine = FormattingOptions.LineFormatting.NewLine,
        };
#endif
}

internal static class CodeCleanupOptionsProviders
{
#if !CODE_STYLE
    public static CodeCleanupOptions GetCodeCleanupOptions(this IOptionsReader options, LanguageServices languageServices, bool? allowImportsInHiddenRegions = null)
        => new()
        {
            FormattingOptions = options.GetSyntaxFormattingOptions(languageServices),
            SimplifierOptions = options.GetSimplifierOptions(languageServices),
            AddImportOptions = options.GetAddImportPlacementOptions(languageServices, allowImportsInHiddenRegions),
            DocumentFormattingOptions = options.GetDocumentFormattingOptions(),
        };

    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetCodeCleanupOptions(document.Project.Services, document.AllowImportsInHiddenRegions());
    }
#endif
}

