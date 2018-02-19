// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal AnchorIndentationOperation(SyntaxToken anchorToken, SyntaxToken startToken, SyntaxToken endToken, TextSpan textSpan)
        {
            Contract.ThrowIfTrue(anchorToken.RawKind == 0);
            Contract.ThrowIfTrue(textSpan.Start < 0 || textSpan.Length < 0);

            Contract.ThrowIfTrue(startToken.RawKind == 0);
            Contract.ThrowIfTrue(endToken.RawKind == 0);

            this.AnchorToken = anchorToken;
            this.TextSpan = textSpan;

            this.StartToken = startToken;
            this.EndToken = endToken;
        }

        public SyntaxToken AnchorToken { get; }
        public TextSpan TextSpan { get; }

        public SyntaxToken StartToken { get; }
        public SyntaxToken EndToken { get; }
    }
}
