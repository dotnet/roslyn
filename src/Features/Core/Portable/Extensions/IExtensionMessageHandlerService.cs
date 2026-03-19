// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Extensions;

[DataContract]
internal readonly record struct ExtensionMessageNames(
    [property: DataMember(Order = 0)] ImmutableArray<string> WorkspaceMessageHandlers,
    [property: DataMember(Order = 1)] ImmutableArray<string> DocumentMessageHandlers,
    // Note: ServiceHub supports translating *all* exceptions over the wire.  Even exceptions whose types are not
    // available on the other side.
    [property: DataMember(Order = 2)] Exception? ExtensionException);

/// <param name="Response">The result value the extension produced, converted to Json using JsonSerializer.Serialize. On
/// the object value returned from <see cref="IExtensionMessageHandlerWrapper{TArgument}.ExecuteAsync"/>.  Can be <see
/// langword="null"/> if the extension was unloaded, or there was an extension thrown by the extension.  If the
/// extension actually returns <see langword="null"/> then that will be encoded into Json as <c>"null"</c>.</param>
/// <param name="ExtensionWasUnloaded">If the extension the message was called for was previously unloaded and is no longer
/// available to perform the request.</param>
/// <param name="ExtensionException">Any exception produced by the extension itself during <see
/// cref="IExtensionMessageHandlerWrapper{TArgument}.ExecuteAsync"/>.</param>
[DataContract]
internal readonly record struct ExtensionMessageResult(
    [property: DataMember(Order = 0)] string? Response,
    [property: DataMember(Order = 1)] bool ExtensionWasUnloaded,
    // Note: ServiceHub supports translating *all* exceptions over the wire.  Even exceptions whose types are not
    // available on the other side.
    [property: DataMember(Order = 2)] Exception? ExtensionException);

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
    /// Unregisters all extension message handlers.
    /// </summary>
    ValueTask ResetAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the message names supported by the extension specified by <paramref name="assemblyFilePath"/>.
    /// </summary>
    ValueTask<ExtensionMessageNames> GetExtensionMessageNamesAsync(string assemblyFilePath, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a non-document-specific extension message handler with the given message and solution.
    /// </summary>
    /// <param name="solution">The solution the message refers to.</param>
    /// <param name="messageName">The name of the handler to execute. This is generally the full name of the type implementing the handler.</param>
    /// <param name="jsonMessage">The json message to be passed to the handler.</param>
    /// <returns>The json message returned by the handler.</returns>
    ValueTask<ExtensionMessageResult> HandleExtensionWorkspaceMessageAsync(
        Solution solution, string messageName, string jsonMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a document-specific extension message handler with the given message and solution.
    /// </summary>
    /// <param name="documentId">The document the message refers to.</param>
    /// <param name="messageName">The name of the handler to execute. This is generally the full name of the type implementing the handler.</param>
    /// <param name="jsonMessage">The json message to be passed to the handler.</param>
    /// <returns>The json message returned by the handler.</returns>
    ValueTask<ExtensionMessageResult> HandleExtensionDocumentMessageAsync(
        Document documentId, string messageName, string jsonMessage, CancellationToken cancellationToken);
}
