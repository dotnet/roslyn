// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias DSR;

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Debugging;
using DSR::Microsoft.DiaSymReader;

namespace Roslyn.Test.PdbUtilities
{
    public static class CustomDebugInfoUtilities
    {
        public static byte[] GetCustomDebugInfoBytes(ISymUnmanagedReader3 reader, MethodDefinitionHandle handle, int methodVersion)
        {
            return reader.GetCustomDebugInfo(MetadataTokens.GetToken(handle), methodVersion);
        }

        public static ImmutableArray<byte> GetEditAndContinueLocalSlotMapRecord(byte[] customDebugInfoBlob)
        {
            return CustomDebugInfoReader.TryGetCustomDebugInfoRecord(customDebugInfoBlob, CustomDebugInfoKind.EditAndContinueLocalSlotMap);
        }

        public static ImmutableArray<byte> GetEditAndContinueLambdaMapRecord(byte[] customDebugInfoBlob)
        {
            return CustomDebugInfoReader.TryGetCustomDebugInfoRecord(customDebugInfoBlob, CustomDebugInfoKind.EditAndContinueLambdaMap);
        }
    }
}
