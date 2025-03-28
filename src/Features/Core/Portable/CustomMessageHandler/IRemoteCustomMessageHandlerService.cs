// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

internal interface IRemoteCustomMessageHandlerService
{
    ValueTask<RegisterHandlersResponse> LoadCustomMessageHandlersAsync(
        Checksum solutionChecksum,
        string assemblyFolderPath,
        string assemblyFileName,
        CancellationToken cancellationToken);

    ValueTask<string> HandleCustomDocumentMessageAsync(
        Checksum solutionChecksum,
        string messageName,
        string jsonMessage,
        DocumentId documentId,
        CancellationToken cancellationToken);

    ValueTask<string> HandleCustomMessageAsync(
        Checksum solutionChecksum,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken);

    ValueTask UnloadCustomMessageHandlersAsync(
        string assemblyFolderPath,
        CancellationToken cancellationToken);

    ValueTask ResetAsync(
        CancellationToken cancellationToken);
}
