// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ValueTracking
{
    internal class ValueTrackedItem
    {
        public Location Location { get; }
        public ISymbol Symbol { get; }

        public ValueTrackedItem(
            Location location,
            ISymbol symbol)
        {
            Location = location;
            Symbol = symbol;
        }
    }
}
