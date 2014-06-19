using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Compilers.Internal.MetadataReader.PEFileFlags;
using Roslyn.Compilers.Internal.MetadataReader.UtilityDataStructures;

namespace Roslyn.Compilers.Internal.MetadataReader.PEFile
{
    // 0x0E
    internal struct DeclSecurityRow
    {
        internal readonly DeclSecurityActionFlags ActionFlags;
        internal readonly uint Parent;
        internal readonly uint PermissionSet;
        internal DeclSecurityRow(
          DeclSecurityActionFlags actionFlags,
          uint parent,
          uint permissionSet)
        {
            this.ActionFlags = actionFlags;
            this.Parent = parent;
            this.PermissionSet = permissionSet;
        }
    }
}