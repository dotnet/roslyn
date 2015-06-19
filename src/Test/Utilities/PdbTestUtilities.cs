// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias PDB;


using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.DiaSymReader;
using PDB::Microsoft.DiaSymReader;
using PDB::Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    internal static class PdbTestUtilities
    {
        public static EditAndContinueMethodDebugInformation GetEncMethodDebugInfo(this ISymUnmanagedReader symReader, MethodDefinitionHandle handle)
        {
            var cdi = symReader.GetCustomDebugInfoBytes(MetadataTokens.GetToken(handle), methodVersion: 1);
            if (cdi == null)
            {
                return default(EditAndContinueMethodDebugInformation);
            }

            return GetEncMethodDebugInfo(cdi);
        }

        public static EditAndContinueMethodDebugInformation GetEncMethodDebugInfo(byte[] customDebugInfoBlob)
        {
            var localSlots = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(customDebugInfoBlob, CustomDebugInfoKind.EditAndContinueLocalSlotMap);
            var lambdaMap = CustomDebugInfoReader.TryGetCustomDebugInfoRecord(customDebugInfoBlob, CustomDebugInfoKind.EditAndContinueLambdaMap);
            return EditAndContinueMethodDebugInformation.Create(localSlots, lambdaMap);
        }
    }
}
