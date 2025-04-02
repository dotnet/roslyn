// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Extensions;

internal interface IRemoteExtensionMessageHandlerService
{
    ValueTask<string> HandleExtensionDocumentMessageAsync(
        Checksum solutionChecksum,
        string messageName,
        string jsonMessage,
        DocumentId documentId,
        CancellationToken cancellationToken);

    ValueTask<string> HandleExensionWorkspaceMessageAsync(
        Checksum solutionChecksum,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken);

    ValueTask<RegisterExtensionResponse> RegisterExtensionAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken);

    ValueTask UnregisterExtensionAsync(
        string assemblyFilePath,
        CancellationToken cancellationToken);

    ValueTask ResetAsync(
        CancellationToken cancellationToken);
}
