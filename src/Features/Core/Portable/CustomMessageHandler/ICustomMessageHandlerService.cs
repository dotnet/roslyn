// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

/// <summary>
/// This service is used to load, unload and execute custom message handlers.
/// </summary>
internal interface ICustomMessageHandlerService : IWorkspaceService
{
    /// <summary>
    /// Loads custom message handlers from the specified assembly.
    /// </summary>
    /// <param name="assemblyFolderPath">The folder containing <paramref name="assemblyFileName"/>.</param>
    /// <param name="assemblyFileName">The assembly to load and create message handlers from.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the async operation.</param>
    /// <returns>The names of the loaded handlers.</returns>
    ValueTask<RegisterHandlersResponse> LoadCustomMessageHandlersAsync(
        string assemblyFolderPath,
        string assemblyFileName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a non-document-specific custom message handler with the given message and solution.
    /// </summary>
    /// <param name="solution">The solution the message refers to.</param>
    /// <param name="messageName">The name of the handler to execute. This is generally the full name of the type implementing the handler.</param>
    /// <param name="jsonMessage">The json message to be passed to the handler.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the async operation.</param>
    /// <returns>The json message returned by the handler.</returns>
    ValueTask<string> HandleCustomMessageAsync(
        Solution solution,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a document-specific custom message handler with the given message and solution.
    /// </summary>
    /// <param name="documentId">The document the message refers to.</param>
    /// <param name="messageName">The name of the handler to execute. This is generally the full name of the type implementing the handler.</param>
    /// <param name="jsonMessage">The json message to be passed to the handler.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the async operation.</param>
    /// <returns>The json message returned by the handler.</returns>
    ValueTask<string> HandleCustomDocumentMessageAsync(
        Document documentId,
        string messageName,
        string jsonMessage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Unloads custom message handlers for all assemblies previously loaded from <paramref name="assemblyFolderPath"/>.
    /// </summary>
    /// <param name="assemblyFolderPath">The path for which handlers should be unloaded.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the async operation.</param>
    /// <returns>A task representing the async operation.</returns>
    ValueTask UnloadCustomMessageHandlersAsync(
        string assemblyFolderPath,
        CancellationToken cancellationToken);

    /// <summary>
    /// Unloads all custom message handlers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the async operation.</param>
    /// <returns>A task representing the async operation.</returns>
    ValueTask ResetAsync(
        CancellationToken cancellationToken);
}
