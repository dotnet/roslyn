// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    internal class SyntaxNodeLocationComparer : IComparer<SyntaxNode>
    {
        private readonly Compilation _compilation;

        public SyntaxNodeLocationComparer(Compilation compilation)
        {
            _compilation = compilation;
        }
        public int Compare(SyntaxNode? x, SyntaxNode? y)
        {
            if (x is null)
            {
                if (y is null)
                {
                    return 0;
                }

                return -1;
            }
            else if (y is null)
            {
                return 1;
            }
            else
            {
                return _compilation.CompareSourceLocations(x, y);
            }
        }
    }
}
