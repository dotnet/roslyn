// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial struct ChildSyntaxList
    {
        private readonly GreenNode node;
        private int count;

        internal ChildSyntaxList(GreenNode node)
        {
            this.node = node;
            this.count = -1;
        }

        public int Count
        {
            get
            {
                if (this.count == -1)
                {
                    this.count = this.CountNodes();
                }

                return this.count;
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
            return new Enumerator(node);
        }

        public Reversed Reverse()
        {
            return new Reversed(this.node);
        }
    }
}
