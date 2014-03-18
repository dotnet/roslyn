// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    public partial class Project
    {
        private class EquivalenceResult
        {
            public readonly bool PublicallyEquivalent;
            public readonly bool PrivatelyEquivalent;

            public EquivalenceResult(bool publicallyEquivalent, bool privatelyEquivalent)
            {
                this.PublicallyEquivalent = publicallyEquivalent;
                this.PrivatelyEquivalent = privatelyEquivalent;
            }
        }
    }
}