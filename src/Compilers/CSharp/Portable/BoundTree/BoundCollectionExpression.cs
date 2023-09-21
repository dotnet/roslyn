// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundCollectionExpressionBase
    {
        internal bool HasSpreadElements()
        {
            GetKnownLength(out bool hasSpreadElements);
            return hasSpreadElements;
        }

        // PROTOTYPE: Consider removing this method (and just keeping HasSpreadElements() above).
        // The value returned is just node.Elements.Length when HasSpreadElements() returns true,
        // and when HasSpreadElements() returns false, the value is just confusing. Make all callers explicit.
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
