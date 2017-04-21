// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


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
