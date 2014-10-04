// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias PDB;

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Emit;
using PDB::Roslyn.Utilities.Pdb;

namespace Roslyn.Test.Utilities
{
    static class PdbTestUtilities
    {
        public static EditAndContinueMethodDebugInformation GetEncMethodDebugInfo(this ISymUnmanagedReader symReader, MethodHandle handle)
        {
            var cdi = symReader.GetCustomDebugInfo(MetadataTokens.GetToken(handle));
            return GetEncMethodDebugInfo(cdi);
        }

        public static EditAndContinueMethodDebugInformation GetEncMethodDebugInfo(byte[] customDebugInfoBlob)
        {
            var record = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(customDebugInfoBlob, CustomDebugInfoKind.EditAndContinueLocalSlotMap);
            return EditAndContinueMethodDebugInformation.Create(record);
        }
    }
}
