// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal class SyntaxNodeLocationComparer : IComparer<SyntaxNode>
    {
        private readonly Compilation _compilation;

        public SyntaxNodeLocationComparer(Compilation compilation)
        {
            _compilation = compilation;
        }

        public int Compare(SyntaxNode x, SyntaxNode y)
        {
            return _compilation.CompareSourceLocations(x.GetLocation(), y.GetLocation());
        }
    }
}
