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
    ///     expr.P1.M1(...).P2.M2(...).P3.I1[...]
    /// </c>
    /// 
    /// into
    /// 
    /// <c>
    ///     expr.P1.M1(...)
    ///         .P2.M2(...)
    ///         .P3.I1[...]
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

            var callChunks = GetCallChunks(node);
            if (callChunks.Length <= 1)
            {
                return null;
            }

            // If any of these chunk parts are unformattable, then we don't want to offer anything
            // here as we may make formatting worse for this construct.
            foreach (var callChunk in callChunks)
            {
                foreach (var memberChunk in callChunk.MemberChunks)
                {
                    var memberContainsUnformattableContent = await ContainsUnformattableContentAsync(
                       document, new SyntaxNodeOrToken[] { memberChunk.DotToken, memberChunk.Name }, cancellationToken).ConfigureAwait(false);
                    if (memberContainsUnformattableContent)
                    {
                        return null;
                    }
                }

                var containsUnformattableContent = await ContainsUnformattableContentAsync(
                        document, new SyntaxNodeOrToken[] { callChunk.ArgumentList }, cancellationToken).ConfigureAwait(false);

                if (containsUnformattableContent)
                {
                    return null;
                }
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return new CallCodeActionComputer(
                this, document, sourceText, options, callChunks, cancellationToken);
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

        private ImmutableArray<CallChunk> GetCallChunks(SyntaxNode node)
        {
            var chunks = ArrayBuilder<CallChunk>.GetInstance();
            AddChunks(node, chunks);
            return chunks.ToImmutableAndFree();
        }

        private void AddChunks(SyntaxNode node, ArrayBuilder<CallChunk> chunks)
        {
            if (IsInvocationOrElementAccessExpression(node, out var expression, out var argumentList) &&
                expression is TMemberAccessExpressionSyntax)
            {
                var memberChunks = ArrayBuilder<MemberChunk>.GetInstance();
                var current = (TExpressionSyntax)expression;
                while (current is TMemberAccessExpressionSyntax memberAccess)
                {
                    _syntaxFacts.GetPartsOfMemberAccessExpression(
                        current, out var left, out var operatorToken, out var name);
                    memberChunks.Insert(0, new MemberChunk(operatorToken, (TNameSyntax)name));
                    current = (TExpressionSyntax)left;
                }

                AddChunks(current, chunks);
                chunks.Add(new CallChunk(memberChunks.ToImmutableAndFree(), argumentList));
            }
        }
    }
}
