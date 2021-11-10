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

        public Task<DocumentDebugInfoReader?> GetDocumentDebugInfoReaderAsync(string dllPath, CancellationToken cancellationToken)
        {
            var dllStream = IOUtilities.PerformIO(() => File.OpenRead(dllPath));
            if (dllStream is null)
                return Task.FromResult<DocumentDebugInfoReader?>(null);

            Stream? pdbStream = null;
            DocumentDebugInfoReader? result = null;
            var peReader = new PEReader(dllStream);
            try
            {

                // The simplest possible thing is that the PDB happens to be right next to the DLL. You never know, we might get lucky.
                var pdbPath = Path.ChangeExtension(dllPath, ".pdb");
                if (File.Exists(pdbPath))
                {
                    pdbStream = IOUtilities.PerformIO(() => File.OpenRead(pdbPath));

                    if (pdbStream is not null &&
                        IsPortable(pdbStream))
                    {
                        var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);

                        result = new DocumentDebugInfoReader(peReader, pdbReaderProvider);
                    }
                }

                // Otherwise lets see if its an embedded PDB
                if (result is null)
                {
                    var entry = peReader.ReadDebugDirectory().FirstOrDefault(x => x.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                    if (entry.Type != DebugDirectoryEntryType.Unknown)
                    {
                        var pdbReaderProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);

                        result = new DocumentDebugInfoReader(peReader, pdbReaderProvider);
                    }
                }

                // TODO: Otherwise call the debugger to find the PDB from a symbol server etc.
                if (result is null)
                {
                    // Debugger needs:
                    // - PDB MVID
                    // - PDB Age
                    // - PDB TimeStamp
                    // - PDB Path
                    // - DLL Path
                    // 
                    // Most of this info comes from the CodeView Debug Directory from the dll
                }
            }
            catch (BadImageFormatException)
            {
                // If the PDB is corrupt in some way we can just ignore it, and let the system fall through to another provider
                // TODO: Log this to the output window: https://github.com/dotnet/roslyn/issues/57352
                result = null;
            }
            finally
            {
                // If we're returning a result then it will own the disposal of the reader, but if not
                // then we need to do it ourselves.
                if (result is null)
                {
                    pdbStream?.Dispose();
                    peReader.Dispose();
                }
            }

            return Task.FromResult<DocumentDebugInfoReader?>(result);
        }

        private static bool IsPortable(Stream pdbStream)
        {
            var isPortable = pdbStream.ReadByte() == 'B' && pdbStream.ReadByte() == 'S' && pdbStream.ReadByte() == 'J' && pdbStream.ReadByte() == 'B';
            pdbStream.Position = 0;

            return isPortable;
        }
    }
}
