// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractSyntaxFacts
    {
        public abstract ISyntaxKinds SyntaxKinds { get; }

        protected AbstractSyntaxFacts()
        {
        }

        public bool IsSingleLineCommentTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.SingleLineCommentTrivia == trivia.RawKind;

        public bool IsMultiLineCommentTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.MultiLineCommentTrivia == trivia.RawKind;

        public bool IsSingleLineDocCommentTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.SingleLineDocCommentTrivia == trivia.RawKind;

        public bool IsMultiLineDocCommentTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.MultiLineDocCommentTrivia == trivia.RawKind;

        public bool IsShebangDirectiveTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.ShebangDirectiveTrivia == trivia.RawKind;

        public abstract bool IsPreprocessorDirective(SyntaxTrivia trivia);

        public bool ContainsInterleavedDirective(
            ImmutableArray<SyntaxNode> nodes, CancellationToken cancellationToken)
        {
            if (nodes.Length > 0)
            {
                var span = TextSpan.FromBounds(nodes.First().Span.Start, nodes.Last().Span.End);

                foreach (var node in nodes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (ContainsInterleavedDirective(span, node, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool ContainsInterleavedDirective(SyntaxNode node, CancellationToken cancellationToken)
            => ContainsInterleavedDirective(node.Span, node, cancellationToken);

        public bool ContainsInterleavedDirective(
            TextSpan span, SyntaxNode node, CancellationToken cancellationToken)
        {
            foreach (var token in node.DescendantTokens())
            {
                if (ContainsInterleavedDirective(span, token, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        protected abstract bool ContainsInterleavedDirective(TextSpan span, SyntaxToken token, CancellationToken cancellationToken);

        public bool SpansPreprocessorDirective(IEnumerable<SyntaxNode> nodes)
        {
            if (nodes == null || nodes.IsEmpty())
            {
                return false;
            }

            return SpansPreprocessorDirective(nodes.SelectMany(n => n.DescendantTokens()));
        }

        /// <summary>
        /// Determines if there is preprocessor trivia *between* any of the <paramref name="tokens"/>
        /// provided.  The <paramref name="tokens"/> will be deduped and then ordered by position.
        /// Specifically, the first token will not have it's leading trivia checked, and the last
        /// token will not have it's trailing trivia checked.  All other trivia will be checked to
        /// see if it contains a preprocessor directive.
        /// </summary>
        public bool SpansPreprocessorDirective(IEnumerable<SyntaxToken> tokens)
        {
            // we want to check all leading trivia of all tokens (except the 
            // first one), and all trailing trivia of all tokens (except the
            // last one).

            var first = true;
            var previousToken = default(SyntaxToken);

            // Allow duplicate nodes/tokens to be passed in.  Also, allow the nodes/tokens
            // to not be in any particular order when passed in.
            var orderedTokens = tokens.Distinct().OrderBy(t => t.SpanStart);

            foreach (var token in orderedTokens)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    // check the leading trivia of this token, and the trailing trivia
                    // of the previous token.
                    if (SpansPreprocessorDirective(token.LeadingTrivia) ||
                        SpansPreprocessorDirective(previousToken.TrailingTrivia))
                    {
                        return true;
                    }
                }

                previousToken = token;
            }

            return false;
        }

        private bool SpansPreprocessorDirective(SyntaxTriviaList list)
            => list.Any(t => IsPreprocessorDirective(t));

        public abstract SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode node);

        public abstract bool IsParameterNameXmlElementSyntax(SyntaxNode node);

        public abstract SyntaxList<SyntaxNode> GetContentFromDocumentationCommentTriviaSyntax(SyntaxTrivia trivia);

        public bool HasIncompleteParentMember([NotNullWhen(true)] SyntaxNode? node)
            => node?.Parent?.RawKind == SyntaxKinds.IncompleteMember;
    }
}
