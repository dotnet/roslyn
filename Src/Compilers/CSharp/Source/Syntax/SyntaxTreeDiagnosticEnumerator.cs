// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An enumerator for diagnostic lists.
    /// </summary>
    internal struct SyntaxTreeDiagnosticEnumerator
    {
        private readonly SyntaxTree syntaxTree;
        private NodeIterationStack stack;
        private Diagnostic current;
        private int position;
        private const int DefaultStackCapacity = 8;

        internal SyntaxTreeDiagnosticEnumerator(SyntaxTree syntaxTree, GreenNode node, int position)
        {
            this.syntaxTree = null;
            this.current = null;
            this.position = position;
            if (node != null && node.ContainsDiagnostics)
            {
                this.syntaxTree = syntaxTree;
                this.stack = new NodeIterationStack(DefaultStackCapacity);
                this.stack.PushNodeOrToken(node);
            }
            else
            {
                this.stack = new NodeIterationStack();
            }
        }

        /// <summary>
        /// Moves the enumerator to the next diagnostic instance in the diagnostic list.
        /// </summary>
        /// <returns>Returns true if enumerator moved to the next diagnostic, false if the
        /// enumerator was at the end of the diagnostic list.</returns>
        public bool MoveNext()
        {
            while (this.stack.Any())
            {
                var diagIndex = this.stack.Top.DiagnosticIndex;
                var node = this.stack.Top.Node;
                var diags = node.GetDiagnostics();
                if (diagIndex < diags.Length - 1)
                {
                    diagIndex++;
                    var sdi = (SyntaxDiagnosticInfo)diags[diagIndex];

                    //for tokens, we've already seen leading trivia on the stack, so we have to roll back
                    //for nodes, we have yet to see the leading trivia
                    int leadingWidthAlreadyCounted = node.IsToken ? node.GetLeadingTriviaWidth() : 0;

                    current = new CSDiagnostic(sdi, new SourceLocation(this.syntaxTree, new TextSpan(this.position - leadingWidthAlreadyCounted + sdi.Offset, sdi.Width)));

                    stack.UpdateDiagnosticIndexForStackTop(diagIndex);
                    return true;
                }

                var slotIndex = this.stack.Top.SlotIndex;
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
                        this.position += child.FullWidth;
                        goto tryAgain;
                    }

                    this.stack.UpdateSlotIndexForStackTop(slotIndex);
                    this.stack.PushNodeOrToken(child);
                }
                else
                {
                    if (node.SlotCount == 0)
                    {
                        this.position += node.Width;
                    }

                    this.stack.Pop();
                }
            }

            return false;
        }

        /// <summary>
        /// The current diagnostic that the enumerator is pointing at.
        /// </summary>
        public Diagnostic Current
        {
            get { return this.current; }
        }

        private struct NodeIteration
        {
            internal GreenNode Node;
            internal int DiagnosticIndex;
            internal int SlotIndex;

            internal NodeIteration(GreenNode node)
            {
                this.Node = node;
                this.SlotIndex = -1;
                this.DiagnosticIndex = -1;
            }
        }

        private struct NodeIterationStack
        {
            private NodeIteration[] stack;
            private int count;

            internal NodeIterationStack(int capacity)
            {
                Debug.Assert(capacity > 0);
                this.stack = new NodeIteration[capacity];
                this.count = 0;
            }

            internal void PushNodeOrToken(GreenNode node)
            {
                var token = node as Syntax.InternalSyntax.SyntaxToken;
                if (token != null)
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
                var trailing = token.GetTrailingTrivia();
                if (trailing != null)
                {
                    this.Push(trailing);
                }

                this.Push(token);
                var leading = token.GetLeadingTrivia();
                if (leading != null)
                {
                    this.Push(leading);
                }
            }

            private void Push(GreenNode node)
            {
                if (this.count >= this.stack.Length)
                {
                    var tmp = new NodeIteration[this.stack.Length * 2];
                    Array.Copy(this.stack, tmp, this.stack.Length);
                    this.stack = tmp;
                }

                this.stack[this.count] = new NodeIteration(node);
                this.count++;
            }

            internal void Pop()
            {
                this.count--;
            }

            internal bool Any()
            {
                return this.count > 0;
            }

            internal NodeIteration Top
            {
                get
                {
                    return this[this.count - 1];
                }
            }

            internal NodeIteration this[int index]
            {
                get
                {
                    Debug.Assert(this.stack != null);
                    Debug.Assert(index >= 0 && index < this.count);
                    return this.stack[index];
                }
            }

            internal void UpdateSlotIndexForStackTop(int slotIndex)
            {
                Debug.Assert(this.stack != null);
                Debug.Assert(this.count > 0);
                this.stack[this.count - 1].SlotIndex = slotIndex;
            }

            internal void UpdateDiagnosticIndexForStackTop(int diagnosticIndex)
            {
                Debug.Assert(this.stack != null);
                Debug.Assert(this.count > 0);
                this.stack[this.count - 1].DiagnosticIndex = diagnosticIndex;
            }
        }
    }
}
