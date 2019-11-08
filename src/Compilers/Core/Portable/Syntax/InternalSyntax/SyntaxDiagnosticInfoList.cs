// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    // Avoid implementing IEnumerable so we do not get any unintentional boxing.
    internal struct SyntaxDiagnosticInfoList
    {
        private readonly GreenNode _node;

        internal SyntaxDiagnosticInfoList(GreenNode node)
        {
            _node = node;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_node);
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
                internal readonly GreenNode Node;
                internal int DiagnosticIndex;
                internal int SlotIndex;

                internal NodeIteration(GreenNode node)
                {
                    this.Node = node;
                    this.SlotIndex = -1;
                    this.DiagnosticIndex = -1;
                }
            }

            private NodeIteration[] _stack;
            private int _count;

            public DiagnosticInfo Current { get; private set; }

            internal Enumerator(GreenNode node)
            {
                Current = null;
                _stack = null;
                _count = 0;
                if (node is { ContainsDiagnostics: true })
                {
                    _stack = new NodeIteration[8];
                    this.PushNodeOrToken(node);
                }
            }

            public bool MoveNext()
            {
                while (_count > 0)
                {
                    var diagIndex = _stack[_count - 1].DiagnosticIndex;
                    var node = _stack[_count - 1].Node;
                    var diags = node.GetDiagnostics();
                    if (diagIndex < diags.Length - 1)
                    {
                        diagIndex++;
                        Current = diags[diagIndex];
                        _stack[_count - 1].DiagnosticIndex = diagIndex;
                        return true;
                    }

                    var slotIndex = _stack[_count - 1].SlotIndex;
tryAgain:
                    if (slotIndex < node.SlotCount - 1)
                    {
                        slotIndex++;
                        var child = node.GetSlot(slotIndex);
                        if (child == null || !child.ContainsDiagnostics)
                        {
                            goto tryAgain;
                        }

                        _stack[_count - 1].SlotIndex = slotIndex;
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
                if (node.IsToken)
                {
                    this.PushToken(node);
                }
                else
                {
                    this.Push(node);
                }
            }

            private void PushToken(GreenNode token)
            {
                var trailing = token.GetTrailingTriviaCore();
                if (trailing != null)
                {
                    this.Push(trailing);
                }

                this.Push(token);
                var leading = token.GetLeadingTriviaCore();
                if (leading != null)
                {
                    this.Push(leading);
                }
            }

            private void Push(GreenNode node)
            {
                if (_count >= _stack.Length)
                {
                    var tmp = new NodeIteration[_stack.Length * 2];
                    Array.Copy(_stack, tmp, _stack.Length);
                    _stack = tmp;
                }

                _stack[_count] = new NodeIteration(node);
                _count++;
            }

            private void Pop()
            {
                _count--;
            }
        }
    }
}
