// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.PortableExecutable;

namespace Microsoft.Cci
{
    internal sealed class CorHeader
    {
        internal ushort MajorRuntimeVersion;
        internal ushort MinorRuntimeVersion;
        internal DirectoryEntry MetadataDirectory;
        internal CorFlags Flags;
        internal uint EntryPointToken;
        internal DirectoryEntry Resources;
        internal DirectoryEntry StrongNameSignature;
        internal DirectoryEntry CodeManagerTable;
        internal DirectoryEntry VTableFixups;
        internal DirectoryEntry ExportAddressTableJumps;
    }
}