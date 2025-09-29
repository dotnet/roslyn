// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed partial class CompilationVerifier
    {
        internal readonly struct EmitOutput
        {
            internal ImmutableArray<byte> Assembly { get; }
            internal ImmutableArray<byte> Pdb { get; }
            internal EmitOutput(ImmutableArray<byte> assembly, ImmutableArray<byte> pdb)
            {
                if (pdb.IsDefault)
                {
                    // We didn't emit a discrete PDB file, so we'll look for an embedded PDB instead.
                    using (var peReader = new PEReader(assembly))
                    {
                        DebugDirectoryEntry portablePdbEntry = peReader.ReadDebugDirectory().FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                        if (portablePdbEntry.DataSize != 0)
                        {
                            using (var embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(portablePdbEntry))
                            {
                                var mdReader = embeddedMetadataProvider.GetMetadataReader();
                                pdb = readMetadata(mdReader);
                            }
                        }
                    }
                }

                Assembly = assembly;
                Pdb = pdb;

                unsafe ImmutableArray<byte> readMetadata(MetadataReader mdReader)
                {
                    var length = mdReader.MetadataLength;
                    var bytes = new byte[length];
                    Marshal.Copy((IntPtr)mdReader.MetadataPointer, bytes, 0, length);
                    return ImmutableArray.Create(bytes);
                }
            }
        }
    }
}
