// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal class RazorSemanticTokensAccessor
    {
        [Obsolete("Use GetTokenTypes")]
        public static ImmutableArray<string> RoslynTokenTypes => SemanticTokensSchema.LegacyTokenSchemaForRazor.AllTokenTypes;

        public static ImmutableArray<string> GetTokenTypes(ClientCapabilities capabilities) => SemanticTokensSchema.GetSchema(capabilities).AllTokenTypes;
    }
}
