// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Helper that avoids loading a <see cref="Type"/> by its full assembly-qualified name until needed.
/// </summary>
internal readonly partial record struct TypeRef
{
    public static AbstractTypeRefResolver DefaultResolver { get; } = new DefaultResolverImpl();

    /// <summary>
    /// Returns the full assembly-qualified name of this type.
    /// </summary>
    public string TypeName { get; }

    public bool IsDefault => TypeName is null;

    private TypeRef(string typeName)
    {
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
    }

    public override string ToString()
        => IsDefault ? "<DEFAULT>" : TypeName;

    public static TypeRef From(string typeName) => new(typeName);
    public static TypeRef From(Type type) => new(type.AssemblyQualifiedName!);

    public static TypeRef Of<T>() => From(typeof(T));
}
