// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Extensions;

/// <summary>
/// Factory for creating instances of extension message handlers.
/// </summary>
internal interface IExtensionMessageHandlerFactory : IWorkspaceService
{
    /// <summary>
    /// Creates <see cref="IExtensionMessageHandlerWrapper{Solution}"/> instances for each
    /// <c>IExtensionWorkspaceMessageHandler</c> type in <paramref name="assembly"/>.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <param name="extensionIdentifier">Unique identifier of the extension owning this handler.</param>
    /// <remarks>May be called multiple times for the same <see cref="Assembly"/> instance.</remarks>
    ImmutableArray<IExtensionMessageHandlerWrapper<Solution>> CreateWorkspaceMessageHandlers(
        Assembly assembly, string extensionIdentifier, CancellationToken cancellationToken);

    /// <summary>
    /// Creates <see cref="IExtensionMessageHandlerWrapper{Document}"/> instances for each
    /// <c>IExtensionDocumentMessageHandler</c> type in <paramref name="assembly"/>.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <param name="extensionIdentifier">Unique identifier of the extension owning this handler.</param>
    /// <remarks>May be called multiple times for the same <see cref="Assembly"/> instance.</remarks>
    ImmutableArray<IExtensionMessageHandlerWrapper<Document>> CreateDocumentMessageHandlers(
        Assembly assembly, string extensionIdentifier, CancellationToken cancellationToken);
}
