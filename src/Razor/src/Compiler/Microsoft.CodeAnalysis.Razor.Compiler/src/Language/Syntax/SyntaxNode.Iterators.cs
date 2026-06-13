// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class SyntaxNode
{
    private IEnumerable<SyntaxNode> DescendantNodesImpl(TextSpan span, Func<SyntaxNode, bool>? descendIntoChildren, bool includeSelf)
    {
        if (includeSelf && IsInSpan(in span, Span))
        {
            yield return this;
        }

        using var stack = new ChildSyntaxListEnumeratorStack(this, descendIntoChildren);

        while (stack.IsNotEmpty)
        {
            var node = stack.TryGetNextAsNodeInSpan(in span);
            if (node != null)
            {
                // PERF: Push before yield return so that "node" is 'dead' after the yield
                // and therefore doesn't need to be stored in the iterator state machine. This
                // saves a field.
                stack.PushChildren(node, descendIntoChildren);

                yield return node;
            }
        }
    }

    private IEnumerable<SyntaxNodeOrToken> DescendantNodesAndTokensImpl(TextSpan span, Func<SyntaxNode, bool>? descendIntoChildren, bool includeSelf)
    {
        if (includeSelf && IsInSpan(in span, Span))
        {
            yield return this;
        }

        using var stack = new ChildSyntaxListEnumeratorStack(this, descendIntoChildren);

        while (stack.IsNotEmpty)
        {
            if (stack.TryGetNextInSpan(in span, out var value))
            {
                if (value.IsNode)
                {
                    // PERF: Push before yield return so that "value" is 'dead' after the yield
                    // and therefore doesn't need to be stored in the iterator state machine. This
                    // saves a field.
                    stack.PushChildren(value.AsNode()!, descendIntoChildren);
                }

                yield return value;
            }
        }
    }

    private static bool IsInSpan(in TextSpan span, TextSpan childSpan)
    {
        return span.OverlapsWith(childSpan)
            // special case for zero-width tokens (OverlapsWith never returns true for these)
            || (childSpan.Length == 0 && span.IntersectsWith(childSpan));
    }

    private struct ChildSyntaxListEnumeratorStack : IDisposable
    {
        private sealed class Policy : IPooledObjectPolicy<ChildSyntaxList.Enumerator[]>
        {
            public static readonly Policy Instance = new();

            private Policy()
            {
            }

            public ChildSyntaxList.Enumerator[] Create() => new ChildSyntaxList.Enumerator[16];

            public bool Return(ChildSyntaxList.Enumerator[] stack)
            {
                // Return only reasonably-sized stacks to the pool.
                if (stack.Length < 256)
                {
                    Array.Clear(stack, 0, stack.Length);
                    return true;
                }

                return false;
            }
        }

        private static readonly ObjectPool<ChildSyntaxList.Enumerator[]> StackPool = DefaultPool.Create(Policy.Instance);

        private ChildSyntaxList.Enumerator[]? _stack;
        private int _stackPtr;

        public ChildSyntaxListEnumeratorStack(SyntaxNode startingNode, Func<SyntaxNode, bool>? descendIntoChildren)
        {
            if (descendIntoChildren == null || descendIntoChildren(startingNode))
            {
                _stack = StackPool.Get();
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
            Debug.Assert(_stack != null);

            while (_stack[_stackPtr].TryMoveNextAndGetCurrent(out value))
            {
                if (IsInSpan(in span, value.Span))
                {
                    return true;
                }
            }

            _stackPtr--;
            return false;
        }

        public SyntaxNode? TryGetNextAsNodeInSpan(in TextSpan span)
        {
            Debug.Assert(_stack != null);

            SyntaxNode? nodeValue;
            while ((nodeValue = _stack[_stackPtr].TryMoveNextAndGetCurrentAsNode()) != null)
            {
                if (IsInSpan(in span, nodeValue.Span))
                {
                    return nodeValue;
                }
            }

            _stackPtr--;
            return null;
        }

        public void PushChildren(SyntaxNode node)
        {
            Debug.Assert(_stack != null);

            if (++_stackPtr >= _stack.Length)
            {
                // Geometric growth
                Array.Resize(ref _stack, checked(_stackPtr * 2));
            }

            _stack[_stackPtr].InitializeFrom(node);
        }

        public void PushChildren(SyntaxNode node, Func<SyntaxNode, bool>? descendIntoChildren)
        {
            if (descendIntoChildren == null || descendIntoChildren(node))
            {
                PushChildren(node);
            }
        }

        public void Dispose()
        {
            if (_stack is { } stack)
            {
                StackPool.Return(stack);
            }
        }
    }

    /// <summary>
    /// A struct-based enumerable that iterates descendant nodes without allocating a state machine.
    /// </summary>
    internal readonly ref struct DescendantNodeEnumerable(SyntaxNode root, Func<SyntaxNode, bool>? descendIntoChildren)
    {
        private readonly SyntaxNode _root = root;
        private readonly Func<SyntaxNode, bool>? _descendIntoChildren = descendIntoChildren;

        public DescendantNodeEnumerator GetEnumerator() => new(_root, _descendIntoChildren);

        public DescendantNodeSelectWhereEnumerable<T> OfType<T>() where T : SyntaxNode
            => new(_root, _descendIntoChildren, static n => n is T, static n => (T)n);

        public bool Any(Func<SyntaxNode, bool> predicate)
        {
            foreach (var node in this)
            {
                if (predicate(node))
                {
                    return true;
                }
            }

            return false;
        }

        public SyntaxNode? FirstOrDefault(Func<SyntaxNode, bool> predicate)
        {
            foreach (var node in this)
            {
                if (predicate(node))
                {
                    return node;
                }
            }

            return null;
        }

        public SyntaxNode? LastOrDefault(Func<SyntaxNode, bool> predicate)
        {
            SyntaxNode? last = null;

            foreach (var node in this)
            {
                if (predicate(node))
                {
                    last = node;
                }
            }

            return last;
        }
    }

    /// <summary>
    /// A struct-based enumerable that iterates descendant nodes with a filter and projection.
    /// </summary>
    internal readonly ref struct DescendantNodeSelectWhereEnumerable<TResult>(
        SyntaxNode root,
        Func<SyntaxNode, bool>? descendIntoChildren,
        Func<SyntaxNode, bool> predicate,
        Func<SyntaxNode, TResult> selector)
    {
        private readonly SyntaxNode _root = root;
        private readonly Func<SyntaxNode, bool>? _descendIntoChildren = descendIntoChildren;
        private readonly Func<SyntaxNode, bool> _predicate = predicate;
        private readonly Func<SyntaxNode, TResult> _selector = selector;

        public readonly DescendantNodeSelectWhereEnumerator<TResult> GetEnumerator()
            => new(_root, _descendIntoChildren, _predicate, _selector);

        public readonly TResult? FirstOrDefault(Func<TResult, bool>? predicate = null)
        {
            using var enumerator = new DescendantNodeSelectWhereEnumerator<TResult>(_root, _descendIntoChildren, _predicate, _selector);

            while (enumerator.MoveNext())
            {
                if (predicate is null || predicate(enumerator.Current))
                {
                    return enumerator.Current;
                }
            }

            return default;
        }

        public readonly ImmutableArray<TOut> SelectWhereAsArray<TOut>(Func<TResult, TOut> projection, Func<TResult, bool>? filter = null)
        {
            using var builder = new PooledArrayBuilder<TOut>();
            using var enumerator = new DescendantNodeSelectWhereEnumerator<TResult>(_root, _descendIntoChildren, _predicate, _selector);

            while (enumerator.MoveNext())
            {
                if (filter is null || filter(enumerator.Current))
                {
                    builder.Add(projection(enumerator.Current));
                }
            }

            return builder.ToImmutableAndClear();
        }
    }

    /// <summary>
    /// A struct-based enumerator that iterates descendant nodes without allocating a state machine.
    /// Uses a pooled stack internally (same as <see cref="DescendantNodes()"/>).
    /// </summary>
    internal ref struct DescendantNodeEnumerator(SyntaxNode root, Func<SyntaxNode, bool>? descendIntoChildren)
    {
        private ChildSyntaxListEnumeratorStack _stack = new(root, descendIntoChildren);
        private readonly Func<SyntaxNode, bool>? _descendIntoChildren = descendIntoChildren;
        private readonly TextSpan _span = root.Span;
        private SyntaxNode? _current = null;

        public readonly SyntaxNode Current => _current!;

        public bool MoveNext()
        {
            while (_stack.IsNotEmpty)
            {
                var node = _stack.TryGetNextAsNodeInSpan(in _span);
                if (node != null)
                {
                    _stack.PushChildren(node, _descendIntoChildren);
                    _current = node;
                    return true;
                }
            }

            return false;
        }

        public void Dispose() => _stack.Dispose();
    }

    /// <summary>
    /// A struct-based enumerator that iterates descendant nodes with a filter and projection.
    /// </summary>
    internal ref struct DescendantNodeSelectWhereEnumerator<TResult>(
        SyntaxNode root,
        Func<SyntaxNode, bool>? descendIntoChildren,
        Func<SyntaxNode, bool> predicate,
        Func<SyntaxNode, TResult> selector)
    {
        private DescendantNodeEnumerator _inner = new(root, descendIntoChildren);
        private readonly Func<SyntaxNode, bool> _predicate = predicate;
        private readonly Func<SyntaxNode, TResult> _selector = selector;

        public TResult Current { get; private set; } = default!;

        public bool MoveNext()
        {
            while (_inner.MoveNext())
            {
                if (_predicate(_inner.Current))
                {
                    Current = _selector(_inner.Current);
                    return true;
                }
            }

            return false;
        }

        public void Dispose() => _inner.Dispose();
    }

    /// <summary>
    /// A struct-based enumerable that iterates descendant tokens without allocating a state machine.
    /// </summary>
    internal readonly ref struct DescendantTokenEnumerable(SyntaxNode root, Func<SyntaxNode, bool>? descendIntoChildren)
    {
        private readonly SyntaxNode _root = root;
        private readonly Func<SyntaxNode, bool>? _descendIntoChildren = descendIntoChildren;

        public DescendantTokenEnumerator GetEnumerator() => new(_root, _descendIntoChildren);

        public readonly bool Any(Func<SyntaxToken, bool> predicate)
        {
            foreach (var token in this)
            {
                if (predicate(token))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly ImmutableArray<SyntaxToken> ToImmutableArray()
        {
            using var builder = new PooledArrayBuilder<SyntaxToken>();

            foreach (var token in this)
            {
                builder.Add(token);
            }

            return builder.ToImmutableAndClear();
        }
    }

    /// <summary>
    /// A struct-based enumerator that iterates descendant tokens without allocating a state machine.
    /// </summary>
    internal ref struct DescendantTokenEnumerator(SyntaxNode root, Func<SyntaxNode, bool>? descendIntoChildren)
    {
        private ChildSyntaxListEnumeratorStack _stack = new(root, descendIntoChildren);
        private readonly Func<SyntaxNode, bool>? _descendIntoChildren = descendIntoChildren;
        private readonly TextSpan _span = root.Span;
        private SyntaxToken _current = default;

        public readonly SyntaxToken Current => _current;

        public bool MoveNext()
        {
            while (_stack.IsNotEmpty)
            {
                if (_stack.TryGetNextInSpan(in _span, out var value))
                {
                    if (value.IsNode)
                    {
                        _stack.PushChildren(value.AsNode()!, _descendIntoChildren);
                    }
                    else
                    {
                        _current = value.AsToken();
                        return true;
                    }
                }
            }

            return false;
        }

        public void Dispose() => _stack.Dispose();
    }
}
