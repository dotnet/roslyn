// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    internal readonly struct OperationFactory
    {
        private readonly TokenStream _tokenStream;

        public OperationFactory(TokenStream tokenStream)
        {
            _tokenStream = tokenStream;
        }

        /// <summary>
        /// create suppress region around the given text span
        /// </summary>
        public readonly SuppressOperation SuppressOperation(SyntaxToken startToken, SyntaxToken endToken, TextSpan textSpan, SuppressOption option) 
        {
            Contract.ThrowIfTrue(startToken.RawKind == 0);
            Contract.ThrowIfTrue(endToken.RawKind == 0);

            return new SuppressOperation(
                _tokenStream.GetTokenIndexInStream(startToken),
                _tokenStream.GetTokenIndexInStream(endToken),
                textSpan,
                option);
        }

        /// <summary>
        /// create suppress region around start and end token
        /// </summary>
        public readonly SuppressOperation CreateSuppressOperation(SyntaxToken startToken, SyntaxToken endToken, SuppressOption option)
            => SuppressOperation(startToken, endToken, TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End), option);
    }
}
