// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PdbSourceDocument;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PdbSourceDocument;

internal sealed class TestSourceLinkService : ISourceLinkService
{
    private readonly string? _pdbFilePath;
    private readonly string? _sourceFilePath;

    public TestSourceLinkService(string? pdbFilePath = null, string? sourceFilePath = null)
    {
        _pdbFilePath = pdbFilePath;
        _sourceFilePath = sourceFilePath;
    }

    public async Task<PdbFilePathResult?> GetPdbFilePathAsync(string dllPath, PEReader peReader, bool useDefaultSymbolServers, CancellationToken cancellationToken)
    {
        if (_pdbFilePath is null)
        {
            return null;
        }

        return new PdbFilePathResult(_pdbFilePath);
    }

    public async Task<SourceFilePathResult?> GetSourceFilePathAsync(string url, string relativePath, CancellationToken cancellationToken)
    {
        if (_sourceFilePath is null)
        {
            return null;
        }

        return new SourceFilePathResult(_sourceFilePath);
    }
}
