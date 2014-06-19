// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    //This used to implement IEnumerable and have more functionality, but it was not tested or used.
    internal struct SyntaxDiagnosticInfoList
    {
        private readonly GreenNode node;

        internal SyntaxDiagnosticInfoList(GreenNode node)
        {
            this.node = node;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(node);
        }

        internal bool Any(Func<DiagnosticInfo, bool> predicate)
        {
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current))
                    return true;
            }

            return false;
        }

        public struct Enumerator
        {
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

            private NodeIteration[] stack;
            private int count;
            private DiagnosticInfo current;

            internal Enumerator(GreenNode node)
            {
                this.current = null;
                this.stack = null;
                this.count = 0;
                if (node != null && node.ContainsDiagnostics)
                {
                    this.stack = new NodeIteration[8];
                    this.PushNodeOrToken(node);
                }
            }

            public bool MoveNext()
            {
                while (count > 0)
                {
                    var diagIndex = this.stack[count - 1].DiagnosticIndex;
                    var node = this.stack[count - 1].Node;
                    var diags = node.GetDiagnostics();
                    if (diagIndex < diags.Length - 1)
                    {
                        diagIndex++;
                        current = diags[diagIndex];
                        stack[count - 1].DiagnosticIndex = diagIndex;
                        return true;
                    }

                    var slotIndex = stack[count - 1].SlotIndex;
                tryAgain:
                    if (slotIndex < node.SlotCount - 1)
                    {
                        slotIndex++;
                        var child = node.GetSlot(slotIndex);
                        if (child == null || !child.ContainsDiagnostics)
                        {
                            goto tryAgain;
                        }

                        stack[count - 1].SlotIndex = slotIndex;
                        this.PushNodeOrToken(child);
                    }
                    else
                    {
                        this.Pop();
                    }
                }

                return false;
            }

            private void PushNodeOrToken(GreenNode node)
            {
                var token = node as SyntaxToken;
                if (token != null)
                {
                    this.PushToken(token);
                }
                else
                {
                    this.Push(node);
                }
            }

            private void PushToken(SyntaxToken token)
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
                if (count >= this.stack.Length)
                {
                    var tmp = new NodeIteration[this.stack.Length * 2];
                    Array.Copy(this.stack, tmp, this.stack.Length);
                    this.stack = tmp;
                }

                this.stack[count] = new NodeIteration(node);
                count++;
            }

            private void Pop()
            {
                count--;
            }

            public DiagnosticInfo Current
            {
                get { return this.current; }
            }
        }
    }
}
