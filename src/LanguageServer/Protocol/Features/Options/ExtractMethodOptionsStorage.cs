// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal static class ExtractMethodOptionsStorage
{
    public static ExtractMethodGenerationOptions GetExtractMethodGenerationOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => new()
        {
            CodeGenerationOptions = globalOptions.GetCodeGenerationOptions(languageServices),
            CodeCleanupOptions = globalOptions.GetCodeCleanupOptions(languageServices),
        };
}
