// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
