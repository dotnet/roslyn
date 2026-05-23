// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public abstract partial class IntermediateNodeWalker
{
    /// <summary>
    ///  Simple stack implementation that fills an array from end-to-start
    ///  to keep the items in the reverse order they were pushed. This ensures
    ///  that they are in the correct order to implement the <see cref="Ancestors"/>
    ///  property.
    /// </summary>
    private struct Stack
    {
        private const int InitialStackSize = 4;

        private IntermediateNode[]? _stack;
        private int _stackPointer;

        [MemberNotNullWhen(false, nameof(_stack))]
        public readonly bool IsEmpty
            => _stack is null || _stackPointer == _stack.Length;

        public readonly ReadOnlySpan<IntermediateNode> Span
            => IsEmpty
                ? ReadOnlySpan<IntermediateNode>.Empty
                : _stack.AsSpan()[_stackPointer..];

        public void Push(IntermediateNode node)
        {
            if (_stack is null || _stackPointer == 0)
            {
                Grow();
            }

            _stack[--_stackPointer] = node;
        }

        public void Pop()
        {
            Debug.Assert(!IsEmpty);

            _stack[_stackPointer++] = null!;
        }

        [MemberNotNull(nameof(_stack))]
        private void Grow()
        {
            // If we haven't initialized the stack, do so and set the stack pointer.
            if (_stack == null)
            {
                _stack = new IntermediateNode[InitialStackSize];
                _stackPointer = _stack.Length;
                return;
            }

            // Otherwise, double the size of the stack and copy the contents over.

            Debug.Assert(_stackPointer == 0, "We should only grow the ancestor stack when the stack pointer reaches 0.");

            var length = _stack.Length;
            var newStack = new IntermediateNode[length * 2];

            _stack.CopyTo(newStack, length);

            _stackPointer = length;
            _stack = newStack;
        }
    }
}
