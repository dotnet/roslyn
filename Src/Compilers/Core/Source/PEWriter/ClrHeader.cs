// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Cci = Microsoft.Cci;

namespace Microsoft.Cci
{
    internal class ClrHeader
    {
        internal ushort MajorRuntimeVersion;
        internal ushort MinorRuntimeVersion;
        internal DirectoryEntry MetaData;
        internal uint Flags;
        internal uint EntryPointToken;
        internal DirectoryEntry Resources;
        internal DirectoryEntry StrongNameSignature;
        internal DirectoryEntry CodeManagerTable;
        internal DirectoryEntry VTableFixups;
        internal DirectoryEntry ExportAddressTableJumps;
    }
}