// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => string.Format("({0}, {1})", Label, Value);
    }
}
