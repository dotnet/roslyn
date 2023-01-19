// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias DSR;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using DSR::Microsoft.DiaSymReader;
using Roslyn.Test.PdbUtilities;

namespace Roslyn.Test.Utilities
{
    public static class PdbTestUtilities
    {
        public static ISymUnmanagedReader3 CreateSymReader(this CompilationVerifier verifier)
        {
            var pdbStream = new ImmutableMemoryStream(verifier.EmittedAssemblyPdb);
            return SymReaderFactory.CreateReader(pdbStream, metadataReaderOpt: null, metadataMemoryOwnerOpt: null);
        }

        public static unsafe EditAndContinueMethodDebugInformation GetEncMethodDebugInfo(this ISymUnmanagedReader3 symReader, MethodDefinitionHandle handle)
        {
            const int S_OK = 0;

            if (symReader is ISymUnmanagedReader4 symReader4)
            {
                int hr = symReader4.GetPortableDebugMetadata(out byte* metadata, out int size);
                Marshal.ThrowExceptionForHR(hr);

                if (hr == S_OK)
                {
                    var pdbReader = new MetadataReader(metadata, size);

                    ImmutableArray<byte> GetCdiBytes(Guid kind)
                        => TryGetCustomDebugInformation(pdbReader, handle, kind, out var info) ? pdbReader.GetBlobContent(info.Value) : default(ImmutableArray<byte>);

                    return EditAndContinueMethodDebugInformation.Create(
                        compressedSlotMap: GetCdiBytes(PortableCustomDebugInfoKinds.EncLocalSlotMap),
                        compressedLambdaMap: GetCdiBytes(PortableCustomDebugInfoKinds.EncLambdaAndClosureMap),
                        compressedStateMachineStateMap: GetCdiBytes(PortableCustomDebugInfoKinds.EncStateMachineStateMap));
                }
            }

            var cdi = CustomDebugInfoUtilities.GetCustomDebugInfoBytes(symReader, handle, methodVersion: 1);
            if (cdi == null)
            {
                return EditAndContinueMethodDebugInformation.Create(default(ImmutableArray<byte>), default(ImmutableArray<byte>));
            }

            return GetEncMethodDebugInfo(cdi);
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static bool TryGetCustomDebugInformation(MetadataReader reader, EntityHandle handle, Guid kind, out CustomDebugInformation customDebugInfo)
        {
            bool foundAny = false;
            customDebugInfo = default(CustomDebugInformation);
            foreach (var infoHandle in reader.GetCustomDebugInformation(handle))
            {
                var info = reader.GetCustomDebugInformation(infoHandle);
                var id = reader.GetGuid(info.Kind);
                if (id == kind)
                {
                    if (foundAny)
                    {
                        throw new BadImageFormatException();
                    }
                    customDebugInfo = info;
                    foundAny = true;
                }
            }
            return foundAny;
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
