// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.Wrapping.Call
{
    internal abstract partial class AbstractCallWrapper : AbstractSyntaxWrapper
    {
        /// <summary>
        /// Get's the language specific trivia that should be inserted before an operator if the
        /// user wants to wrap the operator to the next line.  For C# this is a simple newline-trivia.
        /// For VB, this will be a line-continuation char (<c>_</c>), followed by a newline.
        /// </summary>
        public abstract SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine);
    }

    /// <summary>
    /// Finds and wraps code of the form:
    /// <c>
    ///     expr.M1(...).P1.M2(...).P2.I1[...]
    /// </c>
    /// 
    /// into
    /// 
    /// <c>
    ///     expr.M1(...).P1
    ///         .M2(...).P2
    ///         .I1[...]
    /// </c>
    /// </summary>
    internal abstract partial class AbstractCallWrapper<
        TExpressionSyntax,
        TNameSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TElementAccessExpressionSyntax,
        TBaseArgumentListSyntax> : AbstractCallWrapper
        where TExpressionSyntax : SyntaxNode
        where TNameSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TElementAccessExpressionSyntax : TExpressionSyntax
        where TBaseArgumentListSyntax : SyntaxNode
    {
        private readonly ISyntaxFactsService _syntaxFacts;

        protected AbstractCallWrapper(
            ISyntaxFactsService syntaxFacts)
        {
            _syntaxFacts = syntaxFacts;
        }

        public sealed override async Task<ICodeActionComputer> TryCreateComputerAsync(
            Document document, int position, SyntaxNode node, CancellationToken cancellationToken)
        {
            // has to either be `expr(...)` or `expr[...]`
            if (!IsInvocationOrElementAccessExpression(node, out var expression, out var argumentList))
            {
                return null;
            }

            // has to either be `expr.Name(...)` or `expr.Name[...]`
            if (!(expression is TMemberAccessExpressionSyntax memberAccess))
            {
                return null;
            }

            // Don't process this invocation expression if it's contained in some higher member
            // access+invocation expression.  We'll take care of this when we hit the parent.
            var current = node;
            while (current.Parent is TMemberAccessExpressionSyntax)
            {
                current = current.Parent;
            }

            if (IsInvocationOrElementAccessExpression(current.Parent, out _, out _))
            {
                return null;
            }

            _syntaxFacts.GetPartsOfMemberAccessExpression(memberAccess, out _, out var memberName);

            // Now, this is only worth wrapping if we have something below us we could align to
            // i.e. if we only have `this.Goo(...)` there's nothing to wrap.  However, we can
            // wrap when we have `this.Goo(...).Bar(...)`.  Grab the chunks of `.Name(...)` as
            // that's what we're going to be wrapping/aligning.
            // 

            var chunks = GetChunks(node);
            if (chunks.Length <= 1)
            {
                return null;
            }

            foreach (var chunk in chunks)
            {
                // If any of these chunk parts are unformattable, then we don't want to offer anything
                // here as we may make formatting worse for this construct.
                var containsUnformattableContent = await ContainsUnformattableContentAsync(
                    document, new SyntaxNodeOrToken[] { chunk.DotToken, chunk.Name }, cancellationToken).ConfigureAwait(false);

                if (!containsUnformattableContent && chunk.ArgumentListOpt != null)
                {
                    containsUnformattableContent = await ContainsUnformattableContentAsync(
                    document, new SyntaxNodeOrToken[] { chunk.ArgumentListOpt }, cancellationToken).ConfigureAwait(false);
                }

                if (containsUnformattableContent)
                {
                    return null;
                }
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return new CallCodeActionComputer(
                this, document, sourceText, options, chunks, cancellationToken);
        }

        private bool IsInvocationOrElementAccessExpression(
            SyntaxNode node, out TExpressionSyntax expression, out TBaseArgumentListSyntax argumentList)
        {
            if (IsInvocationOrElementAccessExpressionWorker(
                    node, out var expressionNode, out var argumentListNode))
            {
                expression = (TExpressionSyntax)expressionNode;
                argumentList = (TBaseArgumentListSyntax)argumentListNode;
                return true;
            }

            expression = null;
            argumentList = null;
            return false;
        }

        private bool IsInvocationOrElementAccessExpressionWorker(
            SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList)
        {
            if (node is TInvocationExpressionSyntax)
            {
                _syntaxFacts.GetPartsOfInvocationExpression(node, out expression, out argumentList);
                return true;
            }

            if (node is TElementAccessExpressionSyntax)
            {
                _syntaxFacts.GetPartsOfElementAccessExpression(node, out expression, out argumentList);
                return true;
            }

            expression = null;
            argumentList = null;
            return false;
        }

        private ImmutableArray<Chunk> GetChunks(SyntaxNode node)
        {
            var chunks = ArrayBuilder<Chunk>.GetInstance();
            AddChunks(node, chunks);
            return chunks.ToImmutableAndFree();
        }

        private void AddChunks(SyntaxNode node, ArrayBuilder<Chunk> chunks)
        {
            if (IsInvocationOrElementAccessExpression(node, out var expression, out var argumentList) &&
                expression is TMemberAccessExpressionSyntax memberAccess)
            {
                _syntaxFacts.GetPartsOfMemberAccessExpression(
                    node, out var left, out var operatorToken, out var name);
                chunks.Add(new Chunk((TExpressionSyntax)left, operatorToken, (TNameSyntax)name, argumentList));

                AddChunks(left, chunks);
            }
            else if (node is TMemberAccessExpressionSyntax memberAccessExpression)
            {
                _syntaxFacts.GetPartsOfMemberAccessExpression(
                    memberAccessExpression, out var left, out var operatorToken, out var name);
                chunks.Add(new Chunk((TExpressionSyntax)left, operatorToken, (TNameSyntax)name, argumentListOpt: null));
                AddChunks(left, chunks);
            }
        }
    }
}
