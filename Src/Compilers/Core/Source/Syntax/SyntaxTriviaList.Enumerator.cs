// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    public partial struct SyntaxTriviaList
    {
        public struct Enumerator
        {
            private SyntaxToken token;
            private GreenNode singleNodeOrList;
            private int baseIndex;
            private int count;

            private int index;
            private GreenNode current;
            private int position;

            internal Enumerator(ref SyntaxTriviaList list)
            {
                this.token = list.token;
                this.singleNodeOrList = list.node;
                this.baseIndex = list.index;
                this.count = list.Count;

                this.index = -1;
                this.current = null;
                this.position = list.position;
            }

            // PERF: Passing SyntaxToken by ref since it's a non-trivial struct
            private void InitializeFrom(ref SyntaxToken token, GreenNode greenNode, int index, int position)
            {
                this.token = token;
                this.singleNodeOrList = greenNode;
                this.baseIndex = index;
                this.count = greenNode.IsList ? greenNode.SlotCount : 1;

                this.index = -1;
                this.current = null;
                this.position = position;
            }

            // PERF: Used to initialize an enumerator for leading trivia directly from a token.
            // This saves constructing an intermediate SyntaxTriviaList. Also, passing token
            // by ref since it's a non-trivial struct
            internal void InitializeFromLeadingTrivia(ref SyntaxToken token)
            {
                InitializeFrom(ref token, token.Node.GetLeadingTriviaCore(), 0, token.Position);
            }

            // PERF: Used to initialize an enumerator for trailing trivia directly from a token.
            // This saves constructing an intermediate SyntaxTriviaList. Also, passing token
            // by ref since it's a non-trivial struct
            internal void InitializeFromTrailingTrivia(ref SyntaxToken token)
            {
                var leading = token.Node.GetLeadingTriviaCore();
                int index = 0;
                if (leading != null)
                {
                    index = leading.IsList ? leading.SlotCount : 1;
                }

                var trailingGreen = token.Node.GetTrailingTriviaCore();
                int trailingPosition = token.Position + token.FullWidth;
                if (trailingGreen != null)
                {
                    trailingPosition -= trailingGreen.FullWidth;
                }

                InitializeFrom(ref token, trailingGreen, index, trailingPosition);
            }

            public bool MoveNext()
            {
                int newIndex = this.index + 1;
                if (newIndex >= this.count)
                {
                    // invalidate iterator
                    this.current = null;
                    return false;
                }

                this.index = newIndex;

                if (current != null)
                {
                    this.position += this.current.FullWidth;
                }

                this.current = GetGreenNodeAt(this.singleNodeOrList, newIndex);
                return true;
            }

            public SyntaxTrivia Current
            {
                get
                {
                    if (this.current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return new SyntaxTrivia(this.token, this.current, this.position, this.baseIndex + this.index);
                }
            }

            internal bool TryMoveNextAndGetCurrent(ref SyntaxTrivia current)
            {
                if (!MoveNext())
                {
                    return false;
                }

                current = new SyntaxTrivia(this.token, this.current, this.position, this.baseIndex + this.index);
                return true;
            }
        }

        private class EnumeratorImpl : IEnumerator<SyntaxTrivia>
        {
            private Enumerator enumerator;

            // SyntaxTriviaList is a relatively big struct so is passed as ref
            internal EnumeratorImpl(ref SyntaxTriviaList list)
            {
                this.enumerator = new Enumerator(ref list);
            }

            public SyntaxTrivia Current
            {
                get { return enumerator.Current; }
            }

            object IEnumerator.Current
            {
                get { return enumerator.Current; }
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }
    }
}