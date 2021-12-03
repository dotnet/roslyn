// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

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
                if (peReader.TryOpenAssociatedPortablePdb(dllPath, pdbPath => File.OpenRead(pdbPath), out var pdbReaderProvider, out _))
                {
                    Contract.ThrowIfNull(pdbReaderProvider);

                    result = new DocumentDebugInfoReader(peReader, pdbReaderProvider);
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
    }
}
