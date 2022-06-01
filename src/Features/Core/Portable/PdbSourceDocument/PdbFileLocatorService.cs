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
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    [Export(typeof(IPdbFileLocatorService)), Shared]
    internal sealed class PdbFileLocatorService : IPdbFileLocatorService
    {
        private const int SymbolLocatorTimeout = 2000;

        private readonly ISourceLinkService? _sourceLinkService;
        private readonly IPdbSourceDocumentLogger? _logger;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code")]
        public PdbFileLocatorService(
            [Import(AllowDefault = true)] ISourceLinkService? sourceLinkService,
            [Import(AllowDefault = true)] IPdbSourceDocumentLogger? logger)
        {
            _sourceLinkService = sourceLinkService;
            _logger = logger;
        }

        public async Task<DocumentDebugInfoReader?> GetDocumentDebugInfoReaderAsync(string dllPath, bool useDefaultSymbolServers, TelemetryMessage telemetry, CancellationToken cancellationToken)
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
                if (peReader.TryOpenAssociatedPortablePdb(dllPath, pdbPath => File.OpenRead(pdbPath), out var pdbReaderProvider, out var pdbFilePath))
                {
                    Contract.ThrowIfNull(pdbReaderProvider);

                    if (pdbFilePath is null)
                    {
                        telemetry.SetPdbSource("embedded");
                        _logger?.Log(FeaturesResources.Found_embedded_PDB_file);
                    }
                    else
                    {
                        telemetry.SetPdbSource("ondisk");
                        _logger?.Log(FeaturesResources.Found_PDB_file_at_0, pdbFilePath);
                    }

                    result = new DocumentDebugInfoReader(peReader, pdbReaderProvider);
                }

                if (result is null)
                {
                    if (_sourceLinkService is null)
                    {
                        _logger?.Log(FeaturesResources.Could_not_find_PDB_on_disk_or_embedded);
                    }
                    else
                    {
                        var delay = Task.Delay(SymbolLocatorTimeout, cancellationToken);
                        // Call the debugger to find the PDB from a symbol server etc.
                        var pdbResultTask = _sourceLinkService.GetPdbFilePathAsync(dllPath, peReader, useDefaultSymbolServers, cancellationToken);

                        var winner = await Task.WhenAny(pdbResultTask, delay).ConfigureAwait(false);

                        if (winner == pdbResultTask)
                        {
                            var pdbResult = await pdbResultTask.ConfigureAwait(false);
                            if (pdbResult is not null)
                            {
                                pdbStream = IOUtilities.PerformIO(() => File.OpenRead(pdbResult.PdbFilePath));
                                if (pdbStream is not null)
                                {
                                    var readerProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
                                    telemetry.SetPdbSource("symbolserver");
                                    result = new DocumentDebugInfoReader(peReader, readerProvider);
                                    _logger?.Log(FeaturesResources.Found_PDB_on_symbol_server);
                                }
                                else
                                {
                                    _logger?.Log(FeaturesResources.Found_PDB_on_symbol_server_but_could_not_read_file);
                                }
                            }
                            else
                            {
                                _logger?.Log(FeaturesResources.Could_not_find_PDB_on_disk_or_embedded_or_server);
                            }
                        }
                        else
                        {
                            telemetry.SetPdbSource("timeout");
                            _logger?.Log(FeaturesResources.Timeout_symbol_server);
                        }
                    }
                }
            }
            catch (BadImageFormatException ex)
            {
                // If the PDB is corrupt in some way we can just ignore it, and let the system fall through to another provider
                _logger?.Log(FeaturesResources.Error_reading_PDB_0, ex.Message);
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
