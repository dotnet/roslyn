// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Host;

internal sealed class NoOpPersistentStorageService : IChecksummedPersistentStorageService
{
    private static readonly IChecksummedPersistentStorageService Instance = new NoOpPersistentStorageService();

    private NoOpPersistentStorageService()
    {
    }

    public static IChecksummedPersistentStorageService GetOrThrow(IPersistentStorageConfiguration configuration)
        => configuration.ThrowOnFailure
            ? throw new InvalidOperationException("Database was not supported")
            : Instance;

    public async ValueTask<IChecksummedPersistentStorage> GetStorageAsync(SolutionKey solutionKey, CancellationToken cancellationToken)
        => NoOpPersistentStorage.GetOrThrow(solutionKey, throwOnFailure: false);
}
