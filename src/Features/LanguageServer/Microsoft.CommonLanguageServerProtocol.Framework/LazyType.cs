// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Helper that avoids loading a <see cref="Type"/> by its full assembly-qualified name until needed.
/// </summary>
internal sealed record LazyType
{
    private static readonly Dictionary<string, LazyType> s_typeNameToLazyTypeMap = [];

    /// <summary>
    /// Returns the full assembly-qualified name of this type.
    /// </summary>
    public string TypeName { get; init; }

    // May be a Lazy<Type> or Type.
    private readonly object _value;

    /// <summary>
    /// Returns the underlying <see cref="Type"/>, potentially loading its assembly.
    /// </summary>
    public Type Value => _value is Lazy<Type> lazyType
        ? lazyType.Value
        : (Type)_value;

    private LazyType(string typeName, object value)
    {
        TypeName = typeName;
        _value = value;
    }

    private static LazyType GetOrCreate(string typeName, Func<string, LazyType> creator)
    {
        lock (s_typeNameToLazyTypeMap)
        {
            if (!s_typeNameToLazyTypeMap.TryGetValue(typeName, out var result))
            {
                result = creator(typeName);
                s_typeNameToLazyTypeMap.Add(typeName, result);
            }

            return result;
        }
    }

    private static LazyType GetOrCreate(Type type, Func<string, Type, LazyType> creator)
    {
        lock (s_typeNameToLazyTypeMap)
        {
            var typeName = type.AssemblyQualifiedName!;

            if (!s_typeNameToLazyTypeMap.TryGetValue(typeName, out var result))
            {
                result = creator(typeName, type);
                s_typeNameToLazyTypeMap.Add(typeName, result);
            }

            return result;
        }
    }

    public static LazyType From(string typeName)
    {
        return GetOrCreate(typeName, static typeName => new(typeName, new Lazy<Type>(LoadType(typeName))));

        static Func<Type> LoadType(string typeName)
            => () => Type.GetType(typeName)
                  ?? throw new InvalidOperationException($"Could not load type: '{typeName}'");
    }

    public static LazyType? FromOrNull(string? typeName)
        => typeName is not null ? From(typeName) : null;

    public static LazyType From(Type type)
        => GetOrCreate(type, static (typeName, type) => new(typeName, type));

    public static LazyType Of<T>() => From(typeof(T));

    public bool Equals(LazyType? other)
        => other is not null && TypeName == other.TypeName;

    public override int GetHashCode()
        => TypeName.GetHashCode();

    public override string ToString()
        => $"{{{nameof(TypeName)} = {TypeName}}}";

    public static implicit operator LazyType?(Type? type)
        => type is not null ? From(type) : null;
}
