// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

internal interface ICustomMessageHandlerService : IWorkspaceService
{
    ValueTask<RegisterHandlersResponse> LoadCustomMessageHandlersAsync(
        string assemblyFolderPath,
        string assemblyFileName,
        CancellationToken cancellationToken);

    ValueTask<string> HandleCustomMessageAsync(
        Solution solution,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken);

    ValueTask<string> HandleCustomDocumentMessageAsync(
        string messageName,
        string jsonMessage,
        Document documentId,
        CancellationToken cancellationToken);

    ValueTask UnloadCustomMessageHandlersAsync(
        string assemblyFolderPath,
        CancellationToken cancellationToken);

    ValueTask ResetAsync(
        CancellationToken cancellationToken);
}
