// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Storage;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Storage;

[Export(typeof(IFSharpChecksummedPersistentStorageService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FSharpChecksummedPersistentStorageService() : IFSharpChecksummedPersistentStorageService
{
    public async ValueTask<IFSharpChecksummedPersistentStorage> GetStorageAsync(Solution solution, CancellationToken cancellationToken)
    {
        var storageService = solution.Services.GetPersistentStorageService();
        var storage = await storageService.GetStorageAsync(SolutionKey.ToSolutionKey(solution), cancellationToken).ConfigureAwait(false);
        return new FSharpChecksummedPersistentStorage(storage);
    }

    private sealed class FSharpChecksummedPersistentStorage(IChecksummedPersistentStorage storage) : IFSharpChecksummedPersistentStorage
    {
        public Task<bool> ChecksumMatchesAsync(Document document, string name, ImmutableArray<byte> checksum, CancellationToken cancellationToken)
            => storage.ChecksumMatchesAsync(document, name, Checksum.From(checksum), cancellationToken);

        public Task<Stream?> ReadStreamAsync(Document document, string name, ImmutableArray<byte> checksum, CancellationToken cancellationToken)
            => storage.ReadStreamAsync(document, name, ToOptionalChecksum(checksum), cancellationToken);

        public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, ImmutableArray<byte> checksum, CancellationToken cancellationToken)
            => storage.WriteStreamAsync(document, name, stream, ToOptionalChecksum(checksum), cancellationToken);

        private static Checksum? ToOptionalChecksum(ImmutableArray<byte> checksum)
            => checksum.IsDefault ? null : Checksum.From(checksum);
    }
}
