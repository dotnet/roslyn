// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

internal static class CodeActionOptionsStorage
{
    public static Provider CreateProvider(this IGlobalOptionService globalOptions)
        => new(globalOptions);

    // TODO: we can implement providers directly on IGlobalOptionService once it moves to LSP layer
    public sealed class Provider :
        SyntaxFormattingOptionsProvider,
        SimplifierOptionsProvider,
        AddImportPlacementOptionsProvider,
        CleanCodeGenerationOptionsProvider,
        CodeAndImportGenerationOptionsProvider,
        CodeActionOptionsProvider
    {
        private readonly IGlobalOptionService _globalOptions;

        public Provider(IGlobalOptionService globalOptions)
            => _globalOptions = globalOptions;

        CodeActionOptions CodeActionOptionsProvider.GetOptions(LanguageServices languageServices)
            => _globalOptions.GetCodeActionOptions(languageServices);

        ValueTask<LineFormattingOptions> OptionsProvider<LineFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetLineFormattingOptions(languageServices.Language));

        ValueTask<SyntaxFormattingOptions> OptionsProvider<SyntaxFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetSyntaxFormattingOptions(languageServices));

        ValueTask<SimplifierOptions> OptionsProvider<SimplifierOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetSimplifierOptions(languageServices));

        ValueTask<AddImportPlacementOptions> OptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetAddImportPlacementOptions(languageServices, allowInHiddenRegions: null));

        ValueTask<OrganizeImportsOptions> OptionsProvider<OrganizeImportsOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetOrganizeImportsOptions(languageServices.Language));

        ValueTask<CleanCodeGenerationOptions> OptionsProvider<CleanCodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetCleanCodeGenerationOptions(languageServices));

        ValueTask<CodeAndImportGenerationOptions> OptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetCodeAndImportGenerationOptions(languageServices));
    }
}
