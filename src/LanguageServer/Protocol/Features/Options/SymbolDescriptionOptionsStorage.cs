// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.LanguageService;

internal static class SymbolDescriptionOptionsStorage
{
    extension(IGlobalOptionService globalOptions)
    {
        public SymbolDescriptionOptions GetSymbolDescriptionOptions(string language)
        => new()
        {
            QuickInfoOptions = globalOptions.GetQuickInfoOptions(language),
            ClassificationOptions = globalOptions.GetClassificationOptions(language),
        };
    }
}
