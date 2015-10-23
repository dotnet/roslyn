// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis
{
    public partial class Project
    {
        private class EquivalenceResult
        {
            public readonly bool PubliclyEquivalent;
            public readonly bool PrivatelyEquivalent;

            public EquivalenceResult(bool publiclyEquivalent, bool privatelyEquivalent)
            {
                this.PubliclyEquivalent = publiclyEquivalent;
                this.PrivatelyEquivalent = privatelyEquivalent;
            }
        }
    }
}
