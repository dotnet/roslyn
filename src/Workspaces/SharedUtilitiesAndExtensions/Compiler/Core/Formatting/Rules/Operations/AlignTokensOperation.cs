// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules;

/// <summary>
/// align first tokens on lines among the given tokens to the base token
/// </summary>
internal sealed class AlignTokensOperation
{
    internal AlignTokensOperation(SyntaxToken baseToken, IEnumerable<SyntaxToken> tokens, AlignTokensOption option)
    {
        Contract.ThrowIfNull(tokens);
        Debug.Assert(!tokens.IsEmpty());

        this.Option = option;
        this.BaseToken = baseToken;
        this.Tokens = tokens;
    }

    public SyntaxToken BaseToken { get; }
    public IEnumerable<SyntaxToken> Tokens { get; }
    public AlignTokensOption Option { get; }
}
