using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x23
    internal struct AssemblyRefRow
    {
        internal readonly ushort MajorVersion;
        internal readonly ushort MinorVersion;
        internal readonly ushort BuildNumber;
        internal readonly ushort RevisionNumber;
        internal readonly AssemblyFlags Flags;
        internal readonly uint PublicKeyOrToken;
        internal readonly uint Name;
        internal readonly uint Culture;
        internal readonly uint HashValue;
        internal AssemblyRefRow(
          ushort majorVersion,
          ushort minorVersion,
          ushort buildNumber,
          ushort revisionNumber,
          AssemblyFlags flags,
          uint publicKeyOrToken,
          uint name,
          uint culture,
          uint hashValue)
        {
            this.MajorVersion = majorVersion;
            this.MinorVersion = minorVersion;
            this.BuildNumber = buildNumber;
            this.RevisionNumber = revisionNumber;
            this.Flags = flags;
            this.PublicKeyOrToken = publicKeyOrToken;
            this.Name = name;
            this.Culture = culture;
            this.HashValue = hashValue;
        }
    }
}