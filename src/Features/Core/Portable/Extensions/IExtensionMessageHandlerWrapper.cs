// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Extensions;

/// <summary>
/// Wrapper for an <c>IExtensionWorkspaceMessageHandler</c> or <c>IExtensionDocumentMessageHandler</c>
/// as returned by <see cref="IExtensionMessageHandlerFactory"/>. 
/// </summary>
internal interface IExtensionMessageHandlerWrapper
{
    /// <summary>
    /// The type of object received as parameter by the extension message handler.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// The type of object returned as result by the extension message handler.
    /// </summary>
    Type ResponseType { get; }

    /// <summary>
    /// The name of the extension message handler. This is generally the full name of the class implementing the handler.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The identifier of the extension that this message handler belongs to.
    /// </summary>
    string ExtensionIdentifier { get; }
}

/// <typeparam name="TArgument">The type of object received as parameter by the extension message
/// handler.</typeparam>
internal interface IExtensionMessageHandlerWrapper<TArgument> : IExtensionMessageHandlerWrapper
{
    /// <summary>
    /// Executes the extension message handler with the given message and document.
    /// </summary>
    /// <param name="message">An object of type <see cref="IExtensionMessageHandlerWrapper.MessageType"/> to be passed
    /// to the handler.</param>
    /// <param name="argument">The argument the handler operates on.</param>
    /// <returns>An object of type <see cref="IExtensionMessageHandlerWrapper.ResponseType"/> returned by the message
    /// handler.</returns>
    Task<object?> ExecuteAsync(object? message, TArgument argument, CancellationToken cancellationToken);
}
