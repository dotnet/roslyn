// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.PortableExecutable;

namespace Microsoft.Cci
{
    // TODO: merge with System.Reflection.PortableExecutable.CorHeader
    internal sealed class CorHeader
    {
        public ushort MajorRuntimeVersion { get; private set; }
        public ushort MinorRuntimeVersion { get; private set; }
        public DirectoryEntry MetadataDirectory { get; private set; }
        public CorFlags Flags { get; private set; }
        public int EntryPointTokenOrRelativeVirtualAddress { get; private set; }
        public DirectoryEntry ResourcesDirectory { get; private set; }
        public DirectoryEntry StrongNameSignatureDirectory { get; private set; }
        public DirectoryEntry CodeManagerTableDirectory { get; private set; }
        public DirectoryEntry VtableFixupsDirectory { get; private set; }
        public DirectoryEntry ExportAddressTableJumpsDirectory { get; private set; }
        public DirectoryEntry ManagedNativeHeaderDirectory { get; private set; }

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
