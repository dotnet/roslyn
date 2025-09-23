// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An enumerator for diagnostic lists.
    /// </summary>
    internal static class SyntaxTreeDiagnosticEnumerator
    {
        private const int DefaultStackCapacity = 8;

        public static IEnumerable<Diagnostic> EnumerateDiagnostics(SyntaxTree syntaxTree, GreenNode root, int position)
        {
            // Note:during iteration, '_position' is:
            // <list type="number">
            // <item>The FullStart of a node when we're on a node.</item>
            // <item>The FullStart of a trivia when we're on trivia.</item>
            // <item>The FullStart of a list when we're on a list.</item>
            // <item>The *Start* (not FullStart) of a token when we're on a token.</item>
            // </list>
            //
            // The last is because when we hit a token, we will have processed its leading trivia, and will have moved
            // forward.
            // <para/>However, the offset for a diagnostic is relative to its FullStart (see <see
            // cref="SyntaxDiagnosticInfo.Offset"/>). Because of this, the offset can be directly combined with the
            // position for the first 3, but will need to be adjusted when processing a token itself.

            Debug.Assert(root.ContainsDiagnostics, "Caller should have checked that the root has diagnostics");

            using var stack = new NodeIterationStack(DefaultStackCapacity);
            stack.PushNodeOrToken(root);

            while (stack.Any())
            {
                var node = stack.Top.Node;

                if (!stack.Top.ProcessedDiagnostics)
                {
                    foreach (SyntaxDiagnosticInfo sdi in node.GetDiagnostics())
                    {
                        // For tokens, we've already seen leading trivia (as we push leading/trailing trivia explicit as
                        // green nodes to process on the stack), so we have to roll back.  For nodes, we have yet to see the
                        // leading trivia, so those don't need an adjustment.
                        int leadingWidthAlreadyCounted = node.IsToken ? node.GetLeadingTriviaWidth() : 0;

                        // don't produce locations outside of tree span
                        var length = syntaxTree.GetRoot().FullSpan.Length;
                        var spanStart = Math.Min(position - leadingWidthAlreadyCounted + sdi.Offset, length);
                        var spanWidth = Math.Min(spanStart + sdi.Width, length) - spanStart;

                        yield return new CSDiagnostic(sdi, new SourceLocation(syntaxTree, new TextSpan(spanStart, spanWidth)));
                    }

                    stack.Top.ProcessedDiagnostics = true;
                }

                processNode(node);
            }

            yield break;

            void processNode(GreenNode node)
            {
                // SlotCount is 0 when we hit tokens or normal trivia. In this case, we just want to move past the
                // normal width of the item.  Importantly, in the case of tokens, we don't want to move the full-width.
                // That would make us double-count the widths of the leading/trailing trivia which we're walking into as
                // normal green nodes.
                //
                // As this has no children and has had its diagnostics processed, pop it and process its parent.
                if (node.SlotCount == 0)
                {
                    position += node.Width;
                }
                else
                {
                    for (var nextSlotIndex = stack.Top.SlotIndex + 1; nextSlotIndex < node.SlotCount; nextSlotIndex++)
                    {
                        var child = node.GetSlot(nextSlotIndex);
                        if (child == null)
                            continue;

                        // If the child doesn't have diagnostics anywhere in it, we can skip it entirely.
                        if (!child.ContainsDiagnostics)
                        {
                            position += child.FullWidth;
                            continue;
                        }

                        stack.Top.SlotIndex = nextSlotIndex;
                        stack.PushNodeOrToken(child);

                        // Pushed a child to process, return out to continue processing it.
                        return;
                    }
                }

                // Done with this node. Pop it so we continue processing its parent.
                stack.Pop();
            }
        }

        private struct NodeIteration(GreenNode node)
        {
            /// <summary>
            /// The node we're on.
            /// </summary>
            public readonly GreenNode Node = node;

            /// <summary>
            /// The index of the child of <see cref="Node"/> that we're on. Initially at -1 as we're pointing at the
            /// node itself, not any children.
            /// </summary>
            public int SlotIndex = -1;

            /// <summary>
            /// We'll hit nodes multiple times.  First, when we run into them, and then after processing each chiild and
            /// popping back up to it.  This tracks whether we've processed the diagnostics already so we don't do it
            /// twice.
            /// </summary>
            public bool ProcessedDiagnostics;
        }

        private struct NodeIterationStack(int capacity) : IDisposable
        {
            private NodeIteration[] _stack = ArrayPool<NodeIteration>.Shared.Rent(capacity);
            private int _count;

            public readonly void Dispose()
                => ArrayPool<NodeIteration>.Shared.Return(_stack, clearArray: true);

            public void PushNodeOrToken(GreenNode node)
            {
                if (node is Syntax.InternalSyntax.SyntaxToken token)
                {
                    PushToken(token);
                }
                else
                {
                    Push(node);
                }
            }

            private void PushToken(Syntax.InternalSyntax.SyntaxToken token)
            {
                // Push in reverse order of processing.
                this.Push(token.GetTrailingTrivia());
                this.Push(token);
                this.Push(token.GetLeadingTrivia());
            }

            private void Push(GreenNode? node)
            {
                if (node is null)
                    return;

                if (_count >= _stack.Length)
                {
                    var tmp = ArrayPool<NodeIteration>.Shared.Rent(_stack.Length * 2);
                    Array.Copy(_stack, tmp, _stack.Length);
                    ArrayPool<NodeIteration>.Shared.Return(_stack, clearArray: true);
                    _stack = tmp;
                }

                _stack[_count] = new NodeIteration(node);
                _count++;
            }

            public void Pop()
                => _count--;

            public readonly bool Any()
                => _count > 0;

            public readonly ref NodeIteration Top
                => ref _stack[_count - 1];
        }
    }
}
