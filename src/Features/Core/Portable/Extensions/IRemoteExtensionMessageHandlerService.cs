// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Extensions;

/// <summary>
/// Remote API for <see cref="IExtensionMessageHandlerService"/>.
/// </summary>
internal interface IRemoteExtensionMessageHandlerService
{
    ValueTask RegisterExtensionAsync(string assemblyFilePath, CancellationToken cancellationToken);

    ValueTask UnregisterExtensionAsync(string assemblyFilePath, CancellationToken cancellationToken);

    ValueTask ResetAsync(CancellationToken cancellationToken);

    ValueTask<ExtensionMessageNames> GetExtensionMessageNamesAsync(string assemblyFilePath, CancellationToken cancellationToken);

    ValueTask<ExtensionMessageResult> HandleExtensionDocumentMessageAsync(
        Checksum solutionChecksum, string messageName, string jsonMessage, DocumentId documentId, CancellationToken cancellationToken);

    ValueTask<ExtensionMessageResult> HandleExtensionWorkspaceMessageAsync(
        Checksum solutionChecksum, string messageName, string jsonMessage, CancellationToken cancellationToken);
}
