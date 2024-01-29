// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// Some of the known <see cref="QuickInfoProvider"/> names in use.
    /// Names are used for ordering providers with the <see cref="ExtensionOrderAttribute"/>.
    /// </summary>
    internal static class QuickInfoProviderNames
    {
        public const string Semantic = nameof(Semantic);
        public const string Syntactic = nameof(Syntactic);
        public const string DiagnosticAnalyzer = nameof(DiagnosticAnalyzer);
        public const string EmbeddedLanguages = nameof(EmbeddedLanguages);
    }
}
