// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
