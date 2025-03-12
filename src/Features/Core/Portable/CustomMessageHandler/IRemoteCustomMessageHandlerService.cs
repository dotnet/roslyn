// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

internal interface IRemoteCustomMessageHandlerService
{
    /// <summary>
    /// Loads the assembly at <paramref name="assemblyPath"/>
    /// loads the <paramref name="typeFullName"/> handler from the assembly,
    /// converts <paramref name="jsonMessage"/> to a .NET object of the type expected by the handler,
    /// dispatches the message to the handler,
    /// returns the handler's response to the caller as a json string.
    /// </summary>
    /// <returns>A json message.</returns>
    ValueTask<string> HandleCustomMessageAsync(
        Checksum solutionChecksum,
        string assemblyPath,
        string typeFullName,
        string jsonMessage,
        DocumentId? documentId,
        CancellationToken cancellationToken);

    ValueTask UnloadCustomMessageHandlerAsync(
        string assemblyPath,
        CancellationToken cancellationToken);
}
