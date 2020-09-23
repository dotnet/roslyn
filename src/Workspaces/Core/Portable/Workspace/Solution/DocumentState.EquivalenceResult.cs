// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState
    {
        internal class EquivalenceResult
        {
            public readonly bool TopLevelEquivalent;
            public readonly bool InteriorEquivalent;

            public EquivalenceResult(bool topLevelEquivalent, bool interiorEquivalent)
            {
                this.TopLevelEquivalent = topLevelEquivalent;
                this.InteriorEquivalent = interiorEquivalent;
            }
        }
    }
}
