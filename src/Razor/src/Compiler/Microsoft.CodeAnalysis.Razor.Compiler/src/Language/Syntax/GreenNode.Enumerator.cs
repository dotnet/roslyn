// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class GreenNode
{
    /// <summary>
    ///  Provides a depth-first enumerator for traversing <see cref="GreenNode"/> syntax trees.
    ///  List nodes are treated as "transparent" and are not returned during enumeration,
    ///  only their children are processed.
    /// </summary>
    /// <remarks>
    ///  This enumerator uses a stack-based approach to traverse the syntax tree without recursion.
    ///  It pushes child nodes in reverse order to maintain correct left-to-right traversal order.
    ///  The enumerator must be disposed to release the underlying stack resources.
    /// </remarks>
    public ref struct Enumerator
    {
        private MemoryBuilder<GreenNode> _stack;
        private GreenNode? _current;

        public Enumerator(GreenNode node)
        {
            // MemoryBuilder<T> uses ArrayPool<T>.Shared to acquire an array.
            // So, we set an initial capacity of 256 to avoid unnecessary growth.
            // In addition, because the arrays used by MemoryBuilder<T> will be
            // returned to the pool, we clear them to ensure that GreenNodes aren't
            // kept in memory.
            _stack = new(initialCapacity: 256, clearArray: true);
            _stack.Push(node);
        }

        public void Dispose()
        {
            _stack.Dispose();
        }

        public readonly GreenNode Current => _current!;

        public bool MoveNext()
        {
            while (_stack.TryPop(out var node))
            {
                // Push children onto the stack in reverse order for correct traversal order.
                for (var i = node.SlotCount - 1; i >= 0; i--)
                {
                    var child = node.GetSlot(i);
                    if (child != null)
                    {
                        _stack.Push(child);
                    }
                }

                // If this is not a list node, return it as the current item.
                // List nodes are "transparent" - we push their children but don't return the list itself.
                if (!node.IsList)
                {
                    _current = node;
                    return true;
                }
            }

            // Stack is empty, no more nodes to process
            _current = null;
            return false;
        }
    }
}
