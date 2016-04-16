// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.PortableExecutable;

namespace Microsoft.Cci
{
    // TODO: merge with System.Reflection.PortableExecutable.CorHeader
    internal sealed class CorHeader
    {
        public ushort MajorRuntimeVersion { get; }
        public ushort MinorRuntimeVersion { get; }
        public DirectoryEntry MetadataDirectory { get; }
        public CorFlags Flags { get; }
        public int EntryPointTokenOrRelativeVirtualAddress { get; }
        public DirectoryEntry ResourcesDirectory { get; }
        public DirectoryEntry StrongNameSignatureDirectory { get; }
        public DirectoryEntry CodeManagerTableDirectory { get; }
        public DirectoryEntry VtableFixupsDirectory { get; }
        public DirectoryEntry ExportAddressTableJumpsDirectory { get; }
        public DirectoryEntry ManagedNativeHeaderDirectory { get; }

        public CorHeader(
            CorFlags flags,
            DirectoryEntry metadataDirectory,
            int entryPointTokenOrRelativeVirtualAddress = 0,
            ushort majorRuntimeVersion = 2,
            ushort minorRuntimeVersion = 5,
            DirectoryEntry resourcesDirectory = default(DirectoryEntry),
            DirectoryEntry strongNameSignatureDirectory = default(DirectoryEntry),
            DirectoryEntry codeManagerTableDirectory = default(DirectoryEntry),
            DirectoryEntry vtableFixupsDirectory = default(DirectoryEntry),
            DirectoryEntry exportAddressTableJumpsDirectory = default(DirectoryEntry),
            DirectoryEntry managedNativeHeaderDirectory = default(DirectoryEntry))
        {
            MajorRuntimeVersion = majorRuntimeVersion;
            MinorRuntimeVersion = minorRuntimeVersion;
            MetadataDirectory = metadataDirectory;
            Flags = flags;
            EntryPointTokenOrRelativeVirtualAddress = entryPointTokenOrRelativeVirtualAddress;
            ResourcesDirectory = resourcesDirectory;
            StrongNameSignatureDirectory = strongNameSignatureDirectory;
            CodeManagerTableDirectory = codeManagerTableDirectory;
            VtableFixupsDirectory = vtableFixupsDirectory;
            ExportAddressTableJumpsDirectory = exportAddressTableJumpsDirectory;
            ManagedNativeHeaderDirectory = managedNativeHeaderDirectory;
        }
    }
}
