// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

/// <summary>
/// Factory for creating instances of custom message handlers.
/// </summary>
internal interface ICustomMessageHandlerFactory
{
    /// <summary>
    /// Creates <see cref="ICustomMessageHandlerWrapper"/> instances for each <c>ICustomMessageHandler</c> type in <paramref name="assembly"/>.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <returns>The handlers.</returns>
    ImmutableArray<ICustomMessageHandlerWrapper> CreateMessageHandlers(Assembly assembly);

    /// <summary>
    /// Creates <see cref="ICustomMessageDocumentHandlerWrapper"/> instances for each <c>ICustomMessageDocumentHandler</c> type in <paramref name="assembly"/>.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <returns>The handlers.</returns>
    ImmutableArray<ICustomMessageDocumentHandlerWrapper> CreateMessageDocumentHandlers(Assembly assembly);
}
