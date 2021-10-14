// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    [Export(typeof(IPdbFileLocatorService)), Shared]
    internal sealed class PdbFileLocatorService : IPdbFileLocatorService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PdbFileLocatorService()
        {
        }

        public Task<MultiMetadataReaderProvider?> GetMetadataReadersAsync(string dllPath, CancellationToken cancellationToken)
        {
            var dllStream = IOUtilities.PerformIO(() => File.OpenRead(dllPath));
            if (dllStream is null)
                return Task.FromResult<MultiMetadataReaderProvider?>(null);

            var peReader = new PEReader(dllStream);

            // The simplest possible thing is that the PDB happens to be right next to the DLL. You never know, we might get lucky.
            var pdbPath = Path.ChangeExtension(dllPath, ".pdb");
            if (File.Exists(pdbPath))
            {
                var pdbStream = IOUtilities.PerformIO(() => File.OpenRead(pdbPath));

                if (pdbStream is null || !IsPortable(pdbStream))
                {
                    // TODO: Support non portable PDBs: https://github.com/dotnet/roslyn/issues/55834
                    dllStream.Dispose();
                    pdbStream?.Dispose();
                    peReader.Dispose();
                    return Task.FromResult<MultiMetadataReaderProvider?>(null);
                }

                var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);

                var result = new MultiMetadataReaderProvider(dllStream, peReader, pdbStream, pdbReaderProvider);
                return Task.FromResult<MultiMetadataReaderProvider?>(result);
            }

            // Otherwise lets see if its an embedded PDB
            var entry = peReader.ReadDebugDirectory().SingleOrDefault(x => x.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            if (entry.Type != DebugDirectoryEntryType.Unknown)
            {
                var pdbReaderProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);

                var result = new MultiMetadataReaderProvider(dllStream, peReader, pdbStream: null, pdbReaderProvider);
                return Task.FromResult<MultiMetadataReaderProvider?>(result);
            }

            // TODO: Call the debugger to get this

            // Debugger needs:
            // - PDB MVID
            // - PDB Age
            // - PDB TimeStamp
            // - PDB Path
            // - DLL Path
            // 
            // Most of this info comes from the CodeView Debug Directory from the dll

            dllStream.Dispose();
            peReader.Dispose();
            return Task.FromResult<MultiMetadataReaderProvider?>(null);
        }

        private static bool IsPortable(Stream pdbStream)
        {
            var isPortable = pdbStream.ReadByte() == 'B' && pdbStream.ReadByte() == 'S' && pdbStream.ReadByte() == 'J' && pdbStream.ReadByte() == 'B';
            pdbStream.Position = 0;

            return isPortable;
        }
    }
}
