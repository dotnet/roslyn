// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
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
        private const int SymbolLocatorTimeout = 2000;

        private readonly ISourceLinkService? _sourceLinkService;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code")]
        public PdbFileLocatorService([Import(AllowDefault = true)] ISourceLinkService? sourceLinkService)
        {
            _sourceLinkService = sourceLinkService;
        }

        public async Task<DocumentDebugInfoReader?> GetDocumentDebugInfoReaderAsync(string dllPath, IPdbSourceDocumentLogger? logger, CancellationToken cancellationToken)
        {
            var dllStream = IOUtilities.PerformIO(() => File.OpenRead(dllPath));
            if (dllStream is null)
                return null;

            Stream? pdbStream = null;
            DocumentDebugInfoReader? result = null;
            var peReader = new PEReader(dllStream);
            try
            {
                // Try to load the pdb file from disk, or embedded
                if (peReader.TryOpenAssociatedPortablePdb(dllPath, pdbPath => File.OpenRead(pdbPath), out var pdbReaderProvider, out _))
                {
                    Contract.ThrowIfNull(pdbReaderProvider);

                    result = new DocumentDebugInfoReader(peReader, pdbReaderProvider);
                }

                // Otherwise call the debugger to find the PDB from a symbol server etc.
                if (result is null && _sourceLinkService is not null)
                {
                    var delay = Task.Delay(SymbolLocatorTimeout, cancellationToken);
                    var pdbResultTask = _sourceLinkService.GetPdbFilePathAsync(dllPath, peReader, logger, cancellationToken);

                    var winner = await Task.WhenAny(pdbResultTask, delay).ConfigureAwait(false);

                    if (winner == pdbResultTask)
                    {
                        var pdbResult = await pdbResultTask.ConfigureAwait(false);

                        // TODO: Support windows PDBs: https://github.com/dotnet/roslyn/issues/55834
                        // TODO: Log results from pdbResult.Log: https://github.com/dotnet/roslyn/issues/57352
                        if (pdbResult is not null)
                        {
                            pdbStream = IOUtilities.PerformIO(() => File.OpenRead(pdbResult.PdbFilePath));
                            if (pdbStream is not null)
                            {
                                var readerProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
                                result = new DocumentDebugInfoReader(peReader, readerProvider);
                            }
                        }
                    }
                    else
                    {
                        // TODO: Log the timeout: https://github.com/dotnet/roslyn/issues/57352
                    }
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

            return result;
        }
    }
}
