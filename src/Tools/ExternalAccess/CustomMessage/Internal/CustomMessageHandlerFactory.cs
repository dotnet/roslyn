// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

[Export(typeof(ICustomMessageHandlerFactory)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CustomMessageHandlerFactory() : ICustomMessageHandlerFactory
{
    public ImmutableArray<ICustomMessageDocumentHandlerWrapper> CreateMessageDocumentHandlers(Assembly assembly)
    {
        var resultBuilder = ImmutableArray.CreateBuilder<ICustomMessageDocumentHandlerWrapper>();

        foreach (var t in assembly.GetTypes())
        {
            var (handler, handlerInterface) = CreateHandlerIfInterfaceIsImplemented(t, typeof(ICustomMessageDocumentHandler<,>));
            if (handler is null || handlerInterface is null)
            {
                continue;
            }

            resultBuilder.Add(new CustomMessageDocumentHandlerWrapper(handler, handlerInterface);
        }

        return resultBuilder.ToImmutable();
    }

    public ImmutableArray<ICustomMessageHandlerWrapper> CreateMessageHandlers(Assembly assembly)
    {
        var resultBuilder = ImmutableArray.CreateBuilder<ICustomMessageHandlerWrapper>();

        foreach (var t in assembly.GetTypes())
        {
            var (handler, handlerInterface) = CreateHandlerIfInterfaceIsImplemented(t, typeof(ICustomMessageHandler<,>));
            if (handler is null || handlerInterface is null)
            {
                continue;
            }

            resultBuilder.Add(new CustomMessageHandlerWrapper(handler, handlerInterface);
        }

        return resultBuilder.ToImmutable();
    }

    // unboundInterfaceType is either ICustomMessageHandler<,> or ICustomMessageDocumentHandler<,>
    private static (object? Handler, Type? Interface) CreateHandlerIfInterfaceIsImplemented(Type candidateType, Type unboundInterfaceType)
    {
        if (candidateType.IsAbstract || candidateType.IsGenericType)
        {
            return default;
        }

        Type? boundInterfaceType = null;
        foreach (var i in candidateType.GetInterfaces())
        {
            if (i.IsGenericType &&
                !i.IsGenericTypeDefinition &&
                i.GetGenericTypeDefinition() == unboundInterfaceType)
            {
                if (boundInterfaceType is not null)
                {
                    throw new InvalidOperationException($"Type {candidateType.FullName} implements interface {unboundInterfaceType.Name} more than once.");
                }

                boundInterfaceType = i;
            }
        }

        if (boundInterfaceType == null)
        {
            return default;
        }

        var handler = Activator.CreateInstance(candidateType)
            ?? throw new InvalidOperationException($"Cannot create {candidateType.FullName}.");

        return (handler, boundInterfaceType);
    }
}
