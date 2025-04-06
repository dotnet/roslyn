// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Extensions;

/// <summary>
/// This service is used to register, unregister and execute extension message handlers.
/// </summary>
internal interface IExtensionMessageHandlerService : IWorkspaceService
{
    /// <summary>
    /// Registers extension message handlers from the specified assembly.
    /// </summary>
    /// <param name="assemblyFilePath">The assembly to register and create message handlers from.</param>
    ValueTask RegisterExtensionAsync(string assemblyFilePath, CancellationToken cancellationToken);

    /// <summary>
    /// Unregisters extension message handlers previously registered from <paramref name="assemblyFilePath"/>.
    /// </summary>
    /// <param name="assemblyFilePath">The assembly for which handlers should be unregistered.</param>
    ValueTask UnregisterExtensionAsync(string assemblyFilePath, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the message names supported by the extension specified by <paramref name="assemblyFilePath"/>.
    /// </summary>
    ValueTask<GetExtensionMessageNamesResponse> GetExtensionMessageNamesAsync(string assemblyFilePath, CancellationToken cancellationToken);

    /// <summary>
    /// Unregisters all extension message handlers.
    /// </summary>
    ValueTask ResetAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Executes a non-document-specific extension message handler with the given message and solution.
    /// </summary>
    /// <param name="solution">The solution the message refers to.</param>
    /// <param name="messageName">The name of the handler to execute. This is generally the full name of the type implementing the handler.</param>
    /// <param name="jsonMessage">The json message to be passed to the handler.</param>
    /// <returns>The json message returned by the handler.</returns>
    ValueTask<string> HandleExtensionWorkspaceMessageAsync(
        Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a document-specific extension message handler with the given message and solution.
    /// </summary>
    /// <param name="documentId">The document the message refers to.</param>
    /// <param name="messageName">The name of the handler to execute. This is generally the full name of the type implementing the handler.</param>
    /// <param name="jsonMessage">The json message to be passed to the handler.</param>
    /// <returns>The json message returned by the handler.</returns>
    ValueTask<string> HandleExtensionDocumentMessageAsync(
        Document documentId, string messageName, string jsonMessage, CancellationToken cancellationToken);
}
