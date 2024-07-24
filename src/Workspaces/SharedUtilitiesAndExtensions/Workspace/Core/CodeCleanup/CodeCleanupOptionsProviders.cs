using Microsoft.CodeAnalysis.Options;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeCleanup;

internal static class CodeCleanupOptionsProviders
{
    public static CodeCleanupOptions GetCodeCleanupOptions(this IOptionsReader options, LanguageServices languageServices, bool? allowImportsInHiddenRegions = null)
        => new()
        {
            FormattingOptions = options.GetSyntaxFormattingOptions(languageServices),
            SimplifierOptions = options.GetSimplifierOptions(languageServices),
            AddImportOptions = options.GetAddImportPlacementOptions(languageServices, allowImportsInHiddenRegions),
            DocumentFormattingOptions = options.GetDocumentFormattingOptions(),
        };

#if !CODE_STYLE
    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetCodeCleanupOptions(document.Project.Services, document.AllowImportsInHiddenRegions());
    }
#endif
}

