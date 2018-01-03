// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal abstract class RegexNode
    {
        public readonly RegexKind Kind;

        protected RegexNode(RegexKind kind)
        {
            Debug.Assert(kind != RegexKind.None);
            Kind = kind;
        }

        public abstract int ChildCount { get; }
        public abstract RegexNodeOrToken ChildAt(int index);

        public abstract void Accept(IRegexNodeVisitor visitor);

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private readonly RegexNode _regexNode;
            private readonly int _childCount;
            private int _currentIndex;

            public Enumerator(RegexNode regexNode)
            {
                _regexNode = regexNode;
                _childCount = regexNode.ChildCount;
                _currentIndex = -1;
                Current = default;
            }

            public RegexNodeOrToken Current { get; private set; }

            public bool MoveNext()
            {
                _currentIndex++;
                if (_currentIndex >= _childCount)
                {
                    Current = default;
                    return false;
                }

                Current = _regexNode.ChildAt(_currentIndex);
                return true;
            }
        }
    }
}
