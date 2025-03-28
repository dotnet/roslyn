// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

/// <summary>
/// Wrapper for an <c>ICustomMessageDocumentHandler</c> as returned by <see cref="ICustomMessageHandlerFactory"/>. 
/// </summary>
internal interface ICustomMessageDocumentHandlerWrapper
{
    /// <summary>
    /// The type of object received as parameter by the custom message handler.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// The type of object returned as result by the custom message handler.
    /// </summary>
    Type ResponseType { get; }

    /// <summary>
    /// The name of the custom message handler. This is generally the full name of the class implementing the handler.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the custom message handler with the given message and document.
    /// </summary>
    /// <param name="message">An object of type <see cref="MessageType"/> to be passed to the handler.</param>
    /// <param name="document">The document the handler operates on.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the async operation</param>
    /// <returns>An object of type <see cref="ResponseType"/> returned by the custom handler.</returns>
    Task<object?> ExecuteAsync(object? message, Document document, CancellationToken cancellationToken);
}
