// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class SyntaxNode
    {
        private IEnumerable<SyntaxNode> DescendantNodesImpl(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren, bool descendIntoTrivia, bool includeSelf)
        {
            return descendIntoTrivia
                ? DescendantNodesAndTokensImpl(span, descendIntoChildren, descendIntoTrivia, includeSelf).Where(e => e.IsNode).Select(e => e.AsNode())
                : DescendantNodesOnly(span, descendIntoChildren, includeSelf);
        }

        private IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokensImpl(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren, bool descendIntoTrivia, bool includeSelf)
        {
            return descendIntoTrivia
                ? DescendantNodesAndTokensIntoTrivia(span, descendIntoChildren, includeSelf)
                : DescendantNodesAndTokensOnly(span, descendIntoChildren, includeSelf);
        }

        private IEnumerable<SyntaxTrivia> DescendantTriviaImpl(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren = null, bool descendIntoTrivia = false)
        {
            return descendIntoTrivia
                ? DescendantTriviaIntoTrivia(span, descendIntoChildren)
                : DescendantTriviaOnly(span, descendIntoChildren);
        }

        private static bool IsInSpan(ref TextSpan span, TextSpan childSpan)
        {
            return span.OverlapsWith(childSpan)
                // special case for zero-width tokens (OverlapsWith never returns true for these)
                || (childSpan.Length == 0 && span.IntersectsWith(childSpan));
        }

        private struct ChildSyntaxListEnumeratorStack : IDisposable
        {
            private static readonly ObjectPool<ChildSyntaxList.Enumerator[]> StackPool = new ObjectPool<ChildSyntaxList.Enumerator[]>(() => new ChildSyntaxList.Enumerator[16]);

            private ChildSyntaxList.Enumerator[] stack;
            private int stackPtr;

            public ChildSyntaxListEnumeratorStack(SyntaxNode startingNode, Func<SyntaxNode, bool> descendIntoChildren)
            {
                if (descendIntoChildren == null || descendIntoChildren(startingNode))
                {
                this.stack = StackPool.Allocate();
                this.stackPtr = 0;
                this.stack[0].InitializeFrom(startingNode);
            }
                else
                {
                    this.stack = null;
                    this.stackPtr = -1;
                }
            }

            public bool IsNotEmpty { get { return stackPtr >= 0; } }

            public bool TryGetNextInSpan(ref /*readonly*/ TextSpan span, out SyntaxNodeOrToken value)
            {
                value = default(SyntaxNodeOrToken);

                while (stack[stackPtr].TryMoveNextAndGetCurrent(ref value))
                {
                    if (IsInSpan(ref span, value.FullSpan))
                    {
                        return true;
                    }
                }

                stackPtr--;
                return false;
            }

            public SyntaxNode TryGetNextAsNodeInSpan(ref /*readonly*/ TextSpan span)
            {
                SyntaxNode nodeValue;
                while ((nodeValue = stack[stackPtr].TryMoveNextAndGetCurrentAsNode()) != null)
                {
                    if (IsInSpan(ref span, nodeValue.FullSpan))
                    {
                        return nodeValue;
                    }
                }

                stackPtr--;
                return null;
            }

            public void PushChildren(SyntaxNode node)
            {
                if (++stackPtr >= stack.Length)
                {
                    // Geometric growth
                    Array.Resize(ref stack, checked(stackPtr * 2));
                }

                stack[stackPtr].InitializeFrom(node);
            }

            public void PushChildren(SyntaxNode node, Func<SyntaxNode, bool> descendIntoChildren)
            {
                if (descendIntoChildren == null || descendIntoChildren(node))
                {
                    PushChildren(node);
                }
            }

            public void Dispose()
            {
                // Return only reasonably-sized stacks to the pool.
                if (stack?.Length < 256)
                {
                    Array.Clear(stack, 0, stack.Length);
                    StackPool.Free(stack);
                }
            }
        }

        private struct TriviaListEnumeratorStack : IDisposable
        {
            private static readonly ObjectPool<SyntaxTriviaList.Enumerator[]> StackPool = new ObjectPool<SyntaxTriviaList.Enumerator[]>(() => new SyntaxTriviaList.Enumerator[16]);

            private SyntaxTriviaList.Enumerator[] stack;
            private int stackPtr;

            public bool TryGetNext(out SyntaxTrivia value)
            {
                value = default(SyntaxTrivia);

                if (stack[stackPtr].TryMoveNextAndGetCurrent(ref value))
                {
                    return true;
                }

                stackPtr--;
                return false;
            }

            public void PushLeadingTrivia(ref SyntaxToken token)
            {
                Grow();
                stack[stackPtr].InitializeFromLeadingTrivia(ref token);
            }

            public void PushTrailingTrivia(ref SyntaxToken token)
            {
                Grow();
                stack[stackPtr].InitializeFromTrailingTrivia(ref token);
            }

            private void Grow()
            {
                if (stack == null)
                {
                    stack = StackPool.Allocate();
                    stackPtr = -1;
                }

                if (++stackPtr >= stack.Length)
                {
                    // Geometric growth
                    Array.Resize(ref stack, checked(stackPtr * 2));
                }
            }

            public void Dispose()
            {
                // Return only reasonably-sized stacks to the pool.
                if (stack?.Length < 256)
                {
                    Array.Clear(stack, 0, stack.Length);
                    StackPool.Free(stack);
                }
            }
        }

        private struct TwoEnumeratorListStack : IDisposable
        {
            public enum Which : byte
            {
                Node,
                Trivia
            }

            private ChildSyntaxListEnumeratorStack nodeStack;
            private TriviaListEnumeratorStack triviaStack;
            private readonly ArrayBuilder<Which> discriminatorStack;

            public TwoEnumeratorListStack(SyntaxNode startingNode, Func<SyntaxNode, bool> descendIntoChildren)
            {
                this.nodeStack = new ChildSyntaxListEnumeratorStack(startingNode, descendIntoChildren);
                this.triviaStack = new TriviaListEnumeratorStack();
                if (this.nodeStack.IsNotEmpty)
                {
                    this.discriminatorStack = ArrayBuilder<Which>.GetInstance();
                    this.discriminatorStack.Push(Which.Node);
                }
                else
                {
                    this.discriminatorStack = null;
                }
            }

            public bool IsNotEmpty { get { return discriminatorStack?.Count > 0; } }

            public Which PeekNext()
            {
                return discriminatorStack.Peek();
            }

            public bool TryGetNextInSpan(ref TextSpan span, out SyntaxNodeOrToken value)
            {
                if (nodeStack.TryGetNextInSpan(ref span, out value))
                {
                    return true;
                }

                discriminatorStack.Pop();
                return false;
            }

            public bool TryGetNext(out SyntaxTrivia value)
            {
                if (triviaStack.TryGetNext(out value))
                {
                    return true;
                }

                discriminatorStack.Pop();
                return false;
            }

            public void PushChildren(SyntaxNode node, Func<SyntaxNode, bool> descendIntoChildren)
            {
                if (descendIntoChildren == null || descendIntoChildren(node))
                {
                    nodeStack.PushChildren(node);
                    discriminatorStack.Push(Which.Node);
                }
            }

            public void PushLeadingTrivia(ref SyntaxToken token)
            {
                triviaStack.PushLeadingTrivia(ref token);
                discriminatorStack.Push(Which.Trivia);
            }

            public void PushTrailingTrivia(ref SyntaxToken token)
            {
                triviaStack.PushTrailingTrivia(ref token);
                discriminatorStack.Push(Which.Trivia);
            }

            public void Dispose()
            {
                this.nodeStack.Dispose();
                this.triviaStack.Dispose();
                this.discriminatorStack?.Free();
            }
        }

        private struct ThreeEnumeratorListStack : IDisposable
        {
            public enum Which : byte
            {
                Node,
                Trivia,
                Token
            }

            private ChildSyntaxListEnumeratorStack nodeStack;
            private TriviaListEnumeratorStack triviaStack;
            private readonly ArrayBuilder<SyntaxNodeOrToken> tokenStack;
            private readonly ArrayBuilder<Which> discriminatorStack;

            public ThreeEnumeratorListStack(SyntaxNode startingNode, Func<SyntaxNode, bool> descendIntoChildren)
            {
                this.nodeStack = new ChildSyntaxListEnumeratorStack(startingNode, descendIntoChildren);
                this.triviaStack = new TriviaListEnumeratorStack();
                if (this.nodeStack.IsNotEmpty)
                {
                    this.tokenStack = ArrayBuilder<SyntaxNodeOrToken>.GetInstance();
                    this.discriminatorStack = ArrayBuilder<Which>.GetInstance();
                    this.discriminatorStack.Push(Which.Node);
                }
                else
                {
                    this.tokenStack = null;
                    this.discriminatorStack = null;
                }
            }

            public bool IsNotEmpty { get { return discriminatorStack?.Count > 0; } }

            public Which PeekNext()
            {
                return discriminatorStack.Peek();
            }

            public bool TryGetNextInSpan(ref TextSpan span, out SyntaxNodeOrToken value)
            {
                if (nodeStack.TryGetNextInSpan(ref span, out value))
                {
                    return true;
                }

                discriminatorStack.Pop();
                return false;
            }

            public bool TryGetNext(out SyntaxTrivia value)
            {
                if (triviaStack.TryGetNext(out value))
                {
                    return true;
                }

                discriminatorStack.Pop();
                return false;
            }

            public SyntaxNodeOrToken PopToken()
            {
                discriminatorStack.Pop();
                return tokenStack.Pop();
            }

            public void PushChildren(SyntaxNode node, Func<SyntaxNode, bool> descendIntoChildren)
            {
                if (descendIntoChildren == null || descendIntoChildren(node))
                {
                    nodeStack.PushChildren(node);
                    discriminatorStack.Push(Which.Node);
                }
            }

            public void PushLeadingTrivia(ref SyntaxToken token)
            {
                triviaStack.PushLeadingTrivia(ref token);
                discriminatorStack.Push(Which.Trivia);
            }

            public void PushTrailingTrivia(ref SyntaxToken token)
            {
                triviaStack.PushTrailingTrivia(ref token);
                discriminatorStack.Push(Which.Trivia);
            }

            public void PushToken(ref SyntaxNodeOrToken value)
            {
                tokenStack.Push(value);
                discriminatorStack.Push(Which.Token);
            }

            public void Dispose()
            {
                this.nodeStack.Dispose();
                this.triviaStack.Dispose();
                this.tokenStack?.Free();
                this.discriminatorStack?.Free();
            }
        
        }

        private IEnumerable<SyntaxNode> DescendantNodesOnly(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren, bool includeSelf)
        {
            if (includeSelf && IsInSpan(ref span, this.FullSpan))
            {
                yield return this;
            }

            using (var stack = new ChildSyntaxListEnumeratorStack(this, descendIntoChildren))
            {
                while (stack.IsNotEmpty)
                {
                    SyntaxNode nodeValue = stack.TryGetNextAsNodeInSpan(ref span);
                    if (nodeValue != null)
                    {
                        // PERF: Push before yield return so that "nodeValue" is 'dead' after the yield
                        // and therefore doesn't need to be stored in the iterator state machine. This
                        // saves a field.
                        stack.PushChildren(nodeValue, descendIntoChildren);

                        yield return nodeValue;
                    }
                }
            }
        }

        private IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokensOnly(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren, bool includeSelf)
        {
            if (includeSelf && IsInSpan(ref span, this.FullSpan))
            {
                yield return this;
            }

            using (var stack = new ChildSyntaxListEnumeratorStack(this, descendIntoChildren))
            {
                while (stack.IsNotEmpty)
                {
                    SyntaxNodeOrToken value;
                    if (stack.TryGetNextInSpan(ref span, out value))
                    {
                        // PERF: Push before yield return so that "value" is 'dead' after the yield
                        // and therefore doesn't need to be stored in the iterator state machine. This
                        // saves a field.
                        var nodeValue = value.AsNode();
                        if (nodeValue != null)
                        {
                            stack.PushChildren(nodeValue, descendIntoChildren);
                        }

                        yield return value;
                    }
                }
            }
        }

        private IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokensIntoTrivia(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren, bool includeSelf)
        {
            if (includeSelf && IsInSpan(ref span, this.FullSpan))
            {
                yield return this;
            }

            using (var stack = new ThreeEnumeratorListStack(this, descendIntoChildren))
            {
                while (stack.IsNotEmpty)
                {
                    switch (stack.PeekNext())
                    {
                        case ThreeEnumeratorListStack.Which.Node:
                            SyntaxNodeOrToken value;
                            if (stack.TryGetNextInSpan(ref span, out value))
                            {
                                // PERF: The following code has an unusual structure (note the 'break' out of
                                // the case statement from inside an if body) in order to convince the compiler
                                // that it can save a field in the iterator machinery.
                                if (value.IsNode)
                                {
                                    // parent nodes come before children (prefix document order)
                                    stack.PushChildren(value.AsNode(), descendIntoChildren);
                                }
                                else if (value.IsToken)
                                {
                                    var token = value.AsToken();

                                    // only look through trivia if this node has structured trivia
                                    if (token.HasStructuredTrivia)
                                    {
                                        // trailing trivia comes last
                                        if (token.HasTrailingTrivia)
                                        {
                                            stack.PushTrailingTrivia(ref token);
                                        }

                                        // tokens come between leading and trailing trivia
                                        stack.PushToken(ref value);

                                        // leading trivia comes first
                                        if (token.HasLeadingTrivia)
                                        {
                                            stack.PushLeadingTrivia(ref token);
                                        }

                                        // Exit the case block without yielding (see PERF note above)
                                        break;
                                    }

                                    // no structure trivia, so just yield this token now
                                }

                                // PERF: Yield here (rather than inside the if bodies above) so that it's
                                // obvious to the compiler that 'value' is not used beyond this point and,
                                // therefore, doesn't need to be kept in a field.
                                yield return value;
                            }

                            break;

                        case ThreeEnumeratorListStack.Which.Trivia:
                            // yield structure nodes and enumerate their children
                            SyntaxTrivia trivia;
                            if (stack.TryGetNext(out trivia))
                            {
                                if (trivia.HasStructure && IsInSpan(ref span, trivia.FullSpan))
                                {
                                    var structureNode = trivia.GetStructure();

                                    // parent nodes come before children (prefix document order)

                                    // PERF: Push before yield return so that "structureNode" is 'dead' after the yield
                                    // and therefore doesn't need to be stored in the iterator state machine. This
                                    // saves a field.
                                    stack.PushChildren(structureNode, descendIntoChildren);

                                    yield return structureNode;
                                }
                            }
                            break;

                        case ThreeEnumeratorListStack.Which.Token:
                            yield return stack.PopToken();
                            break;
                    }
                }
            }
        }

        private IEnumerable<SyntaxTrivia> DescendantTriviaOnly(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren)
        {
            using (var stack = new ChildSyntaxListEnumeratorStack(this, descendIntoChildren))
            {
                SyntaxNodeOrToken value;
                while (stack.IsNotEmpty)
                {
                    if (stack.TryGetNextInSpan(ref span, out value))
                    {
                        if (value.IsNode)
                        {
                            var nodeValue = value.AsNode();

                            stack.PushChildren(nodeValue, descendIntoChildren);
                        }
                        else if (value.IsToken)
                        {
                            var token = value.AsToken();

                            foreach (var trivia in token.LeadingTrivia)
                            {
                                if (IsInSpan(ref span, trivia.FullSpan))
                                {
                                    yield return trivia;
                                }
                            }

                            foreach (var trivia in token.TrailingTrivia)
                            {
                                if (IsInSpan(ref span, trivia.FullSpan))
                                {
                                    yield return trivia;
                                }
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<SyntaxTrivia> DescendantTriviaIntoTrivia(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren)
        {
            using (var stack = new TwoEnumeratorListStack(this, descendIntoChildren))
            {
                while (stack.IsNotEmpty)
                {
                    switch (stack.PeekNext())
                    {
                        case TwoEnumeratorListStack.Which.Node:
                            SyntaxNodeOrToken value;
                            if (stack.TryGetNextInSpan(ref span, out value))
                            {
                                if (value.IsNode)
                                {
                                    var nodeValue = value.AsNode();
                                    stack.PushChildren(nodeValue, descendIntoChildren);
                                }
                                else if (value.IsToken)
                                {
                                    var token = value.AsToken();

                                    if (token.HasTrailingTrivia)
                                    {
                                        stack.PushTrailingTrivia(ref token);
                                    }

                                    if (token.HasLeadingTrivia)
                                    {
                                        stack.PushLeadingTrivia(ref token);
                                    }
                                }
                            }

                            break;

                        case TwoEnumeratorListStack.Which.Trivia:
                            // yield structure nodes and enumerate their children
                            SyntaxTrivia trivia;
                            if (stack.TryGetNext(out trivia))
                            {
                                // PERF: Push before yield return so that "trivia" is 'dead' after the yield
                                // and therefore doesn't need to be stored in the iterator state machine. This
                                // saves a field.
                                if (trivia.HasStructure)
                                {
                                    var structureNode = trivia.GetStructure();
                                    stack.PushChildren(structureNode, descendIntoChildren);
                                }

                                if (IsInSpan(ref span, trivia.FullSpan))
                                {
                                    yield return trivia;
                                }
                            }

                            break;
                    }
                }
            }
        }
    }
}
