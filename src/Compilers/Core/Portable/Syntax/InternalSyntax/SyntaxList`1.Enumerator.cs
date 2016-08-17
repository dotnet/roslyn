// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal partial struct SyntaxList<TNode> where TNode : GreenNode
    {
        internal struct Enumerator
        {
            private SyntaxList<TNode> _list;
            private int _index;

            internal Enumerator(SyntaxList<TNode> list)
            {
                _list = list;
                _index = -1;
            }

            public bool MoveNext()
            {
                var newIndex = _index + 1;
                if (newIndex < _list.Count)
                {
                    _index = newIndex;
                    return true;
                }

                return false;
            }

            public TNode Current
            {
                get
                {
                    return _list[_index];
                }
            }
        }
    }
}