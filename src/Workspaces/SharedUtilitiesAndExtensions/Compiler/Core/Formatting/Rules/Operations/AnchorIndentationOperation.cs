// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// preserve relative spaces between anchor token and first tokens on lines within the given text span 
    /// as long as it doesn't have explicit line operations associated with them
    /// </summary>
    internal sealed class AnchorIndentationOperation
    {
        internal AnchorIndentationOperation(SyntaxToken anchorToken, SyntaxToken endToken, TextSpan textSpan)
        {
            Contract.ThrowIfTrue(anchorToken.RawKind == 0);
            Contract.ThrowIfTrue(textSpan.Start < 0 || textSpan.Length < 0);

            Contract.ThrowIfTrue(endToken.RawKind == 0);

            this.AnchorToken = anchorToken;
            this.TextSpan = textSpan;

            this.EndToken = endToken;
        }

        public SyntaxToken AnchorToken { get; }
        public TextSpan TextSpan { get; }

        public SyntaxToken StartToken => AnchorToken;
        public SyntaxToken EndToken { get; }
    }
}
