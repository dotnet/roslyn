// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Extensions;

/// <summary>
/// Factory for creating instances of extension message handlers.
/// </summary>
internal interface IExtensionMessageHandlerFactory
{
    /// <summary>
    /// Creates <see cref="IExtensionWorspaceMessageHandlerWrapper"/> instances for each <c>IExtensionWorkspaceMessageHandler</c> type in <paramref name="assembly"/>.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <returns>The handlers.</returns>
    ImmutableArray<IExtensionWorspaceMessageHandlerWrapper> CreateWorkspaceMessageHandlers(Assembly assembly);

    /// <summary>
    /// Creates <see cref="IExtensionDocumentMessageHandlerWrapper"/> instances for each <c>IExtensionDocumentMessageHandler</c> type in <paramref name="assembly"/>.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <returns>The handlers.</returns>
    ImmutableArray<IExtensionDocumentMessageHandlerWrapper> CreateDocumentMessageHandlers(Assembly assembly);
}
