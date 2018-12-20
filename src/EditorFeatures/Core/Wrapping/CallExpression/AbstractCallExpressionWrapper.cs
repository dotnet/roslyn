// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.Wrapping.CallExpression
{
    internal abstract partial class AbstractCallExpressionWrapper : AbstractSyntaxWrapper
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
    /// Note: for the sake of simplicity, from now on, every time we talk about a CallExpression,
    /// it means either an InvocationExpression or an ElementAccessExpression.
    /// 
    /// The way this wrapper works is breaking up a long dotted call expression into 'call-chunks'
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
    internal abstract partial class AbstractCallExpressionWrapper<
        TExpressionSyntax,
        TNameSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TElementAccessExpressionSyntax,
        TBaseArgumentListSyntax> : AbstractCallExpressionWrapper
        where TExpressionSyntax : SyntaxNode
        where TNameSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TElementAccessExpressionSyntax : TExpressionSyntax
        where TBaseArgumentListSyntax : SyntaxNode
    {
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly int _dotTokenKind;
        private readonly int _questionTokenKind;

        protected AbstractCallExpressionWrapper(
            ISyntaxFactsService syntaxFacts,
            int dotTokenKind,
            int questionTokenKind)
        {
            _syntaxFacts = syntaxFacts;
            _dotTokenKind = dotTokenKind;
            _questionTokenKind = questionTokenKind;
        }

        public sealed override async Task<ICodeActionComputer> TryCreateComputerAsync(
            Document document, int position, SyntaxNode node, CancellationToken cancellationToken)
        {
            // We have to be on a chain part.
            if (!IsChainPart(node))
            {
                return null;
            }

            // Has to be the topmost chain part.
            if (IsChainPart(node.Parent))
            {
                return null;
            }

            // Now, this is only worth wrapping if we have something below us we could align to
            // i.e. if we only have `this.Goo(...)` there's nothing to wrap.  However, we can
            // wrap when we have `this.Goo(...).Bar(...)`.  Grab the chunks of `.Name(...)` as
            // that's what we're going to be wrapping/aligning.
            var chunks = GetChainChunks(node);
            if (chunks.Length <= 1)
            {
                return null;
            }

            // If any of these chunk parts are unformattable, then we don't want to offer anything
            // here as we may make formatting worse for this construct.
            foreach (var chunk in chunks)
            {
                var unformattable = await ContainsUnformattableContentAsync(
                    document, chunk, cancellationToken).ConfigureAwait(false);
                if (unformattable)
                {
                    return null;
                }
            }

            // Looks good.  Crate the action computer which will actually determine
            // the set of wrapping options to provide.
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return new CallExpressionCodeActionComputer(
                this, document, sourceText, options, chunks, cancellationToken);
        }

        private ImmutableArray<ImmutableArray<SyntaxNodeOrToken>> GetChainChunks(SyntaxNode node)
        {
            // First, just take the topmost chain node and break into the individual
            // nodes and tokens we want to treat as individual elements.  i.e. an 
            // element that would be kept together.  For example, the arg-list of an
            // invocation is an element we do not want to ever break-up/wrap.
            var pieces = ArrayBuilder<SyntaxNodeOrToken>.GetInstance();
            BreakIntoPieces(node, pieces);

            // Now that we have the pieces, find 'chunks' similar to the form:
            //
            //      . Name (...) Remainder...
            //
            // These will be the chunks that are wrapped and aligned on that dot.
            // 
            // Here 'remainder' is everything up to the next `. Name (...)` chunk.

            var chunks = ArrayBuilder<ImmutableArray<SyntaxNodeOrToken>>.GetInstance();
            BreakPiecesIntoChunks(pieces, chunks);

            pieces.Free();
            return chunks.ToImmutableAndFree();
        }

        private void BreakPiecesIntoChunks(
            ArrayBuilder<SyntaxNodeOrToken> pieces,
            ArrayBuilder<ImmutableArray<SyntaxNodeOrToken>> chunks)
        {
            // Have to look for the first chunk after the first piece.  i.e. if the pieces
            // starts with `.Foo().Bar().Baz()` then the chunks would be `.Bar()` and `.Baz()`.
            // However, if we had `this.Foo().Bar().Baz()` then the chunks would be `.Foo()`
            // `.Bar()` and `.Baz()`.
            var currentChunkStart = FindNextChunkStart(pieces, firstChunk: true, index: 1);
            if (currentChunkStart < 0)
            {
                return;
            }

            while (true)
            {
                // Look for the next chunk starting after the current chunk we're on.
                var nextChunkStart = FindNextChunkStart(pieces, firstChunk: false, index: currentChunkStart + 1);
                if (nextChunkStart < 0)
                {
                    // No next chunk after the current one.  The current chunk just
                    // extends to the end of the pieces.
                    chunks.Add(GetSubRange(pieces, currentChunkStart, end: pieces.Count));
                    return;
                }

                chunks.Add(GetSubRange(pieces, currentChunkStart, end: nextChunkStart));
                currentChunkStart = nextChunkStart;
            }
        }

        /// <summary>
        /// Looks for the next sequence of `. Name (ArgList)`.  Note, except for the first
        /// chunk, this cannot be of the form `? . Name (ArgList)` as we do not want to 
        /// wrap before a dot in a `?.` form.  This doesn't matter for the first chunk as
        /// we won't be wrapping that one.
        /// </summary>
        private int FindNextChunkStart(
            ArrayBuilder<SyntaxNodeOrToken> pieces, bool firstChunk, int index)
        {
            for (var i = index; i < pieces.Count; i++)
            {
                if (IsToken(_dotTokenKind, pieces, i) &&
                    IsNode<TNameSyntax>(pieces, i + 1) &&
                    IsNode<TBaseArgumentListSyntax>(pieces, i + 2))
                {
                    if (firstChunk || !IsToken(_questionTokenKind, pieces, i - 1))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private bool IsNode<TNode>(ArrayBuilder<SyntaxNodeOrToken> pieces, int index)
        {
            if (index < pieces.Count)
            {
                var piece = pieces[index];
                return piece.IsNode && piece.AsNode() is TNode;
            }

            return false;
        }

        private bool IsToken(int tokenKind, ArrayBuilder<SyntaxNodeOrToken> pieces, int index)
        {
            if (index < pieces.Count)
            {
                var piece = pieces[index];
                return piece.IsToken && piece.AsToken().RawKind == tokenKind;
            }

            return false;
        }

        private ImmutableArray<SyntaxNodeOrToken> GetSubRange(
            ArrayBuilder<SyntaxNodeOrToken> pieces, int start, int end)
        {
            var result = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(end - start);
            for (var i = start; i < end; i++)
            {
                result.Add(pieces[i]);
            }

            return result.ToImmutableAndFree();
        }

        private bool IsChainPart(SyntaxNode node)
        {
            if (_syntaxFacts.IsAnyMemberAccessExpression(node) ||
                _syntaxFacts.IsInvocationExpression(node) ||
                _syntaxFacts.IsElementAccessExpression(node) ||
                _syntaxFacts.IsPostfixUnaryExpression(node) ||
                _syntaxFacts.IsConditionalAccessExpression(node) ||
                _syntaxFacts.IsMemberBindingExpression(node))
            {
                return true;
            }

            return false;
        }

        private void BreakIntoPieces(SyntaxNode node, ArrayBuilder<SyntaxNodeOrToken> pieces)
        {
            if (node is null)
            {
                return;
            }

            // Keep in sync with IsChainPart
            if (_syntaxFacts.IsAnyMemberAccessExpression(node))
            {
                _syntaxFacts.GetPartsOfMemberAccessExpression(
                    node, out var expression, out var operatorToken, out var name);
                BreakIntoPieces(expression, pieces);
                pieces.Add(operatorToken);
                pieces.Add(name);
            }
            else if (_syntaxFacts.IsMemberBindingExpression(node))
            {
                _syntaxFacts.GetPartsOfMemberBindingExpression(
                    node, out var dotToken, out var name);
                pieces.Add(dotToken);
                pieces.Add(name);
            }
            else if (_syntaxFacts.IsInvocationExpression(node))
            {
                _syntaxFacts.GetPartsOfInvocationExpression(
                    node, out var expression, out var argumentList);
                BreakIntoPieces(expression, pieces);
                pieces.Add(argumentList);
            }
            else if (_syntaxFacts.IsElementAccessExpression(node))
            {
                _syntaxFacts.GetPartsOfElementAccessExpression(
                    node, out var expression, out var argumentList);
                BreakIntoPieces(expression, pieces);
                pieces.Add(argumentList);
            }
            else if (_syntaxFacts.IsPostfixUnaryExpression(node))
            {
                _syntaxFacts.GetPartsOfPostfixUnaryExpression(
                    node, out var operand, out var operatorToken);
                BreakIntoPieces(operand, pieces);
                pieces.Add(operatorToken);
            }
            else if (_syntaxFacts.IsConditionalAccessExpression(node))
            {
                _syntaxFacts.GetPartsOfConditionalAccessExpression(
                    node, out var expression, out var questionToken, out var whenNotNull);
                BreakIntoPieces(expression, pieces);
                pieces.Add(questionToken);
                BreakIntoPieces(whenNotNull, pieces);
            }
            else
            {
                pieces.Add(node);
            }
        }

        //private bool IsCallExpression(SyntaxNode node)
        //    => IsCallExpression(node, out _, out _);

        //private bool IsCallExpression(
        //    SyntaxNode node, out TExpressionSyntax expression, out TBaseArgumentListSyntax argumentList)
        //{
        //    if (IsCallExpressionWorker(
        //            node, out var expressionNode, out var argumentListNode))
        //    {
        //        expression = (TExpressionSyntax)expressionNode;
        //        argumentList = (TBaseArgumentListSyntax)argumentListNode;
        //        return true;
        //    }

        //    expression = null;
        //    argumentList = null;
        //    return false;
        //}

        //private bool IsCallExpressionWorker(
        //    SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList)
        //{
        //    if (node is TInvocationExpressionSyntax)
        //    {
        //        _syntaxFacts.GetPartsOfInvocationExpression(node, out expression, out argumentList);
        //        return true;
        //    }

        //    if (node is TElementAccessExpressionSyntax)
        //    {
        //        _syntaxFacts.GetPartsOfElementAccessExpression(node, out expression, out argumentList);
        //        return true;
        //    }

        //    expression = null;
        //    argumentList = null;
        //    return false;
        //}

        //private ImmutableArray<CallChunk> GetCallChunks(SyntaxNode node)
        //{
        //    var chunks = ArrayBuilder<CallChunk>.GetInstance();
        //    AddChunks(node, chunks);
        //    return chunks.ToImmutableAndFree();
        //}

        //private void AddChunks(SyntaxNode node, ArrayBuilder<CallChunk> chunks)
        //{
        //    var argumentLists = ArrayBuilder<TBaseArgumentListSyntax>.GetInstance();
        //    var memberChunks = ArrayBuilder<MemberChunk>.GetInstance();

        //    // Walk downwards, consuming argument lists.
        //    // Note: because of how we walk down, the arg lists will be reverse order.
        //    // We take care of that below.
        //    while (IsCallExpression(node, out var expression, out var argumentList))
        //    {
        //        argumentLists.Add(argumentList);
        //        node = expression;
        //    }

        //    // Walk down the left side eating up `.Name` member-chunks.
        //    // Note: because of how we walk down, the member chunks will be reverse order.
        //    // We take care of that below.
        //    while (node is TMemberAccessExpressionSyntax memberAccess)
        //    {
        //        _syntaxFacts.GetPartsOfMemberAccessExpression(
        //            node, out var left, out var operatorToken, out var name);

        //        // We could have a leading member without an 'left' side before
        //        // the dot.  This happens in vb 'with' statements.  In that case
        //        // we don't want to consider this part the one to align with. i.e.
        //        // we don't want to generate:
        //        //
        //        //      with goo
        //        //          .bar.baz()
        //        //          .quux()
        //        //
        //        // We want to generate:
        //        //
        //        //      with goo
        //        //          .bar.baz()
        //        //              .quux()
        //        if (left != null)
        //        {
        //            memberChunks.Add(new MemberChunk(operatorToken, (TNameSyntax)name));
        //        }

        //        node = left;
        //    }

        //    // Had to have at least one argument list and at least one member chunk.
        //    if (argumentLists.Count == 0 || memberChunks.Count == 0)
        //    {
        //        argumentLists.Free();
        //        memberChunks.Free();
        //        return;
        //    }

        //    // Recurse and see if we can pull out any more chunks prior to the
        //    // first `.Name` member-chunk we found.
        //    AddChunks(node, chunks);

        //    memberChunks.ReverseContents();
        //    argumentLists.ReverseContents();

        //    // now, create a call-chunk from the member-chunks and arg-lists we matched against.
        //    chunks.Add(new CallChunk(
        //        memberChunks.ToImmutableAndFree(), argumentLists.ToImmutableAndFree()));
        //}
    }
}
