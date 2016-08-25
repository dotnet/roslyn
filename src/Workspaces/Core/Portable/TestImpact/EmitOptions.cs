// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.TestImpact.BuildManagement
{
    internal struct EmitOptions
    {
        public int FileAlignment;
        public ulong BaseAddress;
        public bool HighEntropyVirtualAddressSpace;
        public string SubsystemVersion;
        public string Instrument;
    }
}
