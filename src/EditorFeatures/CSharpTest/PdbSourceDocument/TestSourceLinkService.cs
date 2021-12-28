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
        private readonly bool _isPortablePdb;

        public TestSourceLinkService(string? pdbFilePath = null, string? sourceFilePath = null, bool isPortablePdb = true)
        {
            _pdbFilePath = pdbFilePath;
            _sourceFilePath = sourceFilePath;
            _isPortablePdb = isPortablePdb;
        }

        public Task<PdbFilePathResult?> GetPdbFilePathAsync(string dllPath, PEReader peReader, IPdbSourceDocumentLogger? logger, CancellationToken cancellationToken)
        {
            if (_pdbFilePath is null)
            {
                return Task.FromResult<PdbFilePathResult?>(null);
            }

            return Task.FromResult<PdbFilePathResult?>(new PdbFilePathResult(_pdbFilePath, "status", Log: null, _isPortablePdb));
        }

        public Task<SourceFilePathResult?> GetSourceFilePathAsync(string url, string relativePath, IPdbSourceDocumentLogger? logger, CancellationToken cancellationToken)
        {
            if (_sourceFilePath is null)
            {
                return Task.FromResult<SourceFilePathResult?>(null);
            }

            return Task.FromResult<SourceFilePathResult?>(new SourceFilePathResult(_sourceFilePath, Log: null));
        }
    }
}
