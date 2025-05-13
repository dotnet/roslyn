// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LspServiceMetadataView
{
    public TypeRef TypeRef { get; }
    public FrozenSet<string> InterfaceNames { get; }
    public WellKnownLspServerKinds ServerKind { get; }
    public bool IsStateless { get; }

    public bool IsMethodHandler => HandlerDetails is not null;

    /// <summary>
    /// Returns an array of request handler method details if this services is an <see cref="IMethodHandler"/>.
    /// </summary>
    public ImmutableArray<MethodHandlerDetails>? HandlerDetails { get; }

    public LspServiceMetadataView(IDictionary<string, object> metadata)
    {
        var typeName = (string)metadata[nameof(AbstractExportLspServiceAttribute.TypeName)];
        var assemblyName = (string)metadata[nameof(AbstractExportLspServiceAttribute.AssemblyName)];
        var codeBase = (string?)metadata[nameof(AbstractExportLspServiceAttribute.CodeBase)];
        TypeRef = TypeRef.From(typeName, assemblyName, codeBase);

        var interfaceNames = (string[])metadata[nameof(AbstractExportLspServiceAttribute.InterfaceNames)];
        InterfaceNames = FrozenSet.ToFrozenSet(interfaceNames);

        ServerKind = (WellKnownLspServerKinds)metadata[nameof(AbstractExportLspServiceAttribute.ServerKind)];
        IsStateless = (bool)metadata[nameof(AbstractExportLspServiceAttribute.IsStateless)];

        var methodHandlerData = (string[]?)metadata[nameof(AbstractExportLspServiceAttribute.MethodHandlerData)];

        if (methodHandlerData is not null)
        {
            using var _ = ArrayBuilder<MethodHandlerDetails>.GetInstance(out var handlerDetails);

            var index = 0;
            while (index < methodHandlerData.Length)
            {
                var methodName = methodHandlerData[index++];
                var language = methodHandlerData[index++];
                var requestTypeRef = ReadTypeRef(methodHandlerData, ref index);
                var responseTypeRef = ReadTypeRef(methodHandlerData, ref index);
                var requestContextTypeRef = ReadTypeRef(methodHandlerData, ref index);
                Contract.ThrowIfNull(requestContextTypeRef);

                handlerDetails.Add(new(
                    methodName,
                    language,
                    requestTypeRef,
                    responseTypeRef,
                    requestContextTypeRef));
            }

            HandlerDetails = handlerDetails.ToImmutableAndClear();
        }
        else
        {
            HandlerDetails = null;
        }

        static TypeRef? ReadTypeRef(string?[] methodHandlerData, ref int index)
        {
            var typeName = methodHandlerData[index++];

            if (typeName is null)
            {
                return null;
            }

            var assemblyName = methodHandlerData[index++];
            Contract.ThrowIfNull(assemblyName);

            var codeBase = methodHandlerData[index++];

            return TypeRef.From(typeName, assemblyName, codeBase);
        }
    }
}
