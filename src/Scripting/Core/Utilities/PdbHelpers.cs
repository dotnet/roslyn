// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal static class PdbHelpers
    {
        public static DebugInformationFormat GetPlatformSpecificDebugInformationFormat()
        {
            // for CoreCLR & Mono, use PortablePdb
            if (CoreClrShim.AssemblyLoadContext.Type != null || Type.GetType("Mono.Runtime") != null)
            {
                return DebugInformationFormat.PortablePdb;
            }

            // otherwise standard PDB
            return DebugInformationFormat.Pdb;
        }
    }
}
