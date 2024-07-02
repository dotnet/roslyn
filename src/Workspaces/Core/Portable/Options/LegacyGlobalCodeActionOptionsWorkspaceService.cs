// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Enables legacy APIs to access global options from workspace.
/// </summary>
[ExportWorkspaceService(typeof(ILegacyGlobalCleanCodeGenerationOptionsWorkspaceService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LegacyGlobalCleanCodeGenerationOptionsWorkspaceService(IGlobalOptionService globalOptions) : ILegacyGlobalCleanCodeGenerationOptionsWorkspaceService
{
    public CleanCodeGenerationOptionsProvider Provider { get; } = new ProviderImpl(globalOptions);

    private sealed class ProviderImpl(IOptionsReader options) : CleanCodeGenerationOptionsProvider
    {
        ValueTask<LineFormattingOptions> OptionsProvider<LineFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GetLineFormattingOptions(languageServices.Language));

        ValueTask<DocumentFormattingOptions> OptionsProvider<DocumentFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GetDocumentFormattingOptions());

        ValueTask<SyntaxFormattingOptions> OptionsProvider<SyntaxFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GetSyntaxFormattingOptions(languageServices));

        ValueTask<SimplifierOptions> OptionsProvider<SimplifierOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GetSimplifierOptions(languageServices));

        ValueTask<AddImportPlacementOptions> OptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GetAddImportPlacementOptions(languageServices, allowInHiddenRegions: null));

        ValueTask<CodeCleanupOptions> OptionsProvider<CodeCleanupOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions: null));

        ValueTask<CleanCodeGenerationOptions> OptionsProvider<CleanCodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GetCleanCodeGenerationOptions(languageServices, allowImportsInHiddenRegions: null));

        ValueTask<CodeAndImportGenerationOptions> OptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GetCodeAndImportGenerationOptions(languageServices, allowImportsInHiddenRegions: null));

        ValueTask<CodeGenerationOptions> OptionsProvider<CodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GetCodeGenerationOptions(languageServices));

        ValueTask<NamingStylePreferences> OptionsProvider<NamingStylePreferences>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GetOption(NamingStyleOptions.NamingPreferences, languageServices.Language));
    }
}
