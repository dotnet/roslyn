// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.DiaSymReader;
using Roslyn.Test.PdbUtilities;

namespace Roslyn.Test.Utilities
{
    internal static class PdbTestUtilities
    {
        public static EditAndContinueMethodDebugInformation GetEncMethodDebugInfo(this ISymUnmanagedReader symReader, MethodDefinitionHandle handle)
        {
            var cdi = CustomDebugInfoUtilities.GetCustomDebugInfoBytes(symReader, handle, methodVersion: 1);
            if (cdi == null)
            {
                return default(EditAndContinueMethodDebugInformation);
            }

            return GetEncMethodDebugInfo(cdi);
        }

        public static EditAndContinueMethodDebugInformation GetEncMethodDebugInfo(byte[] customDebugInfoBlob)
        {
            return EditAndContinueMethodDebugInformation.Create(
                CustomDebugInfoUtilities.GetEditAndContinueLocalSlotMapRecord(customDebugInfoBlob),
                CustomDebugInfoUtilities.GetEditAndContinueLambdaMapRecord(customDebugInfoBlob));
        }

        public static string GetTokenToLocationMap(Compilation compilation, bool maskToken = false)
        {
            using (var exebits = new MemoryStream())
            {
                using (var pdbbits = new MemoryStream())
                {
                    compilation.Emit(exebits, pdbbits);
                    return Token2SourceLineExporter.TokenToSourceMap2Xml(pdbbits, maskToken);
                }
            }
        }
    }
}
