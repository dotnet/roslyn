// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.BraceMatching
{
    internal abstract class AbstractBraceMatcher : IBraceMatcher
    {
        private readonly BraceCharacterAndKind _openBrace;
        private readonly BraceCharacterAndKind _closeBrace;

        protected AbstractBraceMatcher(
            BraceCharacterAndKind openBrace,
            BraceCharacterAndKind closeBrace)
        {
            _openBrace = openBrace;
            _closeBrace = closeBrace;
        }

        private bool TryFindMatchingToken(SyntaxToken token, out SyntaxToken match)
        {
            var parent = token.Parent;

            var braceTokens = (from child in parent.ChildNodesAndTokens()
                               where child.IsToken
                               let tok = child.AsToken()
                               where tok.RawKind == _openBrace.Kind || tok.RawKind == _closeBrace.Kind
                               where tok.Span.Length > 0
                               select tok).ToList();

            if (braceTokens.Count == 2 &&
                braceTokens[0].RawKind == _openBrace.Kind &&
                braceTokens[1].RawKind == _closeBrace.Kind)
            {
                if (braceTokens[0] == token)
                {
                    match = braceTokens[1];
                    return true;
                }
                else if (braceTokens[1] == token)
                {
                    match = braceTokens[0];
                    return true;
                }
            }

            match = default;
            return false;
        }

        public async Task<BraceMatchingResult?> FindBracesAsync(
            Document document,
            int position,
            BraceMatchingOptions options,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (position < text.Length && this.IsBrace(text[position]))
            {
                if (token.RawKind == _openBrace.Kind && AllowedForToken(token))
                {
                    var leftToken = token;
                    if (TryFindMatchingToken(leftToken, out var rightToken))
                    {
                        return new BraceMatchingResult(leftToken.Span, rightToken.Span);
                    }
                }
                else if (token.RawKind == _closeBrace.Kind && AllowedForToken(token))
                {
                    var rightToken = token;
                    if (TryFindMatchingToken(rightToken, out var leftToken))
                    {
                        return new BraceMatchingResult(leftToken.Span, rightToken.Span);
                    }
                }
            }

            return null;
        }

        protected virtual bool AllowedForToken(SyntaxToken token)
            => true;

        private bool IsBrace(char c)
            => _openBrace.Character == c || _closeBrace.Character == c;
    }
}
