// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal sealed partial class TrivialTemporaryStorageService
{
    private sealed class TrivialStorageTextHandle(
        TemporaryStorageIdentifier identifier,
        SourceText sourceText) : ITemporaryStorageTextHandle
    {
        public TemporaryStorageIdentifier Identifier => identifier;

        public SourceText ReadFromTemporaryStorage(CancellationToken cancellationToken)
            => sourceText;

        public Task<SourceText> ReadFromTemporaryStorageAsync(CancellationToken cancellationToken)
            => Task.FromResult(ReadFromTemporaryStorage(cancellationToken));
    }
}
