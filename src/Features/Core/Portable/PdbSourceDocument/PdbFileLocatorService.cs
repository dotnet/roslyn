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

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    [Export(typeof(IPdbFileLocatorService)), Shared]
    internal class PdbFileLocatorService : IPdbFileLocatorService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PdbFileLocatorService()
        {
        }

        public Task<MultiMetadataReaderProvider?> GetMetadataReadersAsync(string dllPath, CancellationToken cancellationToken)
        {
            var dllStream = File.OpenRead(dllPath);

            // The simplest possible thing is that the PDB happens to be right next to the DLL. You know, we might get lucky.
            var pdbPath = Path.ChangeExtension(dllPath, ".pdb");
            if (File.Exists(pdbPath))
            {
                //var dllReaderProvider = MetadataReaderProvider.FromMetadataStream(dllStream);   // TODO: Fails with "System.BadImageFormatException : Invalid COR20 header signature.", from tests at least
                var dllReaderProvider = ModuleMetadata.CreateFromStream(dllStream);

                var pdbStream = File.OpenRead(pdbPath);
                var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);

                var result = new MultiMetadataReaderProvider(dllStream, dllReaderProvider, pdbStream, pdbReaderProvider);
                return Task.FromResult<MultiMetadataReaderProvider?>(result);
            }

            // Otherwise lets see if its an embedded PDB. We'll need to read the DLL to get info
            // for the debugger anyway
            var peReader = new PEReader(dllStream);

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
    }
}
