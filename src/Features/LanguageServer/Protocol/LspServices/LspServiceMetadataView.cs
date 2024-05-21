// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

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
        TypeRef = TypeRef.From(typeName);

        var interfaceNames = (string[])metadata[nameof(AbstractExportLspServiceAttribute.InterfaceNames)];
        InterfaceNames = FrozenSet.ToFrozenSet(interfaceNames);

        ServerKind = (WellKnownLspServerKinds)metadata[nameof(AbstractExportLspServiceAttribute.ServerKind)];
        IsStateless = (bool)metadata[nameof(AbstractExportLspServiceAttribute.IsStateless)];

        var methodHandlerData = (string[]?)metadata[nameof(AbstractExportLspServiceAttribute.MethodHandlerData)];

        if (methodHandlerData is not null)
        {
            Contract.ThrowIfFalse(methodHandlerData.Length % 5 == 0);

            var total = methodHandlerData.Length / 5;

            var handlerDetails = new MethodHandlerDetails[total];

            var index = 0;
            for (var i = 0; i < total; i++)
            {
                var methodName = methodHandlerData[index++];
                var language = methodHandlerData[index++];
                var requestTypeName = methodHandlerData[index++];
                var responseTypeName = methodHandlerData[index++];
                var requestContextTypeName = methodHandlerData[index++];

                handlerDetails[i] = new(
                    methodName,
                    language,
                    requestTypeName is not null ? TypeRef.From(requestTypeName) : null,
                    responseTypeName is not null ? TypeRef.From(responseTypeName) : null,
                    TypeRef.From(requestContextTypeName));
            }

            HandlerDetails = ImmutableCollectionsMarshal.AsImmutableArray(handlerDetails);
        }
        else
        {
            HandlerDetails = null;
        }
    }
}
