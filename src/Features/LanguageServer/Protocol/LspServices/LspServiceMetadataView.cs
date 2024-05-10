// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LspServiceMetadataView
{
    private Type? _type;

    public Type Type
    {
        get
        {
            return _type ??= InterlockedOperations.Initialize(ref _type, LoadType, AssemblyQualifiedName);

            static Type LoadType(string assemblyQualifiedName)
            {
                return Type.GetType(assemblyQualifiedName)
                    ?? throw new InvalidOperationException($"Could not load type: '{assemblyQualifiedName}'");
            }
        }
    }

    public string AssemblyQualifiedName { get; set; }

    public WellKnownLspServerKinds ServerKind { get; set; }

    public bool IsStateless { get; set; }

    public LspServiceMetadataView(IDictionary<string, object> metadata)
    {
        AssemblyQualifiedName = (string)metadata[nameof(AssemblyQualifiedName)];
        ServerKind = (WellKnownLspServerKinds)metadata[nameof(ServerKind)];
        IsStateless = (bool)metadata[nameof(IsStateless)];
    }

    public LspServiceMetadataView(Type type)
    {
        Contract.ThrowIfNull(type.FullName);

        _type = type;
        AssemblyQualifiedName = type.FullName;
        ServerKind = WellKnownLspServerKinds.Any;
        IsStateless = false;
    }
}
