using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial struct DiagnosticList
    {
        partial struct Enumerator
        {
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

                internal void PushNodeOrToken(Syntax.InternalSyntax.SyntaxNode node)
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

                private void Push(Syntax.InternalSyntax.SyntaxNode node)
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
}