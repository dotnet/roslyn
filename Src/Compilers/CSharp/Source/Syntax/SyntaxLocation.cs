// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    internal class SyntaxLocation : SourceLocation
    {
        private readonly SyntaxTree tree;
        private readonly int position;
        private readonly int width;

        public SyntaxLocation(SyntaxTree tree, int position, int width)
        {
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException("width");
            }

            this.tree = tree;
            this.position = position;
            this.width = width;
        }

        public override SyntaxTree SyntaxTree
        {
            get { return this.tree; }
        }

        public override TextSpan Span
        {
            get { return new TextSpan(this.position, this.width); }
        }
    }
}