// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Helper that avoids loading a <see cref="Type"/> by its full assembly-qualified name until needed.
/// </summary>
internal abstract partial class TypeRef : IEquatable<TypeRef>
{
    public static ITypeRefResolver DefaultResolver { get; } = new DefaultResolverImpl();

    private static readonly Dictionary<string, TypeRef> s_typeNameToLazyTypeMap = [];

    /// <summary>
    /// Returns the full assembly-qualified name of this type.
    /// </summary>
    public string TypeName { get; }

    private TypeRef(string typeName)
    {
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
    }

    /// <summary>
    /// Returns the underlying <see cref="Type"/>, potentially loading its assembly.
    /// </summary>
    public Type GetResolvedType(ITypeRefResolver? resolver = null)
        => GetResolvedTypeCore(resolver ?? DefaultResolver);

    protected abstract Type GetResolvedTypeCore(ITypeRefResolver resolver);

    [return: NotNullIfNotNull(nameof(typeName))]
    public static TypeRef? From(string? typeName)
    {
        if (typeName is null)
        {
            return null;
        }

        lock (s_typeNameToLazyTypeMap)
        {
            if (!s_typeNameToLazyTypeMap.TryGetValue(typeName, out var result))
            {
                result = new LazyTypeRef(typeName);
                s_typeNameToLazyTypeMap.Add(typeName, result);
            }

            return result;
        }
    }

    [return: NotNullIfNotNull(nameof(type))]
    public static TypeRef? From(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        lock (s_typeNameToLazyTypeMap)
        {
            var typeName = type.AssemblyQualifiedName!;

            if (!s_typeNameToLazyTypeMap.TryGetValue(typeName, out var result))
            {
                result = new ConcreteTypeRef(type);
                s_typeNameToLazyTypeMap.Add(typeName, result);
            }

            return result;
        }
    }

    public static TypeRef Of<T>() => From(typeof(T));

    public override bool Equals(object? obj)
        => Equals(obj as TypeRef);

    public bool Equals(TypeRef? other)
        => other is not null && TypeName == other.TypeName;

    public override int GetHashCode()
        => TypeName.GetHashCode();

    public override string ToString()
        => $"{{{nameof(TypeName)} = {TypeName}}}";

    public static bool operator ==(TypeRef? x, TypeRef? y)
        => EqualityComparer<TypeRef?>.Default.Equals(x, y);

    public static bool operator !=(TypeRef? x, TypeRef? y)
        => !(x == y);

    [return: NotNullIfNotNull(nameof(type))]
    public static implicit operator TypeRef?(Type? type)
        => type is not null ? From(type) : null;
}
