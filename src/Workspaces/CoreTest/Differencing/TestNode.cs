// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Differencing.UnitTests
{
    public class TestNode
    {
        public const int MaxValue = 10;
        public const int MaxLabel = 1;

        public readonly int Label;
        public readonly int Value;
        public readonly TestNode[] Children;
        public TestNode Parent;

        private TestNode _lazyRoot;

        public TestNode(int label, int value, params TestNode[] children)
        {
            Debug.Assert(value >= 0 && value <= MaxValue);
            Debug.Assert(label >= 0 && label <= MaxLabel);

            this.Label = label;
            this.Value = value;
            this.Children = children;

            foreach (var child in children)
            {
                child.Parent = this;
            }
        }

        public TestNode Root
        {
            get
            {
                if (_lazyRoot == null)
                {
                    _lazyRoot = this.Parent == null ? this : this.Parent.Root;
                }

                return _lazyRoot;
            }
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", Label, Value);
        }
    }
}
