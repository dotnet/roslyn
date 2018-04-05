// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal partial struct ChildSyntaxList
    {
        private readonly GreenNode _node;
        private int _count;

        internal ChildSyntaxList(GreenNode node)
        {
            _node = node;
            _count = -1;
        }

        public int Count
        {
            get
            {
                if (_count == -1)
                {
                    _count = this.CountNodes();
                }

                return _count;
            }
        }

        private int CountNodes()
        {
            int n = 0;
            var enumerator = this.GetEnumerator();
            while (enumerator.MoveNext())
            {
                n++;
            }

            return n;
        }

        // for debugging
        private GreenNode[] Nodes
        {
            get
            {
                var result = new GreenNode[this.Count];
                var i = 0;

                foreach (var n in this)
                {
                    result[i++] = n;
                }

                return result;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_node);
        }

        public Reversed Reverse()
        {
            return new Reversed(_node);
        }
    }
}
