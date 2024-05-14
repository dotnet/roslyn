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
    public ImmutableArray<MethodHandlerDescriptor>? MethodHandlers { get; }

    public LspServiceMetadataView(IDictionary<string, object> metadata)
    {
        var typeName = (string)metadata["TypeName"];
        TypeRef = TypeRef.From(typeName);

        ServerKind = (WellKnownLspServerKinds)metadata[nameof(ServerKind)];
        IsStateless = (bool)metadata[nameof(IsStateless)];

        var methodHandlerDescriptorData = (byte[]?)metadata["MethodHandlerDescriptorData"];

        MethodHandlers = methodHandlerDescriptorData is not null
            ? MefSerialization.DeserializeMethodHandlers(methodHandlerDescriptorData)
            : null;
    }
}
