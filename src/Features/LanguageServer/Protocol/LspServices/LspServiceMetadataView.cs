// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LspServiceMetadataView
{
    public Type Type { get; set; }

    public WellKnownLspServerKinds ServerKind { get; set; }

    public bool IsStateless { get; set; }

    public LspServiceMetadataView(IDictionary<string, object> metadata)
    {
        var handlerMetadata = (Type)metadata[nameof(Type)];
        Type = handlerMetadata;

        ServerKind = (WellKnownLspServerKinds)metadata[nameof(ServerKind)];
        IsStateless = (bool)metadata[nameof(IsStateless)];
    }

    public LspServiceMetadataView(Type type)
    {
        Type = type;
        ServerKind = WellKnownLspServerKinds.Any;
        IsStateless = false;
    }
}
