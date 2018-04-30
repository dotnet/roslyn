// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class SyntaxNode
    {
        private IEnumerable<SyntaxNode> DescendantNodesImpl(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren, bool descendIntoTrivia, bool includeSelf)
        {
            return descendIntoTrivia
                ? DescendantNodesAndTokensImpl(span, descendIntoChildren, true, includeSelf).Where(e => e.IsNode).Select(e => e.AsNode())
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

        private static bool IsInSpan(in TextSpan span, TextSpan childSpan)
        {
            return span.OverlapsWith(childSpan)
                // special case for zero-width tokens (OverlapsWith never returns true for these)
                || (childSpan.Length == 0 && span.IntersectsWith(childSpan));
        }

        private struct ChildSyntaxListEnumeratorStack : IDisposable
        {
            private static readonly ObjectPool<ChildSyntaxList.Enumerator[]> s_stackPool = new ObjectPool<ChildSyntaxList.Enumerator[]>(() => new ChildSyntaxList.Enumerator[16]);

            private ChildSyntaxList.Enumerator[] _stack;
            private int _stackPtr;

            public ChildSyntaxListEnumeratorStack(SyntaxNode startingNode, Func<SyntaxNode, bool> descendIntoChildren)
            {
                if (descendIntoChildren == null || descendIntoChildren(startingNode))
                {
                    _stack = s_stackPool.Allocate();
                    _stackPtr = 0;
                    _stack[0].InitializeFrom(startingNode);
                }
                else
                {
                    _stack = null;
                    _stackPtr = -1;
                }
            }

            public bool IsNotEmpty { get { return _stackPtr >= 0; } }

            public bool TryGetNextInSpan(in TextSpan span, out SyntaxNodeOrToken value)
            {
                while (_stack[_stackPtr].TryMoveNextAndGetCurrent(out value))
                {
                    if (IsInSpan(in span, value.FullSpan))
                    {
                        return true;
                    }
                }

                _stackPtr--;
                return false;
            }

            public SyntaxNode TryGetNextAsNodeInSpan(in TextSpan span)
            {
                SyntaxNode nodeValue;
                while ((nodeValue = _stack[_stackPtr].TryMoveNextAndGetCurrentAsNode()) != null)
                {
                    if (IsInSpan(in span, nodeValue.FullSpan))
                    {
                        return nodeValue;
                    }
                }

                _stackPtr--;
                return null;
            }

            public void PushChildren(SyntaxNode node)
            {
                if (++_stackPtr >= _stack.Length)
                {
                    // Geometric growth
                    Array.Resize(ref _stack, checked(_stackPtr * 2));
                }

                _stack[_stackPtr].InitializeFrom(node);
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
                if (_stack?.Length < 256)
                {
                    Array.Clear(_stack, 0, _stack.Length);
                    s_stackPool.Free(_stack);
                }
            }
        }

        private struct TriviaListEnumeratorStack : IDisposable
        {
            private static readonly ObjectPool<SyntaxTriviaList.Enumerator[]> s_stackPool = new ObjectPool<SyntaxTriviaList.Enumerator[]>(() => new SyntaxTriviaList.Enumerator[16]);

            private SyntaxTriviaList.Enumerator[] _stack;
            private int _stackPtr;

            public bool TryGetNext(out SyntaxTrivia value)
            {
                if (_stack[_stackPtr].TryMoveNextAndGetCurrent(out value))
                {
                    return true;
                }

                _stackPtr--;
                return false;
            }

            public void PushLeadingTrivia(in SyntaxToken token)
            {
                Grow();
                _stack[_stackPtr].InitializeFromLeadingTrivia(in token);
            }

            public void PushTrailingTrivia(in SyntaxToken token)
            {
                Grow();
                _stack[_stackPtr].InitializeFromTrailingTrivia(in token);
            }

            private void Grow()
            {
                if (_stack == null)
                {
                    _stack = s_stackPool.Allocate();
                    _stackPtr = -1;
                }

                if (++_stackPtr >= _stack.Length)
                {
                    // Geometric growth
                    Array.Resize(ref _stack, checked(_stackPtr * 2));
                }
            }

            public void Dispose()
            {
                // Return only reasonably-sized stacks to the pool.
                if (_stack?.Length < 256)
                {
                    Array.Clear(_stack, 0, _stack.Length);
                    s_stackPool.Free(_stack);
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

            private ChildSyntaxListEnumeratorStack _nodeStack;
            private TriviaListEnumeratorStack _triviaStack;
            private readonly ArrayBuilder<Which> _discriminatorStack;

            public TwoEnumeratorListStack(SyntaxNode startingNode, Func<SyntaxNode, bool> descendIntoChildren)
            {
                _nodeStack = new ChildSyntaxListEnumeratorStack(startingNode, descendIntoChildren);
                _triviaStack = new TriviaListEnumeratorStack();
                if (_nodeStack.IsNotEmpty)
                {
                    _discriminatorStack = ArrayBuilder<Which>.GetInstance();
                    _discriminatorStack.Push(Which.Node);
                }
                else
                {
                    _discriminatorStack = null;
                }
            }

            public bool IsNotEmpty { get { return _discriminatorStack?.Count > 0; } }

            public Which PeekNext()
            {
                return _discriminatorStack.Peek();
            }

            public bool TryGetNextInSpan(in TextSpan span, out SyntaxNodeOrToken value)
            {
                if (_nodeStack.TryGetNextInSpan(in span, out value))
                {
                    return true;
                }

                _discriminatorStack.Pop();
                return false;
            }

            public bool TryGetNext(out SyntaxTrivia value)
            {
                if (_triviaStack.TryGetNext(out value))
                {
                    return true;
                }

                _discriminatorStack.Pop();
                return false;
            }

            public void PushChildren(SyntaxNode node, Func<SyntaxNode, bool> descendIntoChildren)
            {
                if (descendIntoChildren == null || descendIntoChildren(node))
                {
                    _nodeStack.PushChildren(node);
                    _discriminatorStack.Push(Which.Node);
                }
            }

            public void PushLeadingTrivia(in SyntaxToken token)
            {
                _triviaStack.PushLeadingTrivia(in token);
                _discriminatorStack.Push(Which.Trivia);
            }

            public void PushTrailingTrivia(in SyntaxToken token)
            {
                _triviaStack.PushTrailingTrivia(in token);
                _discriminatorStack.Push(Which.Trivia);
            }

            public void Dispose()
            {
                _nodeStack.Dispose();
                _triviaStack.Dispose();
                _discriminatorStack?.Free();
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

            private ChildSyntaxListEnumeratorStack _nodeStack;
            private TriviaListEnumeratorStack _triviaStack;
            private readonly ArrayBuilder<SyntaxNodeOrToken> _tokenStack;
            private readonly ArrayBuilder<Which> _discriminatorStack;

            public ThreeEnumeratorListStack(SyntaxNode startingNode, Func<SyntaxNode, bool> descendIntoChildren)
            {
                _nodeStack = new ChildSyntaxListEnumeratorStack(startingNode, descendIntoChildren);
                _triviaStack = new TriviaListEnumeratorStack();
                if (_nodeStack.IsNotEmpty)
                {
                    _tokenStack = ArrayBuilder<SyntaxNodeOrToken>.GetInstance();
                    _discriminatorStack = ArrayBuilder<Which>.GetInstance();
                    _discriminatorStack.Push(Which.Node);
                }
                else
                {
                    _tokenStack = null;
                    _discriminatorStack = null;
                }
            }

            public bool IsNotEmpty { get { return _discriminatorStack?.Count > 0; } }

            public Which PeekNext()
            {
                return _discriminatorStack.Peek();
            }

            public bool TryGetNextInSpan(in TextSpan span, out SyntaxNodeOrToken value)
            {
                if (_nodeStack.TryGetNextInSpan(in span, out value))
                {
                    return true;
                }

                _discriminatorStack.Pop();
                return false;
            }

            public bool TryGetNext(out SyntaxTrivia value)
            {
                if (_triviaStack.TryGetNext(out value))
                {
                    return true;
                }

                _discriminatorStack.Pop();
                return false;
            }

            public SyntaxNodeOrToken PopToken()
            {
                _discriminatorStack.Pop();
                return _tokenStack.Pop();
            }

            public void PushChildren(SyntaxNode node, Func<SyntaxNode, bool> descendIntoChildren)
            {
                if (descendIntoChildren == null || descendIntoChildren(node))
                {
                    _nodeStack.PushChildren(node);
                    _discriminatorStack.Push(Which.Node);
                }
            }

            public void PushLeadingTrivia(in SyntaxToken token)
            {
                _triviaStack.PushLeadingTrivia(in token);
                _discriminatorStack.Push(Which.Trivia);
            }

            public void PushTrailingTrivia(in SyntaxToken token)
            {
                _triviaStack.PushTrailingTrivia(in token);
                _discriminatorStack.Push(Which.Trivia);
            }

            public void PushToken(in SyntaxNodeOrToken value)
            {
                _tokenStack.Push(value);
                _discriminatorStack.Push(Which.Token);
            }

            public void Dispose()
            {
                _nodeStack.Dispose();
                _triviaStack.Dispose();
                _tokenStack?.Free();
                _discriminatorStack?.Free();
            }
        }

        private IEnumerable<SyntaxNode> DescendantNodesOnly(TextSpan span, Func<SyntaxNode, bool> descendIntoChildren, bool includeSelf)
        {
            if (includeSelf && IsInSpan(in span, this.FullSpan))
            {
                yield return this;
            }

            using (var stack = new ChildSyntaxListEnumeratorStack(this, descendIntoChildren))
            {
                while (stack.IsNotEmpty)
                {
                    SyntaxNode nodeValue = stack.TryGetNextAsNodeInSpan(in span);
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
            if (includeSelf && IsInSpan(in span, this.FullSpan))
            {
                yield return this;
            }

            using (var stack = new ChildSyntaxListEnumeratorStack(this, descendIntoChildren))
            {
                while (stack.IsNotEmpty)
                {
                    SyntaxNodeOrToken value;
                    if (stack.TryGetNextInSpan(in span, out value))
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
            if (includeSelf && IsInSpan(in span, this.FullSpan))
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
                            if (stack.TryGetNextInSpan(in span, out value))
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
                                            stack.PushTrailingTrivia(in token);
                                        }

                                        // tokens come between leading and trailing trivia
                                        stack.PushToken(in value);

                                        // leading trivia comes first
                                        if (token.HasLeadingTrivia)
                                        {
                                            stack.PushLeadingTrivia(in token);
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
                                if (trivia.HasStructure && IsInSpan(in span, trivia.FullSpan))
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
                while (stack.IsNotEmpty)
                {
                    SyntaxNodeOrToken value;
                    if (stack.TryGetNextInSpan(in span, out value))
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
                                if (IsInSpan(in span, trivia.FullSpan))
                                {
                                    yield return trivia;
                                }
                            }

                            foreach (var trivia in token.TrailingTrivia)
                            {
                                if (IsInSpan(in span, trivia.FullSpan))
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
                            if (stack.TryGetNextInSpan(in span, out value))
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
                                        stack.PushTrailingTrivia(in token);
                                    }

                                    if (token.HasLeadingTrivia)
                                    {
                                        stack.PushLeadingTrivia(in token);
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

                                if (IsInSpan(in span, trivia.FullSpan))
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
