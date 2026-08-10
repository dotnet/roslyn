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
    /// Supports filtering and projection via <see cref="Where"/>, <see cref="Select{TNew}"/>,
    /// and <see cref="OfType{T}"/>.
    /// </summary>
    internal readonly ref struct DescendantNodeEnumerable<TResult>(
        SyntaxNode root,
        Func<SyntaxNode, bool>? descendIntoChildren,
        Func<SyntaxNode, bool>? predicate,
        Func<SyntaxNode, TResult>? selector)
    {
        private readonly SyntaxNode _root = root;
        private readonly Func<SyntaxNode, bool>? _descendIntoChildren = descendIntoChildren;
        private readonly Func<SyntaxNode, bool>? _predicate = predicate;
        private readonly Func<SyntaxNode, TResult>? _selector = selector;

        public DescendantNodeEnumerator<TResult> GetEnumerator()
        {
            Debug.Assert(_selector is not null || typeof(TResult) == typeof(SyntaxNode),
                "selector can only be null when TResult is SyntaxNode (identity projection)");

            return new(_root, _descendIntoChildren, _predicate, _selector);
        }

        public DescendantNodeEnumerable<TNew> Select<TNew>(Func<TResult, TNew> newSelector)
        {
            if (_selector is null)
            {
                return new(_root, _descendIntoChildren, _predicate, (Func<SyntaxNode, TNew>)(object)newSelector);
            }

            return Composed(_root, _descendIntoChildren, _predicate, _selector, newSelector);

            static DescendantNodeEnumerable<TNew> Composed(SyntaxNode root, Func<SyntaxNode, bool>? descendIntoChildren, Func<SyntaxNode, bool>? predicate, Func<SyntaxNode, TResult> outerSelector, Func<TResult, TNew> newSelector)
            {
                return new(root, descendIntoChildren, predicate, n => newSelector(outerSelector(n)));
            }
        }

        public DescendantNodeEnumerable<TResult> Where(Func<TResult, bool> newPredicate)
        {
            if (_predicate is null && _selector is null)
            {
                return new(_root, _descendIntoChildren, (Func<SyntaxNode, bool>)(object)newPredicate, _selector);
            }

            return Composed(_root, _descendIntoChildren, _predicate, _selector, newPredicate);

            static DescendantNodeEnumerable<TResult> Composed(SyntaxNode root, Func<SyntaxNode, bool>? descendIntoChildren, Func<SyntaxNode, bool>? outerPredicate, Func<SyntaxNode, TResult>? outerSelector, Func<TResult, bool> newPredicate)
            {
                if (outerSelector is null)
                {
                    // No selector: compose predicate directly on the raw node.
                    Func<SyntaxNode, bool> composedPredicate = outerPredicate is not null
                        ? n => outerPredicate(n) && newPredicate((TResult)(object)n!)
                        : throw new InvalidOperationException(); // unreachable: (null, null) handled by caller

                    return new(root, descendIntoChildren, composedPredicate, outerSelector);
                }
                else
                {
                    // Selector exists: evaluate it once in the predicate, cache the result,
                    // and use a trivial selector that returns the cached value.
                    TResult cached = default!;

                    Func<SyntaxNode, bool> composedPredicate = outerPredicate is not null
                        ? n => outerPredicate(n) && newPredicate(cached = outerSelector(n))
                        : n => newPredicate(cached = outerSelector(n));

                    return new(root, descendIntoChildren, composedPredicate, _ => cached);
                }
            }
        }

        public DescendantNodeEnumerable<T> OfType<T>() where T : SyntaxNode
            => Where(static n => n is T).Select(static n => (T)(object)n!);

        public bool Any(Func<TResult, bool> predicate)
        {
            foreach (var item in this)
            {
                if (predicate(item))
                {
                    return true;
                }
            }

            return false;
        }

        public TResult? FirstOrDefault(Func<TResult, bool>? predicate = null)
        {
            foreach (var item in this)
            {
                if (predicate is null || predicate(item))
                {
                    return item;
                }
            }

            return default;
        }

        public TResult? LastOrDefault(Func<TResult, bool>? predicate = null)
        {
            TResult? last = default;

            foreach (var item in this)
            {
                if (predicate is null || predicate(item))
                {
                    last = item;
                }
            }

            return last;
        }

        public ImmutableArray<TResult> ToImmutableArray()
        {
            using var builder = new PooledArrayBuilder<TResult>();

            foreach (var item in this)
            {
                builder.Add(item);
            }

            return builder.ToImmutableAndClear();
        }
    }

    /// <summary>
    /// A struct-based enumerator that iterates descendant nodes without allocating a state machine.
    /// Uses a pooled stack internally.
    /// </summary>
    internal ref struct DescendantNodeEnumerator<TResult>(
        SyntaxNode root,
        Func<SyntaxNode, bool>? descendIntoChildren,
        Func<SyntaxNode, bool>? predicate,
        Func<SyntaxNode, TResult>? selector)
    {
        private ChildSyntaxListEnumeratorStack _stack = new(root, descendIntoChildren);
        private readonly Func<SyntaxNode, bool>? _descendIntoChildren = descendIntoChildren;
        private readonly Func<SyntaxNode, bool>? _predicate = predicate;
        private readonly Func<SyntaxNode, TResult>? _selector = selector;
        private readonly TextSpan _span = root.Span;
        private TResult? _current = default;

        public readonly TResult Current => _current!;

        public bool MoveNext()
        {
            while (_stack.IsNotEmpty)
            {
                var node = _stack.TryGetNextAsNodeInSpan(in _span);
                if (node != null)
                {
                    _stack.PushChildren(node, _descendIntoChildren);

                    if (_predicate is null || _predicate(node))
                    {
                        _current = _selector is not null
                            ? _selector(node)
                            : (TResult)(object)node;
                        return true;
                    }
                }
            }

            return false;
        }

        public void Dispose()
        {
            _stack.Dispose();
            _stack = default;
        }
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

        public void Dispose()
        {
            _stack.Dispose();
            _stack = default;
        }
    }
}
