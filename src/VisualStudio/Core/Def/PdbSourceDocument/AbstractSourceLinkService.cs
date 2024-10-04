// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger.Contracts.SourceLink;
using Microsoft.VisualStudio.Debugger.Contracts.SymbolLocator;

namespace Microsoft.VisualStudio.LanguageServices.PdbSourceDocument;

internal abstract class AbstractSourceLinkService : ISourceLinkService
{
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
        var result = await LocateSymbolFileAsync(pdbInfo, flags, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            Logger?.Log($"{nameof(LocateSymbolFileAsync)} returned null");
            return null;
        }

        if (result.Value.Found && result.Value.SymbolFilePath is not null)
        {
            return new PdbFilePathResult(result.Value.SymbolFilePath);
        }
        else if (Logger is not null)
        {
            // We log specific info from the debugger if there is a failure, but the caller will log general failure
            // information otherwise
            Logger.Log(result.Value.Status);
            Logger.Log(result.Value.Log);
        }

        return null;
    }

    public async Task<SourceFilePathResult?> GetSourceFilePathAsync(string url, string relativePath, CancellationToken cancellationToken)
    {
        var result = await GetSourceLinkAsync(url, relativePath, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            Logger?.Log($"{nameof(GetSourceLinkAsync)} returned null");
            return null;
        }

        if (result.Value.Status == SourceLinkResultStatus.Succeeded && result.Value.Path is not null)
        {
            return new SourceFilePathResult(result.Value.Path);
        }
        else if (Logger is not null && result.Value.Log is not null)
        {
            // We log specific info from the debugger if there is a failure, but the caller will log general failure
            // information otherwise.
            Logger.Log(result.Value.Log);
        }

        return null;
    }

    protected abstract Task<SymbolLocatorResult?> LocateSymbolFileAsync(SymbolLocatorPdbInfo pdbInfo, SymbolLocatorSearchFlags flags, CancellationToken cancellationToken);

    protected abstract Task<SourceLinkResult?> GetSourceLinkAsync(string url, string relativePath, CancellationToken cancellationToken);

    protected abstract IPdbSourceDocumentLogger? Logger { get; }
}
