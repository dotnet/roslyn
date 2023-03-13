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
internal sealed class LegacyGlobalCleanCodeGenerationOptionsWorkspaceService : ILegacyGlobalCleanCodeGenerationOptionsWorkspaceService
{
    public CleanCodeGenerationOptionsProvider Provider { get; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LegacyGlobalCleanCodeGenerationOptionsWorkspaceService(IGlobalOptionService globalOptions)
    {
        Provider = new ProviderImpl(globalOptions);
    }

    private sealed class ProviderImpl : CleanCodeGenerationOptionsProvider
    {
        private readonly IOptionsReader _options;

        public ProviderImpl(IOptionsReader options)
            => _options = options;

        ValueTask<LineFormattingOptions> OptionsProvider<LineFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GetLineFormattingOptions(languageServices.Language, fallbackOptions: null));

        ValueTask<DocumentFormattingOptions> OptionsProvider<DocumentFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GetDocumentFormattingOptions(fallbackOptions: null));

        ValueTask<SyntaxFormattingOptions> OptionsProvider<SyntaxFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GetSyntaxFormattingOptions(languageServices, fallbackOptions: null));

        ValueTask<SimplifierOptions> OptionsProvider<SimplifierOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GetSimplifierOptions(languageServices, fallbackOptions: null));

        ValueTask<AddImportPlacementOptions> OptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GetAddImportPlacementOptions(languageServices, allowInHiddenRegions: null, fallbackOptions: null));

        ValueTask<CodeCleanupOptions> OptionsProvider<CodeCleanupOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions: null, fallbackOptions: null));

        ValueTask<CleanCodeGenerationOptions> OptionsProvider<CleanCodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GetCleanCodeGenerationOptions(languageServices, allowImportsInHiddenRegions: null, fallbackOptions: null));

        ValueTask<CodeAndImportGenerationOptions> OptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GetCodeAndImportGenerationOptions(languageServices, allowImportsInHiddenRegions: null, fallbackOptions: null));

        ValueTask<CodeGenerationOptions> OptionsProvider<CodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GetCodeGenerationOptions(languageServices, fallbackOptions: null));

        ValueTask<NamingStylePreferences> OptionsProvider<NamingStylePreferences>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GetOption(NamingStyleOptions.NamingPreferences, languageServices.Language));
    }
}
