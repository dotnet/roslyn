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
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger.Contracts.SourceLink;
using Microsoft.VisualStudio.Debugger.Contracts.SymbolLocator;

namespace Microsoft.VisualStudio.LanguageServices.PdbSourceDocument
{
    [Export(typeof(ISourceLinkService)), Shared]
    internal class SourceLinkService : ISourceLinkService
    {
        private readonly IDebuggerSymbolLocatorService _debuggerSymbolLocatorService;
        private readonly IDebuggerSourceLinkService _debuggerSourceLinkService;
        private readonly IPdbSourceDocumentLogger? _logger;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SourceLinkService(
            IDebuggerSymbolLocatorService debuggerSymbolLocatorService,
            IDebuggerSourceLinkService debuggerSourceLinkService,
            [Import(AllowDefault = true)] IPdbSourceDocumentLogger? logger)
        {
            _debuggerSymbolLocatorService = debuggerSymbolLocatorService;
            _debuggerSourceLinkService = debuggerSourceLinkService;
            _logger = logger;
        }

        public async Task<PdbFilePathResult?> GetPdbFilePathAsync(string dllPath, PEReader peReader, bool useDefaultSymbolServers, CancellationToken cancellationToken)
        {
            var hasCodeViewEntry = false;
            uint timeStamp = 0;
            CodeViewDebugDirectoryData codeViewEntry = default;
            using var _ = ArrayBuilder<PdbChecksum>.GetInstance(out var checksums);
            foreach (var entry in peReader.ReadDebugDirectory())
            {
                if (entry.Type == DebugDirectoryEntryType.PdbChecksum)
                {
                    var checksum = peReader.ReadPdbChecksumDebugDirectoryData(entry);
                    checksums.Add(new PdbChecksum(checksum.AlgorithmName, checksum.Checksum));
                }
                else if (entry.Type == DebugDirectoryEntryType.CodeView && entry.IsPortableCodeView)
                {
                    hasCodeViewEntry = true;
                    timeStamp = entry.Stamp;
                    codeViewEntry = peReader.ReadCodeViewDebugDirectoryData(entry);
                }
            }

            if (!hasCodeViewEntry)
                return null;

            var pdbInfo = new SymbolLocatorPdbInfo(
                Path.GetFileName(codeViewEntry.Path),
                codeViewEntry.Guid,
                (uint)codeViewEntry.Age,
                timeStamp,
                checksums.ToImmutable(),
                dllPath,
                codeViewEntry.Path);

            var flags = useDefaultSymbolServers
                ? SymbolLocatorSearchFlags.ForceNuGetSymbolServer | SymbolLocatorSearchFlags.ForceMsftSymbolServer
                : SymbolLocatorSearchFlags.None;
            var result = await _debuggerSymbolLocatorService.LocateSymbolFileAsync(pdbInfo, flags, progress: null, cancellationToken).ConfigureAwait(false);

            if (result.Found && result.SymbolFilePath is not null)
            {
                return new PdbFilePathResult(result.SymbolFilePath);
            }
            else if (_logger is not null)
            {
                // We log specific info from the debugger if there is a failure, but the caller will log general failure
                // information otherwise
                _logger.Log(result.Status);
                _logger.Log(result.Log);
            }

            return null;
        }

        public async Task<SourceFilePathResult?> GetSourceFilePathAsync(string url, string relativePath, CancellationToken cancellationToken)
        {
            var result = await _debuggerSourceLinkService.GetSourceLinkAsync(url, relativePath, allowInteractiveLogin: false, cancellationToken).ConfigureAwait(false);

            if (result.Status == SourceLinkResultStatus.Succeeded && result.Path is not null)
            {
                return new SourceFilePathResult(result.Path);
            }
            else if (_logger is not null && result.Log is not null)
            {
                // We log specific info from the debugger if there is a failure, but the caller will log general failure
                // information otherwise.
                _logger.Log(result.Log);
            }

            return null;
        }
    }
}
