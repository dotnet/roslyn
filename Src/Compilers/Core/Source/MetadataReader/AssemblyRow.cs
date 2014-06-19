using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x20
    internal struct AssemblyRow
    {
        internal readonly uint HashAlgId;
        internal readonly ushort MajorVersion;
        internal readonly ushort MinorVersion;
        internal readonly ushort BuildNumber;
        internal readonly ushort RevisionNumber;
        internal readonly AssemblyFlags Flags;
        internal readonly uint PublicKey;
        internal readonly uint Name;
        internal readonly uint Culture;
        internal AssemblyRow(
          uint hashAlgId,
          ushort majorVersion,
          ushort minorVersion,
          ushort buildNumber,
          ushort revisionNumber,
          AssemblyFlags flags,
          uint publicKey,
          uint name,
          uint culture)
        {
            this.HashAlgId = hashAlgId;
            this.MajorVersion = majorVersion;
            this.MinorVersion = minorVersion;
            this.BuildNumber = buildNumber;
            this.RevisionNumber = revisionNumber;
            this.Flags = flags;
            this.PublicKey = publicKey;
            this.Name = name;
            this.Culture = culture;
        }
    }
}