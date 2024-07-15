// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Wrapping.ChainedExpression;

/// <summary>
/// Finds and wraps 'chained' expressions.  For the purpose of this feature, a chained
/// expression is built out of 'chunks' where each chunk is of the form
///
/// <code>
///     . name (arglist) remainder
/// </code>
/// 
/// So, if there are two or more of these like:
/// 
/// <code>
///     . name1 (arglist1) remainder1 . name2 (arglist2) remainder2
/// </code>
/// 
/// Then this will be wrapped such that the dots align like so:
/// 
/// <code>
///     . name1 (arglist1) remainder1
///     . name2 (arglist2) remainder2
/// </code>
/// 
/// Note: for the sake of simplicity, (arglist) is used both for the argument list of
/// an InvocationExpression and an ElementAccessExpression.
/// 
/// 'remainder' is all the postfix expression that can follow <c>. name (arglist)</c>.  i.e.
/// member-access expressions, conditional-access expressions, etc.  Effectively, anything
/// the language allows at this point as long as it doesn't start another 'chunk' itself.
/// 
/// This approach gives an intuitive wrapping algorithm that matches the common way
/// many wrap dotted invocations, while also effectively not limiting the wrapper to
/// only simple forms like <c>.a(...).b(...).c(...)</c>.  
/// </summary>
internal abstract partial class AbstractChainedExpressionWrapper<
    TNameSyntax,
    TBaseArgumentListSyntax> : AbstractSyntaxWrapper
    where TNameSyntax : SyntaxNode
    where TBaseArgumentListSyntax : SyntaxNode
{
    private readonly ISyntaxFacts _syntaxFacts;
    private readonly int _dotToken;
    private readonly int _questionToken;

    protected AbstractChainedExpressionWrapper(
        Indentation.IIndentationService indentationService,
        ISyntaxFacts syntaxFacts) : base(indentationService)
    {
        _syntaxFacts = syntaxFacts;
        _dotToken = syntaxFacts.SyntaxKinds.DotToken;
        _questionToken = syntaxFacts.SyntaxKinds.QuestionToken;
    }

    /// <summary>
    /// Gets the language specific trivia that should be inserted before an operator if the
    /// user wants to wrap the operator to the next line.  For C# this is a simple newline-trivia.
    /// For VB, this will be a line-continuation char (<c>_</c>), followed by a newline.
    /// </summary>
    protected abstract SyntaxTriviaList GetNewLineBeforeOperatorTrivia(SyntaxTriviaList newLine);

    public sealed override async Task<ICodeActionComputer?> TryCreateComputerAsync(
        Document document, int position, SyntaxNode node, SyntaxWrappingOptions options, bool containsSyntaxError, CancellationToken cancellationToken)
    {
        if (containsSyntaxError)
            return null;

        // We have to be on a chain part.  If not, there's nothing to do here at all.
        if (!IsDecomposableChainPart(node))
            return null;

        // Has to be the topmost chain part.  If we're not on the topmost, then just
        // bail out here.  Our caller will continue walking upwards until it hits the 
        // topmost node.
        if (IsDecomposableChainPart(node.Parent))
            return null;

        // We're at the top of something that looks like it could be part of a chained
        // expression.  Break it into the individual chunks.  We need to have at least
        // two chunks or this to be worth wrapping.
        //
        // i.e. if we only have <c>this.Goo(...)</c> there's nothing to wrap.  However, we can
        // wrap when we have <c>this.Goo(...).Bar(...)</c>.
        var chunks = GetChainChunks(node);
        if (chunks.Length <= 1)
            return null;

        // If any of these chunk parts are unformattable, then we don't want to offer anything
        // here as we may make formatting worse for this construct.
        foreach (var chunk in chunks)
        {
            var unformattable = await ContainsUnformattableContentAsync(
                document, chunk, cancellationToken).ConfigureAwait(false);
            if (unformattable)
                return null;
        }

        // Looks good.  Create the action computer which will actually determine
        // the set of wrapping options to provide.
        var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        return new CallExpressionCodeActionComputer(
            this, document, sourceText, options, chunks, cancellationToken);
    }

    private ImmutableArray<ImmutableArray<SyntaxNodeOrToken>> GetChainChunks(SyntaxNode node)
    {
        // First, just take the topmost chain node and break into the individual
        // nodes and tokens we want to treat as individual elements.  i.e. an 
        // element that would be kept together.  For example, the arg-list of an
        // invocation is an element we do not want to ever break-up/wrap. 
        using var _1 = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var pieces);
        Decompose(node, pieces);

        // Now that we have the pieces, find 'chunks' similar to the form:
        //
        //      . Name (...) Remainder...
        //
        // These will be the chunks that are wrapped and aligned on that dot.
        // 
        // Here 'remainder' is everything up to the next <c>. Name (...)</c> chunk.

        using var _2 = ArrayBuilder<ImmutableArray<SyntaxNodeOrToken>>.GetInstance(out var chunks);
        BreakPiecesIntoChunks(pieces, chunks);
        return chunks.ToImmutable();
    }

    private void BreakPiecesIntoChunks(
        ArrayBuilder<SyntaxNodeOrToken> pieces,
        ArrayBuilder<ImmutableArray<SyntaxNodeOrToken>> chunks)
    {
        // Have to look for the first chunk after the first piece.  i.e. if the pieces
        // starts with <c>.Foo().Bar().Baz()</c> then the chunks would be <c>.Bar()</c> 
        // and <c>.Baz()</c>.
        //
        // However, if we had <c>this.Foo().Bar().Baz()</c> then the chunks would be 
        // <c>.Foo()</c> <c>.Bar()</c> and <c>.Baz()</c>.
        //
        // Note: the only way to get the <c>.Foo().Bar().Baz()</c> case today is in VB in
        // a 'with' statement.  if we have that, we don't want to wrap it into:
        //
        //  <code>
        //  with ...
        //      .Foo()
        //      .Bar()
        //      .Baz()
        //  </code>
        //
        // Instead, we want to create
        //
        //  <code>
        //  with ...
        //      .Foo().Bar()
        //            .Baz()
        //  </code>
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

            // Had a chunk after this one.  Record the current chunk, move to the start
            // of the next one, and then keep going.
            chunks.Add(GetSubRange(pieces, currentChunkStart, end: nextChunkStart));
            currentChunkStart = nextChunkStart;
        }
    }

    /// <summary>
    /// Looks for the next sequence of <c>. Name (ArgList)</c>.  Note, except for the first
    /// chunk, this cannot be of the form <c>? . Name (ArgList)</c> as we do not want to 
    /// wrap before a dot in a <c>?.</c> form.  This doesn't matter for the first chunk as
    /// we won't be wrapping that one.
    /// </summary>
    private int FindNextChunkStart(
        ArrayBuilder<SyntaxNodeOrToken> pieces, bool firstChunk, int index)
    {
        for (var i = index; i < pieces.Count; i++)
        {
            if (IsToken(_dotToken, pieces, i) &&
                IsNode<TNameSyntax>(pieces, i + 1) &&
                IsNode<TBaseArgumentListSyntax>(pieces, i + 2))
            {
                if (firstChunk ||
                    !IsToken(_questionToken, pieces, i - 1))
                {
                    return i;
                }
            }
        }

        // Couldn't find the start of another chunk.
        return -1;
    }

    private static bool IsNode<TNode>(ArrayBuilder<SyntaxNodeOrToken> pieces, int index)
        => index < pieces.Count &&
           pieces[index] is var piece &&
           piece.IsNode &&
           piece.AsNode() is TNode;

    private static bool IsToken(int tokenKind, ArrayBuilder<SyntaxNodeOrToken> pieces, int index)
        => index < pieces.Count &&
           pieces[index] is var piece &&
           piece.IsToken &&
           piece.AsToken().RawKind == tokenKind;

    private static ImmutableArray<SyntaxNodeOrToken> GetSubRange(
        ArrayBuilder<SyntaxNodeOrToken> pieces, int start, int end)
    {
        using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(end - start, out var result);
        for (var i = start; i < end; i++)
            result.Add(pieces[i]);

        return result.ToImmutableAndClear();
    }

    private bool IsDecomposableChainPart(SyntaxNode? node)
    {
        // This is the effective set of language constructs that can 'chain' 
        // off of a call <c>.M(...)</c>.  They are:
        //
        // 1. <c>.Name</c> or <c>->Name</c>.        i.e. <c>.M(...).Name</c>
        // 2. <c>(...)</c>.                         i.e. <c>.M(...)(...)</c>
        // 3. <c>[...]</c>.                         i.e. <c>.M(...)[...]</c>
        // 4. <c>++</c>, </c>--</c>, </c>!</c>.     i.e. <c>.M(...)++</c>
        // 5. <c>?</c>.                             i.e. <c>.M(...)?. ...</c> or <c>.M(...)?[...]</c>
        //      '5' handles both the ConditionalAccess and MemberBinding cases below.

        if (node != null)
        {
            return _syntaxFacts.IsMemberAccessExpression(node)
                   || _syntaxFacts.IsInvocationExpression(node)
                   || _syntaxFacts.IsElementAccessExpression(node)
                   || _syntaxFacts.IsPostfixUnaryExpression(node)
                   || _syntaxFacts.IsConditionalAccessExpression(node)
                   || _syntaxFacts.IsMemberBindingExpression(node);
        }

        return false;
    }

    /// <summary>
    /// Walks down <paramref name="node"/> decomposing it into the individual tokens and nodes we want to look for chunks in. 
    /// </summary>
    private void Decompose(SyntaxNode node, ArrayBuilder<SyntaxNodeOrToken> pieces)
    {
        // Ignore null nodes, they are never relevant when building up the sequence of
        // pieces in this chained expression.
        if (node is null)
            return;

        var stack = SharedPools.Default<Stack<SyntaxNodeOrToken>>().AllocateAndClear();
        stack.Push(node);
        try
        {
            while (stack.Count > 0)
            {
                var nodeOrToken = stack.Pop();
                if (nodeOrToken.IsToken)
                {
                    // tokens can't be decomposed.  just add to the result list.
                    pieces.Add(nodeOrToken.AsToken());
                    continue;
                }

                var currentNode = nodeOrToken.AsNode()!;
                if (!IsDecomposableChainPart(currentNode))
                {
                    // We've hit some node that can't be decomposed further (like an argument list, or name node).
                    // Just add directly to the pieces list.
                    pieces.Add(currentNode);
                    continue;
                }

                // Hit something that can be decomposed.  Push it onto the stack in reverse so that we continue to
                // traverse the node from right to left as we pop things off the end of the stack.
                foreach (var child in currentNode.ChildNodesAndTokens().Reverse())
                    stack.Push(child);
            }
        }
        finally
        {
            SharedPools.Default<Stack<SyntaxNodeOrToken>>().Free(stack);
        }
    }
}
