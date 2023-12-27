// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal class RazorSemanticTokensAccessor
    {
        [Obsolete("Use GetTokenTypes")]
        public static ImmutableArray<string> RoslynTokenTypes => SemanticTokensSchema.LegacyTokenSchemaForRazor.AllTokenTypes;

        [Obsolete("Use GetTokenTypes(bool)")]
        public static ImmutableArray<string> GetTokenTypes(ClientCapabilities capabilities) => SemanticTokensSchema.GetSchema(capabilities is VSInternalClientCapabilities { SupportsVisualStudioExtensions: true }).AllTokenTypes;

        public static ImmutableArray<string> GetTokenTypes(bool clientSupportsVisualStudioExtensions) => SemanticTokensSchema.GetSchema(clientSupportsVisualStudioExtensions).AllTokenTypes;

        public static string[] GetTokenModifiers() => SemanticTokensSchema.TokenModifiers;
    }
}
