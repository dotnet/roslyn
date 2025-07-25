// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal static class CodeGenerationOptionsProviders
{
    extension(IOptionsReader options)
    {
        public CodeGenerationOptions GetCodeGenerationOptions(LanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeGenerationService>().GetCodeGenerationOptions(options);

        public CodeAndImportGenerationOptions GetCodeAndImportGenerationOptions(LanguageServices languageServices, bool? allowImportsInHiddenRegions = null)
            => new()
            {
                GenerationOptions = options.GetCodeGenerationOptions(languageServices),
                AddImportOptions = options.GetAddImportPlacementOptions(languageServices, allowImportsInHiddenRegions)
            };

        public CleanCodeGenerationOptions GetCleanCodeGenerationOptions(LanguageServices languageServices, bool? allowImportsInHiddenRegions = null)
            => new()
            {
                GenerationOptions = options.GetCodeGenerationOptions(languageServices),
                CleanupOptions = options.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions)
            };
    }

    extension(Document document)
    {
        public async ValueTask<CodeGenerationOptions> GetCodeGenerationOptionsAsync(CancellationToken cancellationToken)
        {
            var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return configOptions.GetCodeGenerationOptions(document.Project.GetExtendedLanguageServices().LanguageServices);
        }

        public async ValueTask<CodeGenerationContextInfo> GetCodeGenerationInfoAsync(CodeGenerationContext context, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(document.Project.ParseOptions);

            var options = await GetCodeGenerationOptionsAsync(document, cancellationToken).ConfigureAwait(false);
            var service = document.GetRequiredLanguageService<ICodeGenerationService>();
            return service.GetInfo(context, options, document.Project.ParseOptions);
        }
    }

    public static CodeGenerationOptions GetDefault(LanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeGenerationService>().DefaultOptions;
}
