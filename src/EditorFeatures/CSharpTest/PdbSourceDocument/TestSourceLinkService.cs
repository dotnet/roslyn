// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PdbSourceDocument;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PdbSourceDocument
{
    internal class TestSourceLinkService : ISourceLinkService
    {
        private readonly string? _pdbFilePath;
        private readonly string? _sourceFilePath;

        public TestSourceLinkService(string? pdbFilePath = null, string? sourceFilePath = null)
        {
            _pdbFilePath = pdbFilePath;
            _sourceFilePath = sourceFilePath;
        }

        public Task<PdbFilePathResult?> GetPdbFilePathAsync(string dllPath, PEReader peReader, bool useDefaultSymbolServers, CancellationToken cancellationToken)
        {
            if (_pdbFilePath is null)
            {
                return Task.FromResult<PdbFilePathResult?>(null);
            }

            return Task.FromResult<PdbFilePathResult?>(new PdbFilePathResult(_pdbFilePath));
        }

        public Task<SourceFilePathResult?> GetSourceFilePathAsync(string url, string relativePath, CancellationToken cancellationToken)
        {
            if (_sourceFilePath is null)
            {
                return Task.FromResult<SourceFilePathResult?>(null);
            }

            return Task.FromResult<SourceFilePathResult?>(new SourceFilePathResult(_sourceFilePath));
        }
    }
}
