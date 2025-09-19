// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An enumerator for diagnostic lists.
    /// </summary>
    internal struct SyntaxTreeDiagnosticEnumerator
    {
        private const int DefaultStackCapacity = 8;

        private readonly SyntaxTree? _syntaxTree;
        private NodeIterationStack _stack;
        private Diagnostic? _current;
        private int _position;

        internal SyntaxTreeDiagnosticEnumerator(SyntaxTree syntaxTree, GreenNode? node, int position)
        {
            _syntaxTree = null;
            _current = null;
            _position = position;
            if (node != null && node.ContainsDiagnostics)
            {
                _syntaxTree = syntaxTree;
                _stack = new NodeIterationStack(DefaultStackCapacity);
                _stack.PushNodeOrToken(previousTrailingTrivia: null, node);
            }
            else
            {
                _stack = new NodeIterationStack();
            }
        }

        /// <summary>
        /// Moves the enumerator to the next diagnostic instance in the diagnostic list.
        /// </summary>
        /// <returns>Returns true if enumerator moved to the next diagnostic, false if the
        /// enumerator was at the end of the diagnostic list.</returns>
        public bool MoveNext()
        {
            while (_stack.Any())
            {
                var diagIndex = _stack.Top.DiagnosticIndex;
                var node = _stack.Top.Node;
                var diags = node.GetDiagnostics();
                if (diagIndex < diags.Length - 1)
                {
                    diagIndex++;
                    var sdi = (SyntaxDiagnosticInfo)diags[diagIndex];

                    //for tokens, we've already seen leading trivia on the stack, so we have to roll back
                    //for nodes, we have yet to see the leading trivia

                    // don't produce locations outside of tree span
                    Debug.Assert(_syntaxTree is object);
                    var length = _syntaxTree.GetRoot().FullSpan.Length;
                    var spanStart = Math.Min(_position - leadingWidthAlreadyCounted + sdi.Offset, length);
                    var spanWidth = Math.Min(spanStart + sdi.Width, length) - spanStart;

                    _current = new CSDiagnostic(sdi, new SourceLocation(_syntaxTree, new TextSpan(spanStart, spanWidth)));

                    _stack.UpdateDiagnosticIndexForStackTop(diagIndex);
                    return true;
                }

                // Now that we've consumed all the diagnostics from this top node, it becomes our last seen node.
                _previousNodeOrToken = node;
                var slotIndex = _stack.Top.SlotIndex;
tryAgain:
                if (slotIndex < node.SlotCount - 1)
                {
                    slotIndex++;
                    var child = node.GetSlot(slotIndex);
                    if (child == null)
                    {
                        goto tryAgain;
                    }

                    if (!child.ContainsDiagnostics)
                    {
                        // We're skipping past a node that has no diagnostics.  Set it as the last seen node, as
                        // we may need to introspect it when looking at the next node.
                        _previousNodeOrToken = child;
                        _position += child.FullWidth;
                        goto tryAgain;
                    }

                    _stack.UpdateSlotIndexForStackTop(slotIndex);
                    _stack.PushNodeOrToken(child);
                }
                else
                {
                    if (node.SlotCount == 0)
                    {
                        _position += node.Width;
                    }

                    _stack.Pop();
                }
            }

            return false;
        }

        /// <summary>
        /// The current diagnostic that the enumerator is pointing at.
        /// </summary>
        public Diagnostic Current
        {
            get { Debug.Assert(_current is object); return _current; }
        }

        private struct NodeIteration
        {
            internal readonly GreenNode Node;
            internal readonly GreenNode? PreviousTrailingTrivia;
            internal int DiagnosticIndex;
            internal int SlotIndex;

            internal NodeIteration(GreenNode? previousTrailingTrivia, GreenNode node)
            {
                this.Node = node;
                this.PreviousTrailingTrivia = previousTrailingTrivia;

#if DEBUG
                if (this.PreviousTrailingTrivia is not null)
                {
                    Debug.Assert(!this.PreviousTrailingTrivia.IsTrivia);
                }

                this.SlotIndex = -1;
                this.DiagnosticIndex = -1;
            }
        }

        private struct NodeIterationStack
        {
            private NodeIteration[] _stack;
            private int _count;

            internal NodeIterationStack(int capacity)
            {
                Debug.Assert(capacity > 0);
                _stack = new NodeIteration[capacity];
                _count = 0;
            }

            internal void PushNodeOrToken(GreenNode? previousTrailingTrivia, GreenNode node)
            {
                if (node is Syntax.InternalSyntax.SyntaxToken token)
                {
                    PushToken(previousTrailingTrivia, token);
                }
                else
                {
                    Push(previousTrailingTrivia, node);
                }
            }

            private void PushToken(GreenNode? previousTrailingTrivia, Syntax.InternalSyntax.SyntaxToken token)
            {
                var trailing = token.GetTrailingTrivia();
                if (trailing != null)
                {
                    this.Push(null, trailing);
                }

                this.Push(previousTrailingTrivia, token);
                var leading = token.GetLeadingTrivia();
                if (leading != null)
                {
                    this.Push(null, leading);
                }
            }

            private void Push(GreenNode? previousTrailingTrivia, GreenNode node)
            {
                if (_count >= _stack.Length)
                {
                    var tmp = new NodeIteration[_stack.Length * 2];
                    Array.Copy(_stack, tmp, _stack.Length);
                    _stack = tmp;
                }

                _stack[_count] = new NodeIteration(previousTrailingTrivia, node);
                _count++;
            }

            internal void Pop()
            {
                _count--;
            }

            internal bool Any()
            {
                return _count > 0;
            }

            internal NodeIteration Top
            {
                get
                {
                    return this[_count - 1];
                }
            }

            internal NodeIteration this[int index]
            {
                get
                {
                    Debug.Assert(_stack != null);
                    Debug.Assert(index >= 0 && index < _count);
                    return _stack[index];
                }
            }

            internal void UpdateSlotIndexForStackTop(int slotIndex)
            {
                Debug.Assert(_stack != null);
                Debug.Assert(_count > 0);
                _stack[_count - 1].SlotIndex = slotIndex;
            }

            internal void UpdateDiagnosticIndexForStackTop(int diagnosticIndex)
            {
                Debug.Assert(_stack != null);
                Debug.Assert(_count > 0);
                _stack[_count - 1].DiagnosticIndex = diagnosticIndex;
            }
        }
    }
}
