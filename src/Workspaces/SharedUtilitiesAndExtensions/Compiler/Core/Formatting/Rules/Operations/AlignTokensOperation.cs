// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// align first tokens on lines among the given tokens to the base token
    /// </summary>
    internal readonly struct AlignTokensOperation
    {
        public readonly SyntaxToken BaseToken;
        public readonly ImmutableArray<SyntaxToken> Tokens;
        public readonly AlignTokensOption Option;

        public AlignTokensOperation(SyntaxToken baseToken, ImmutableArray<SyntaxToken> tokens, AlignTokensOption option)
        {
            Debug.Assert(!tokens.IsDefaultOrEmpty);

            Option = option;
            BaseToken = baseToken;
            Tokens = tokens;
        }
    }
}
