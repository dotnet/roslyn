// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

internal sealed class CustomMessageHandlerWrapper : ICustomMessageHandlerWrapper
{
    private readonly object handler;
    private readonly MethodInfo executeAsyncMethod;
    private readonly PropertyInfo responseTaskResultProperty;

    public CustomMessageHandlerWrapper(object handler)
    {
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));

        Type iCustomMessageHandlerInterface;
        try
        {
            iCustomMessageHandlerInterface = handler.GetType().GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICustomMessageHandler<,>))
                .Single();
        }
        catch
        {
            throw new InvalidOperationException($"The handler {handler.GetType().FullName} must implement {nameof(ICustomMessageHandler<,>)} once.");
        }

        MessageType = iCustomMessageHandlerInterface.GenericTypeArguments[0];
        ResponseType = iCustomMessageHandlerInterface.GenericTypeArguments[1];
        var responseTaskType = iCustomMessageHandlerInterface.GenericTypeArguments[1];

        executeAsyncMethod = iCustomMessageHandlerInterface.GetMethod(nameof(ICustomMessageHandler<,>.ExecuteAsync));
        responseTaskResultProperty = typeof(Task<>).MakeGenericType(ResponseType).GetProperty(nameof(Task<>.Result));
    }

    public Type MessageType { get; }

    public Type ResponseType { get; }

    public async Task<object?> ExecuteAsync(object? message, Document? document, Solution solution, CancellationToken cancellationToken)
    {
        if ((message is null && MessageType.IsValueType) || (message is not null && !MessageType.IsAssignableFrom(message.GetType())))
        {
            throw new InvalidOperationException($"The message type {message?.GetType().FullName ?? "null"} is not assignable to {MessageType.FullName}.");
        }

        var responseTask = (Task)executeAsyncMethod.Invoke(handler, [message, new CustomMessageContext(document, solution), cancellationToken]);
        await responseTask.ConfigureAwait(false);
        var response = responseTaskResultProperty.GetValue(responseTask);

        if ((response is null && ResponseType.IsValueType) || (response is not null && !ResponseType.IsAssignableFrom(response.GetType())))
        {
            throw new InvalidOperationException($"The message type {response?.GetType().FullName ?? "null"} is not assignable to {ResponseType.FullName}.");
        }

        return response;
    }
}
