// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal class RazorSemanticTokensAccessor
    {
        public static ImmutableArray<string> GetTokenTypes(bool clientSupportsVisualStudioExtensions) => SemanticTokensSchema.GetSchema(clientSupportsVisualStudioExtensions).AllTokenTypes;

        public static string[] GetTokenModifiers() => SemanticTokensSchema.TokenModifiers;
    }
}
