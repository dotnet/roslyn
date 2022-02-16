// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal readonly record struct SymbolDescriptionOptions(
        QuickInfoOptions QuickInfoOptions,
        ClassificationOptions ClassificationOptions)
    {
        public static readonly SymbolDescriptionOptions Default
          = new(
              QuickInfoOptions: QuickInfoOptions.Default,
              ClassificationOptions: ClassificationOptions.Default);
    }
}
