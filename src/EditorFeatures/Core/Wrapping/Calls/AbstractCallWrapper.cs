// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.Wrapping.Call
{
    internal abstract partial class AbstractCallWrapper : AbstractSyntaxWrapper
    {
        /// <summary>
        /// Gets the language specific trivia that should be inserted before an operator if the
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
    /// 
    /// Note: for the sake of simplicity, from now on, every time an invocation is 
    /// referred to, it means either an InvocationExpression or an ElementAccessExpression.
    /// 
    /// The way this wrapper works is breaking up a long dotted expression into 'call-chunks'
    /// of the form `.P1.P2.P3.M(...)`  i.e. a *non-empty* sequence of dot-and-name pairs
    /// followed by one or more ArgumentLists.  In this example the sequence is considered:
    /// 
    /// <c>
    ///     .P1  .P2  .P3  .M  (...)
    /// </c>
    /// 
    /// Note there are *multiple* call-chunks then the first is allowed to have any
    /// expression prior to `.P1`, whereas all the rest just point to the prior chunk.
    /// i.e.  `expr.P1.M().P2.N()` contains the two chunks:
    /// 
    /// <c>
    ///     .P1  .M  ()    // and
    ///     .P2  .N  ()
    /// </c>
    /// 
    /// If an expression can be broken into multiple chunks it is eligible for 
    /// normalized wrapping.  Normalized wrapping works by taking each chunk and
    /// removing any unnecessary whitespace between the individual call-chunks and
    /// between the last call-chunk and the arglists.  It then takes each call-chunk 
    /// and aligns the first dot of all of them if performing 'wrap all'.  If performing
    /// 'wrap long', then the wrapping only occurs if the current call-chunk's end
    /// would go past the preferred wrapping column
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
            if (!IsInvocationOrElementAccessExpression(node))
            {
                return null;
            }

            // Has to be the topmost invocation/element-access.
            if (IsInvocationOrElementAccessExpression(node.Parent))
            {
                return null;
            }

            // Now, this is only worth wrapping if we have something below us we could align to
            // i.e. if we only have `this.Goo(...)` there's nothing to wrap.  However, we can
            // wrap when we have `this.Goo(...).Bar(...)`.  Grab the chunks of `.Name(...)` as
            // that's what we're going to be wrapping/aligning.
            var callChunks = GetCallChunks(node);
            if (callChunks.Length <= 1)
            {
                return null;
            }

            // Don't process this invocation expression if it's contained in some higher member
            // call-chunk expression.  We'll take care of this when we hit the parent.
            var current = node;
            while (current.Parent is TMemberAccessExpressionSyntax)
            {
                current = current.Parent;
            }

            if (IsInvocationOrElementAccessExpression(current.Parent))
            {
                return null;
            }

            // If any of these chunk parts are unformattable, then we don't want to offer anything
            // here as we may make formatting worse for this construct.
            foreach (var callChunk in callChunks)
            {
                foreach (var memberChunk in callChunk.MemberChunks)
                {
                    var unformattable = await ContainsUnformattableContentAsync(
                       document, new SyntaxNodeOrToken[] { memberChunk.DotToken, memberChunk.Name }, cancellationToken).ConfigureAwait(false);
                    if (unformattable)
                    {
                        return null;
                    }
                }

                foreach (var argumentList in callChunk.ArgumentLists)
                {
                    var unformattable = await ContainsUnformattableContentAsync(
                        document, new SyntaxNodeOrToken[] { argumentList }, cancellationToken).ConfigureAwait(false);

                    if (unformattable)
                    {
                        return null;
                    }
                }
            }

            // Looks good.  Crate the action computer which will actually determine
            // the set of wrapping options to provide.
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return new CallCodeActionComputer(
                this, document, sourceText, options, callChunks, cancellationToken);
        }

        private bool IsInvocationOrElementAccessExpression(SyntaxNode node)
            => IsInvocationOrElementAccessExpression(node, out _, out _);

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
            var argumentLists = ArrayBuilder<TBaseArgumentListSyntax>.GetInstance();
            var memberChunks = ArrayBuilder<MemberChunk>.GetInstance();

            // Walk downwards, consuming argument lists.
            // Note: because of how we walk down, the arg lists will be reverse order.
            // We take care of that below.
            while (IsInvocationOrElementAccessExpression(node, out var expression, out var argumentList))
            {
                argumentLists.Add(argumentList);
                node = expression;
            }

            // Walk down the left side eating up `.Name` member-chunks.
            // Note: because of how we walk down, the member chunks will be reverse order.
            // We take care of that below.
            while (node is TMemberAccessExpressionSyntax memberAccess)
            {
                _syntaxFacts.GetPartsOfMemberAccessExpression(
                    node, out var left, out var operatorToken, out var name);
                memberChunks.Add(new MemberChunk(operatorToken, (TNameSyntax)name));
                node = left;
            }

            // Had to have at least one argument list and at least one member chunk.
            if (argumentLists.Count == 0 || memberChunks.Count == 0)
            {
                argumentLists.Free();
                memberChunks.Free();
                return;
            }

            // Recurse and see if we can pull out any more chunks prior to the
            // first `.Name` member-chunk we found.
            AddChunks(node, chunks);

            memberChunks.ReverseContents();
            argumentLists.ReverseContents();

            // now, create a call-chunk from the member-chunks and arg-lists we matched against.
            chunks.Add(new CallChunk(
                memberChunks.ToImmutableAndFree(), argumentLists.ToImmutableAndFree()));
        }
    }
}
