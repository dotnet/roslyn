// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
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
        CodeCleanupOptionsProvider,
        CodeGenerationOptionsProvider,
        CleanCodeGenerationOptionsProvider,
        CodeAndImportGenerationOptionsProvider,
        CodeActionOptionsProvider
    {
        private readonly IGlobalOptionService _globalOptions;

        public Provider(IGlobalOptionService globalOptions)
            => _globalOptions = globalOptions;

        CodeActionOptions CodeActionOptionsProvider.GetOptions(HostLanguageServices languageService)
            => _globalOptions.GetCodeActionOptions(languageService);

        ValueTask<SyntaxFormattingOptions> OptionsProvider<SyntaxFormattingOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetSyntaxFormattingOptions(languageServices));

        ValueTask<SimplifierOptions> OptionsProvider<SimplifierOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetSimplifierOptions(languageServices));

        ValueTask<AddImportPlacementOptions> OptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetAddImportPlacementOptions(languageServices));

        ValueTask<CodeCleanupOptions> OptionsProvider<CodeCleanupOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetCodeCleanupOptions(languageServices));

        ValueTask<CodeGenerationOptions> OptionsProvider<CodeGenerationOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetCodeGenerationOptions(languageServices));

        ValueTask<CleanCodeGenerationOptions> OptionsProvider<CleanCodeGenerationOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetCleanCodeGenerationOptions(languageServices));

        ValueTask<CodeAndImportGenerationOptions> OptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetCodeAndImportGenerationOptions(languageServices));
    }
}
