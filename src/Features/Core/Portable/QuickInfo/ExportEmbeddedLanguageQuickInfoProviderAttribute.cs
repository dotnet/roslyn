// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.QuickInfo;

internal sealed class ExportEmbeddedLanguageQuickInfoProviderAttribute
    : ExportEmbeddedLanguageFeatureServiceAttribute
{
    public ExportEmbeddedLanguageQuickInfoProviderAttribute(string name, string[] languages, params string[] identifiers)
        : base(typeof(IEmbeddedLanguageQuickInfoProvider), name, languages, identifiers)
    {
    }
}
