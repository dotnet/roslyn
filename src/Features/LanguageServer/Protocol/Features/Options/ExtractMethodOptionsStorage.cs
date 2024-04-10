// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal static class ExtractMethodOptionsStorage
{
    public static ExtractMethodOptions GetExtractMethodOptions(this IGlobalOptionService globalOptions, string language)
        => new()
        {
            DoNotPutOutOrRefOnStruct = globalOptions.GetOption(DoNotPutOutOrRefOnStruct, language)
        };

    public static ExtractMethodGenerationOptions GetExtractMethodGenerationOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => new()
        {
            CodeGenerationOptions = globalOptions.GetCodeGenerationOptions(languageServices),
            CodeCleanupOptions = globalOptions.GetCodeCleanupOptions(languageServices),
            ExtractOptions = globalOptions.GetExtractMethodOptions(languageServices.Language),
        };

    public static ValueTask<ExtractMethodGenerationOptions> GetExtractMethodGenerationOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetExtractMethodGenerationOptionsAsync(globalOptions.GetExtractMethodGenerationOptions(document.Project.Services), cancellationToken);

    public static readonly PerLanguageOption2<bool> DoNotPutOutOrRefOnStruct = new(
        "dotnet_extract_method_no_ref_or_out_structs", ExtractMethodOptions.Default.DoNotPutOutOrRefOnStruct);
}
