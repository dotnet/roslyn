// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.ExternalAccess.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Extensions;

[ExportWorkspaceService(typeof(IExtensionMessageHandlerFactory)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ExtensionMessageHandlerFactory() : IExtensionMessageHandlerFactory
{
    public ImmutableArray<IExtensionMessageHandlerWrapper<Document>> CreateDocumentMessageHandlers(
        Assembly assembly, string extensionIdentifier, CancellationToken cancellationToken)
        => CreateWorkspaceHandlers(
            assembly,
            typeof(IExtensionDocumentMessageHandler<,>),
            (handler, handlerInterface) => new ExtensionDocumentMessageHandlerWrapper(handler, handlerInterface, extensionIdentifier),
            cancellationToken);

    public ImmutableArray<IExtensionMessageHandlerWrapper<Solution>> CreateWorkspaceMessageHandlers(
        Assembly assembly, string extensionIdentifier, CancellationToken cancellationToken)
        => CreateWorkspaceHandlers(
            assembly,
            typeof(IExtensionWorkspaceMessageHandler<,>),
            (handler, handlerInterface) => new ExtensionWorkspaceMessageHandlerWrapper(handler, handlerInterface, extensionIdentifier),
            cancellationToken);

    private static ImmutableArray<IExtensionMessageHandlerWrapper<TArgument>> CreateWorkspaceHandlers<TArgument>(
        Assembly assembly,
        Type unboundInterfaceType,
        Func<object, Type, IExtensionMessageHandlerWrapper<TArgument>> wrapperCreator,
        CancellationToken cancellationToken)
    {
        var resultBuilder = ImmutableArray.CreateBuilder<IExtensionMessageHandlerWrapper<TArgument>>();

        foreach (var candidateType in assembly.GetTypes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (candidateType.IsAbstract || candidateType.IsGenericType)
                continue;

            Type? boundInterfaceType = null;
            foreach (var interfaceType in candidateType.GetInterfaces())
            {
                if (interfaceType.IsGenericType &&
                    !interfaceType.IsGenericTypeDefinition &&
                    interfaceType.GetGenericTypeDefinition() == unboundInterfaceType)
                {
                    if (boundInterfaceType is not null)
                        throw new InvalidOperationException(string.Format(ExternalAccessExtensionsResources.Type_0_implements_interface_1_more_than_once, candidateType.FullName, unboundInterfaceType.Name));

                    boundInterfaceType = interfaceType;
                }
            }

            if (boundInterfaceType == null)
                continue;

            var handler = Activator.CreateInstance(candidateType)
                ?? throw new InvalidOperationException(string.Format(ExternalAccessExtensionsResources.Cannot_create_0, candidateType.FullName));

            resultBuilder.Add(wrapperCreator(handler, boundInterfaceType));
        }

        return resultBuilder.ToImmutable();
    }
}
