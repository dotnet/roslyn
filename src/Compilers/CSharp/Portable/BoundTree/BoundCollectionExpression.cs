// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundCollectionExpression
    {
        internal int? GetKnownLength()
        {
            return Elements.Any(e => e is BoundCollectionExpressionSpreadElement) ?
                null :
                Elements.Length;
        }
    }
}
