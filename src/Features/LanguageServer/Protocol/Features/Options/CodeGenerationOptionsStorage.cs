// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal interface ICodeGenerationOptionsStorage : ILanguageService
{
    CodeGenerationOptions GetOptions(IGlobalOptionService globalOptions);
}

internal static class CodeGenerationOptionsStorage
{
    public static ValueTask<CodeGenerationOptions> GetCodeGenerationOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetCodeGenerationOptionsAsync(globalOptions.GetCodeGenerationOptions(document.Project.Services), cancellationToken);

    public static ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetCleanCodeGenerationOptionsAsync(globalOptions.GetCleanCodeGenerationOptions(document.Project.Services), cancellationToken);

    public static CodeGenerationOptions.CommonOptions GetCommonCodeGenerationOptions(this IGlobalOptionService globalOptions, string language)
        => new()
        {
            NamingStyle = globalOptions.GetNamingStylePreferences(language)
        };

    public static CodeGenerationOptions GetCodeGenerationOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeGenerationOptionsStorage>().GetOptions(globalOptions);

    public static CodeAndImportGenerationOptions GetCodeAndImportGenerationOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => new(globalOptions.GetCodeGenerationOptions(languageServices), globalOptions.GetAddImportPlacementOptions(languageServices));

    public static CleanCodeGenerationOptions GetCleanCodeGenerationOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => new(globalOptions.GetCodeGenerationOptions(languageServices), globalOptions.GetCodeCleanupOptions(languageServices));
}
