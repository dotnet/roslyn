// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens
{
    public abstract class AbstractSemanticTokensTests : AbstractLanguageServerProtocolTests
    {
        protected static int GetTypeIndex(string type) => Array.IndexOf(SemanticTokensHelpers.TokenTypes, type);

        protected static int GetModifierBits(string modifier, int result)
        {
            result |= Array.IndexOf(SemanticTokensHelpers.TokenModifiers, modifier) + 1;
            return result;
        }
    }
}
