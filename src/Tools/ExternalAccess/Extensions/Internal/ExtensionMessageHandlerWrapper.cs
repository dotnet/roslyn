// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Extensions;

internal abstract class ExtensionHandlerWrapper<TArgument>
    : IExtensionMessageHandlerWrapper<TArgument>
{
    private readonly object _handler;
    private readonly MethodInfo _executeAsyncMethod;
    private readonly PropertyInfo _responseTaskResultProperty;

    protected ExtensionHandlerWrapper(object handler, Type customMessageHandlerInterface, string extensionIdentifier)
    {
        _handler = handler;

        Name = handler.GetType().FullName;
        MessageType = customMessageHandlerInterface.GenericTypeArguments[0];
        ResponseType = customMessageHandlerInterface.GenericTypeArguments[1];
        ExtensionIdentifier = extensionIdentifier;

        _executeAsyncMethod = customMessageHandlerInterface.GetMethod(nameof(ExecuteAsync));
        _responseTaskResultProperty = typeof(Task<>).MakeGenericType(ResponseType).GetProperty(nameof(Task<int>.Result));
    }

    public Type MessageType { get; }

    public Type ResponseType { get; }

    public string Name { get; }

    public string ExtensionIdentifier { get; }

    public async Task<object?> ExecuteAsync(object? message, TArgument argument, CancellationToken cancellationToken)
    {
        if ((message is null && MessageType.IsValueType) || (message is not null && !MessageType.IsAssignableFrom(message.GetType())))
        {
            throw new InvalidOperationException($"The message type {message?.GetType().FullName ?? "null"} is not assignable to {MessageType.FullName}.");
        }

        var responseTask = ExecuteAsync(_executeAsyncMethod, _handler, message, argument, cancellationToken);
        await responseTask.ConfigureAwait(false);
        var response = _responseTaskResultProperty.GetValue(responseTask);

        if ((response is null && ResponseType.IsValueType) || (response is not null && !ResponseType.IsAssignableFrom(response.GetType())))
        {
            throw new InvalidOperationException($"The message type {response?.GetType().FullName ?? "null"} is not assignable to {ResponseType.FullName}.");
        }

        return response;
    }

    protected abstract Task ExecuteAsync(MethodInfo executeAsyncMethod, object handler, object? message, TArgument argument, CancellationToken cancellationToken);
}
