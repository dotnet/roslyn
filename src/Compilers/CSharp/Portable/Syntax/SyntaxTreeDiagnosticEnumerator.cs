// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An enumerator for diagnostic lists.
    /// </summary>
    internal static class SyntaxTreeDiagnosticEnumerator
    {
        private const int DefaultStackCapacity = 8;

        public static IEnumerable<Diagnostic> EnumerateDiagnostics(SyntaxTree syntaxTree, GreenNode root, int rootStartPosition)
        {
            Debug.Assert(root.ContainsDiagnostics, "Caller should have checked that the root has diagnostics");

            // Note: for the duration of this method, 'currentPosition' is:
            //
            // 1. The FullStart of a node when we're on a node.
            // 2. The FullStart of a trivia when we're on trivia.
            // 3. The FullStart of a list whne we're on a list.
            // 4. The *Start* (not FullStart) of a token when we're on a token.
            //
            // This is because when we hit a token, we will have processed its leading trivia, and will have moved foward.
            //
            // However, the offset for a diagnostic is relative to its FullStart (see SyntaxDiagnosticInfo.Offset).  Because
            // of this, the offset can be directly combined with the position for the first 3, but will need to be adjusted
            // when processing a token itself.
            var currentPosition = rootStartPosition;

            using var stack = new NodeIterationStack(DefaultStackCapacity);
            stack.PushNodeOrToken(root);

            // don't produce locations outside of tree span
            var fullTreeLength = syntaxTree.GetRoot().FullSpan.Length;

            while (stack.Any())
            {
                var node = stack.Top.Node;

                if (!stack.Top.ProcessedDiagnostics)
                {
                    foreach (SyntaxDiagnosticInfo sdi in node.GetDiagnostics())
                    {
                        var span = computeDiagnosticSpan(sdi, node);
                        yield return new CSDiagnostic(sdi, new SourceLocation(syntaxTree, span));
                    }

                    stack.MarkProcessedDiagnosticsForStackTop();
                }

                if (tryContinueWithThisNode(node))
                    continue;

                // Weren't able to continue with this node. Pop it so we continue processing its parent.
                stack.Pop();
            }

            yield break;

            TextSpan computeDiagnosticSpan(SyntaxDiagnosticInfo sdi, GreenNode node)
            {
                if (isMissingNodeOrToken(node))
                {
                    var priorToken = findTokenThatEndsAtCurrentPosition(root, rootStartPosition, currentPosition);
                    if (priorToken != null)
                    {
                        // Missing nodes/tokens are handled specially.  They are reported at the start of the token that
                        // follows them (since that is the information the parser has when creating them).  However, that's
                        // highly undesireable from a UX perspective.  Consider something like:

                        // To accomplish this, we use the previousNonTriviaNode to find the token before us.  And we see if
                        // it had any end-of-lines in it.  If so, we'll move the back as well
                        var previousTrailingTrivia = priorToken.GetTrailingTriviaCore();
                        var widthToSubtract = determineWidthToSubtract(previousTrailingTrivia);
                        if (widthToSubtract > 0)
                        {
                            var trailingTriviaStartPosition = Math.Min(currentPosition - widthToSubtract, fullTreeLength);
                            return new TextSpan(trailingTriviaStartPosition, length: 0);
                        }
                    }

                    var currentToken = findTokenThatStartsAtCurrentPosition(root, rootStartPosition, currentPosition);
                    if (currentToken != null)
                    {
                        var currentTokenStart = Math.Min(currentPosition + currentToken.GetLeadingTriviaWidth(), fullTreeLength);
                        var currentTokenEnd = Math.Min(currentTokenStart + currentToken.Width, fullTreeLength);
                        return TextSpan.FromBounds(currentTokenStart, currentTokenEnd);
                    }
                }

                // Normal case.  

                // For tokens, we've already seen leading trivia (as we push leading/trailing trivia explicit as
                // green nodes to process on the stack), so we have to roll back.  For nodes, we have yet to see the
                // leading trivia, so those don't need an adjustment.
                int leadingWidthAlreadyCounted = node.IsToken ? node.GetLeadingTriviaWidth() : 0;

                var spanStart = Math.Min(currentPosition + sdi.Offset - leadingWidthAlreadyCounted, fullTreeLength);
                var spanEnd = Math.Min(spanStart + sdi.Width, fullTreeLength);

                return TextSpan.FromBounds(spanStart, spanEnd);
            }

            static GreenNode? findTokenThatEndsAtCurrentPosition(GreenNode current, int currentFullStart, int desiredFullEndPosition)
            {
                // If we're moved past the token we're lookign for, we won't it here.
                if (currentFullStart > desiredFullEndPosition)
                    return null;

                if (current.IsToken)
                {
                    // We're looking at a token.  Return it if it's the one that ends at the position we're looking for.
                    return currentFullStart + current.FullWidth == desiredFullEndPosition ? current : null;
                }

                // Otherwise, we're before the token.  Recurse into children, skiping those that end before the position
                // we're looking, and recursing into those that overlap it.
                var currentChildFullStart = currentFullStart;
                for (var i = 0; i < current.SlotCount; i++)
                {
                    var child = current.GetSlot(i);
                    if (child is null || child.FullWidth == 0)
                        continue;

                    var childFullEnd = currentChildFullStart + child.FullWidth;
                    if (childFullEnd < desiredFullEndPosition)
                    {
                        currentChildFullStart += child.FullWidth;
                        continue;
                    }
                    else
                    {
                        return findTokenThatEndsAtCurrentPosition(child, currentChildFullStart, desiredFullEndPosition);
                    }
                }

                return null;
            }

            static GreenNode? findTokenThatStartsAtCurrentPosition(GreenNode? current, int currentFullStart, int desiredFullStartPosition)
            {
                if (current is null)
                    return null;

                // If we're moved past the token we're lookign for, we won't it here.
                if (currentFullStart > desiredFullStartPosition)
                    return null;

                if (current.IsToken)
                {

                    if (currentFullStart == desiredFullStartPosition)
                        return current;

                    // The token we're looking for might be skipped trailing trivia in this current token.
                    if (current.ContainsSkippedText && currentFullStart + current.FullWidth > desiredFullStartPosition)
                    {
                        var trivia = current.GetTrailingTriviaCore();
                        return findTokenThatStartsAtCurrentPosition(trivia, currentFullStart + current.Width, desiredFullStartPosition);
                    }

                    return null;
                }

                // Otherwise, we're before the token.  Recurse into children, skiping those that end before the position
                // we're looking, and recursing into those that overlap it.
                var currentChildFullStart = currentFullStart;
                for (var i = 0; i < current.SlotCount; i++)
                {
                    var child = current.GetSlot(i);
                    if (child is null || child.FullWidth == 0)
                        continue;

                    var childFullEnd = currentChildFullStart + child.FullWidth;
                    if (childFullEnd < desiredFullStartPosition)
                    {
                        currentChildFullStart += child.FullWidth;
                        continue;
                    }
                    else
                    {
                        return findTokenThatStartsAtCurrentPosition(child, currentChildFullStart, desiredFullStartPosition);
                    }
                }

                return null;
            }

            int determineWidthToSubtract([NotNullWhen(true)] GreenNode? trivia)
            {
                // Empty list.
                if (trivia is null)
                    return 0;

                // Singleton list.
                if (trivia.RawKind == (int)SyntaxKind.EndOfLineTrivia)
                    return trivia.FullWidth;

                var containsEndOfLine = false;
                var widthToSubtract = 0;

                for (var i = trivia.SlotCount - 1; i >= 0; i--)
                {
                    var child = trivia.GetSlot(i);
                    if (child is null)
                        continue;

                    if (child.RawKind == (int)SyntaxKind.SkippedTokensTrivia)
                        break;

                    if (child.RawKind == (int)SyntaxKind.EndOfLineTrivia)
                        containsEndOfLine = true;

                    widthToSubtract += child.FullWidth;
                }

                return containsEndOfLine ? widthToSubtract : 0;
            }

            bool isMissingNodeOrToken(GreenNode node)
            {
                if (!node.IsMissing)
                    return false;

                if (node.IsToken)
                    return true;

                return !node.IsList && !node.IsTrivia;
            }

            bool tryContinueWithThisNode(GreenNode node)
            {
                // SlotCount is 0 when we hit tokens or normal trivia. In this case, we just want to move past the
                // normal width of the item.  Importantly, in the case of tokens, we don't want to move the
                // full-width.  That would make us double-count the widths of the leading/trailing trivia which
                // we're walking into as normal green nodes.
                if (node.SlotCount == 0)
                {
                    currentPosition += node.Width;
                }
                else
                {
                    var nextSlotIndex = stack.Top.SlotIndex;
                    while (++nextSlotIndex < node.SlotCount)
                    {
                        var child = node.GetSlot(nextSlotIndex);
                        if (child == null)
                            continue;

                        // If the child doesn't have diagnostics anywhere in it, we can skip it entirely.
                        if (!child.ContainsDiagnostics)
                        {
                            currentPosition += child.FullWidth;
                            continue;
                        }

                        stack.UpdateSlotIndexForStackTop(nextSlotIndex);
                        stack.PushNodeOrToken(child);

                        return true;
                    }
                }

                // Weren't able to continue with this node. Pop it so we continue processing its parent.
                return false;
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
            /// We'll hit nodes multiple times.  First, when we run into them, and next, when we've processed all their
            /// children and are on the way back up.  This tracks whether we've processed the diagnostics already so we
            /// don't do it twice.
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

            public readonly NodeIteration Top
                => this[_count - 1];

            public readonly NodeIteration this[int index]
            {
                get
                {
                    Debug.Assert(_stack != null);
                    Debug.Assert(index >= 0 && index < _count);
                    return _stack[index];
                }
            }

            public readonly void UpdateSlotIndexForStackTop(int slotIndex)
                => _stack[_count - 1].SlotIndex = slotIndex;

            public readonly void MarkProcessedDiagnosticsForStackTop()
                => _stack[_count - 1].ProcessedDiagnostics = true;
        }
    }
}
