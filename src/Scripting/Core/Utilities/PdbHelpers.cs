// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal static class PdbHelpers
    {
        public static DebugInformationFormat GetPlatformSpecificDebugInformationFormat()
        {
#if NET
            // Use PortablePdb for .NET
            return DebugInformationFormat.PortablePdb;
#else
            // Use PortablePdb for Mono
            if (Type.GetType("Mono.Runtime") != null)
            {
                return DebugInformationFormat.PortablePdb;
            }

            // otherwise standard PDB
            return DebugInformationFormat.Pdb;
#endif
        }
    }
}
