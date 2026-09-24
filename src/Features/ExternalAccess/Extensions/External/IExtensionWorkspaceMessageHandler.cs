// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Extensions;

/// <summary>
/// The base interface required to implement an extension message handler for non-document-specific messages.
/// </summary>
/// <typeparam name="TMessage">The type of the message received by the hander. <typeparamref name="TMessage"/> must be
/// serializable using System.Text.Json.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the hander. <typeparamref name="TResponse"/> must
/// be serializable using System.Text.Json.</typeparam>
/// <remarks>
/// <para>
/// Extension message handlers allow IDE extensions to send messages to the compiler and receive responses. This is
/// useful for scenarios where the IDE needs to query the compilation state or other information from the compiler.
/// </para>
/// <para>
/// The lifetime of a message handler object is tied to the solution: when the solution is closed, the assemblies that
/// contain the handler are unloaded. The state of any static variable will be lost. When a new solution is opened, a
/// new instance of the handler is created. Handlers that are defined in assemblies sharing the same folder are loaded
/// in the same assembly context and can share static state.
/// </para>
/// <para>
/// Multiple instances of a message handler may be created during the loading process.  However, only one will actually
/// be used as the handler that receives messages for a particular solution.
/// </para>
/// </remarks>
public interface IExtensionWorkspaceMessageHandler<TMessage, TResponse>
{
    /// <summary>
    /// The method that receives the message and returns the response.
    /// </summary>
    /// <param name="message">The message sent by the IDE.</param>
    /// <param name="context">The context containing the current state of the solution.</param>
    /// <returns>The response to be returned to the IDE.</returns>
    Task<TResponse> ExecuteAsync(TMessage message, ExtensionMessageContext context, CancellationToken cancellationToken);
}
