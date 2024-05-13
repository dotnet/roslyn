// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

internal sealed record LazyType
{
    public string TypeName { get; init; }

    private readonly Lazy<Type> _lazyValue;

    public Type Value => _lazyValue.Value;

    public LazyType(string typeName)
    {
        TypeName = typeName;
        _lazyValue = new(LoadType(typeName));
    }

    public LazyType(Type type)
    {
        TypeName = type.AssemblyQualifiedName!;
        _lazyValue = new(() => type);
    }

    private static Func<Type> LoadType(string typeName)
        => () => Type.GetType(typeName)
              ?? throw new InvalidOperationException($"Could not load type: '{typeName}'");

    public bool Equals(LazyType? other)
        => other is not null && TypeName == other.TypeName;

    public override int GetHashCode()
        => TypeName.GetHashCode();

    public override string ToString()
        => $"{{{nameof(TypeName)} = {TypeName}}}";
}
