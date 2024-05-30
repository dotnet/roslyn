// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal static class CodeGenerationOptionsStorage
{
    public static ValueTask<CodeGenerationOptions> GetCodeGenerationOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetCodeGenerationOptionsAsync(globalOptions.GetCodeGenerationOptions(document.Project.Services), cancellationToken);

    public static ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetCleanCodeGenerationOptionsAsync(globalOptions.GetCleanCodeGenerationOptions(document.Project.Services), cancellationToken);

    public static CodeGenerationOptions GetCodeGenerationOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => globalOptions.GetCodeGenerationOptions(languageServices, fallbackOptions: null);

    public static CodeAndImportGenerationOptions GetCodeAndImportGenerationOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => globalOptions.GetCodeAndImportGenerationOptions(languageServices, allowImportsInHiddenRegions: null, fallbackOptions: null);

    public static CleanCodeGenerationOptions GetCleanCodeGenerationOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => globalOptions.GetCleanCodeGenerationOptions(languageServices, allowImportsInHiddenRegions: null, fallbackOptions: null);
}
