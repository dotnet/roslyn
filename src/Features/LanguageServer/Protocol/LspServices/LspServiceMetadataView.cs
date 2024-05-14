// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LspServiceMetadataView
{
    public TypeRef TypeRef { get; }
    public WellKnownLspServerKinds ServerKind { get; }
    public bool IsStateless { get; }

    /// <summary>
    /// Returns an array of request handler method details if this services is an <see cref="IMethodHandler"/>.
    /// </summary>
    public ImmutableArray<HandlerMethodDetails>? HandlerMethods { get; }

    public LspServiceMetadataView(IDictionary<string, object> metadata)
    {
        var typeName = (string)metadata[nameof(AbstractExportLspServiceAttribute.TypeName)];
        TypeRef = TypeRef.From(typeName);

        ServerKind = (WellKnownLspServerKinds)metadata[nameof(ServerKind)];
        IsStateless = (bool)metadata[nameof(IsStateless)];

        var handlerMethodData = (byte[]?)metadata[nameof(AbstractExportLspServiceAttribute.HandlerMethodData)];

        HandlerMethods = handlerMethodData is not null
            ? MefSerialization.DeserializeHandlerMethods(handlerMethodData)
            : null;
    }
}
