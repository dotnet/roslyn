// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
