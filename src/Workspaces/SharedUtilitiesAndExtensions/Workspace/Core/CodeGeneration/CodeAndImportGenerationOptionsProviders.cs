// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal static class CodeAndImportGenerationOptionsProviders
{
    internal static CodeAndImportGenerationOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            GenerationOptions = CodeGenerationOptionsProviders.GetDefault(languageServices),
            AddImportOptions = AddImportPlacementOptions.Default
        };
}
