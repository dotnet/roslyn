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

        public Task<(MetadataReader, MetadataReader)?> GetMetadataReadersAsync(string dllPath, CancellationToken cancellationToken)
        {
            using var dllStream = File.OpenRead(dllPath);

            // The simplest possible thing is that the PDB happens to be right next to the DLL
            var pdbPath = Path.ChangeExtension(dllPath, ".pdb");
            if (File.Exists(pdbPath))
            {
                //using var dllReaderProvider = MetadataReaderProvider.FromMetadataStream(dllStream, leaveOpen: true);   // TODO: Fails with "System.BadImageFormatException : Invalid COR20 header signature.", from tests at least
                using var dllReaderProvider = ModuleMetadata.CreateFromStream(dllStream);
                var dllReader = dllReaderProvider.GetMetadataReader();

                using var pdbStream = File.OpenRead(pdbPath);
                using var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
                var pdbReader = pdbReaderProvider.GetMetadataReader();

                return Task.FromResult<(MetadataReader, MetadataReader)?>((dllReader, pdbReader));
            }

            // Otherwise lets see if its an embedded PDB. We'll need to read the DLL to get info
            // for the debugger anyway
            using var peReader = new PEReader(dllStream);

            var entry = peReader.ReadDebugDirectory().SingleOrDefault(x => x.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            if (entry.Type != DebugDirectoryEntryType.Unknown)
            {
                using var provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
                var pdbReader = provider.GetMetadataReader();
                var dllReader = peReader.GetMetadataReader();

                return Task.FromResult<(MetadataReader, MetadataReader)?>((dllReader, pdbReader));
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

            return Task.FromResult<(MetadataReader, MetadataReader)?>(null);
        }
    }
}
