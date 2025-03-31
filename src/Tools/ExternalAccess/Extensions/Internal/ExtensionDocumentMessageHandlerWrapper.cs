// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Extensions;

internal sealed class ExtensionDocumentMessageHandlerWrapper : IExtensionDocumentMessageHandlerWrapper
{
    private readonly object handler;
    private readonly MethodInfo executeAsyncMethod;
    private readonly PropertyInfo responseTaskResultProperty;

    public ExtensionDocumentMessageHandlerWrapper(object handler, Type iCustomMessageDocumentHandlerInterface)
    {
        this.handler = handler;

        Name = handler.GetType().FullName;
        MessageType = iCustomMessageDocumentHandlerInterface.GenericTypeArguments[0];
        ResponseType = iCustomMessageDocumentHandlerInterface.GenericTypeArguments[1];

        executeAsyncMethod = iCustomMessageDocumentHandlerInterface.GetMethod(nameof(IExtensionWorkspaceMessageHandler<,>.ExecuteAsync));
        responseTaskResultProperty = typeof(Task<>).MakeGenericType(ResponseType).GetProperty(nameof(Task<>.Result));
    }

    public Type MessageType { get; }

    public Type ResponseType { get; }

    public string Name { get; }

    public async Task<object?> ExecuteAsync(object? message, Document document, CancellationToken cancellationToken)
    {
        if ((message is null && MessageType.IsValueType) || (message is not null && !MessageType.IsAssignableFrom(message.GetType())))
        {
            throw new InvalidOperationException($"The message type {message?.GetType().FullName ?? "null"} is not assignable to {MessageType.FullName}.");
        }

        var responseTask = (Task)executeAsyncMethod.Invoke(handler, [message, new ExtensionMessageContext(document.Project.Solution), document, cancellationToken]);
        await responseTask.ConfigureAwait(false);
        var response = responseTaskResultProperty.GetValue(responseTask);

        if ((response is null && ResponseType.IsValueType) || (response is not null && !ResponseType.IsAssignableFrom(response.GetType())))
        {
            throw new InvalidOperationException($"The message type {response?.GetType().FullName ?? "null"} is not assignable to {ResponseType.FullName}.");
        }

        return response;
    }
}
