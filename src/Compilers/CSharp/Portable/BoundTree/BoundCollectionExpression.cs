// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundCollectionExpressionBase
    {
        internal bool HasSpreadElements(out int numberIncludingLastSpread, out bool hasKnownLength)
        {
            hasKnownLength = true;
            numberIncludingLastSpread = 0;
            for (int i = 0; i < Elements.Length; i++)
            {
                if (Elements[i] is BoundCollectionExpressionSpreadElement spreadElement)
                {
                    numberIncludingLastSpread = i + 1;
                    if (spreadElement.LengthOrCount is null)
                    {
                        hasKnownLength = false;
                    }
                }
            }
            return numberIncludingLastSpread > 0;
        }
    }
}
