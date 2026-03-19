// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.QuickInfo;

internal interface IEmbeddedLanguageQuickInfoProvider : IEmbeddedLanguageFeatureService
{
    /// <summary>
    /// Gets the <see cref="QuickInfoItem"/> for the position in an embedded language.
    /// </summary>
    /// <returns>The <see cref="QuickInfoItem"/> or null if no item is available.</returns>
    QuickInfoItem? GetQuickInfo(
        QuickInfoContext context,
        SemanticModel semanticModel,
        SyntaxToken token);
}
