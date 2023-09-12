// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundCollectionExpressionBase
    {
        internal int? GetKnownLength(out bool hasSpreadElements)
        {
            hasSpreadElements = false;
            foreach (var element in Elements)
            {
                if (element is BoundCollectionExpressionSpreadElement spreadElement)
                {
                    hasSpreadElements = true;
                    if (spreadElement.LengthOrCount is null)
        {
                        return null;
                    }
                }
            }
            return Elements.Length;
        }
    }
}
