// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Concurrent;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Helper that avoids loading a <see cref="Type"/> by its full assembly-qualified name until needed.
/// </summary>
internal sealed partial class TypeRef : IEquatable<TypeRef>
{
    private static readonly ConcurrentDictionary<(string TypeName, string AssemblyName, string? CodeBase), TypeRef> s_cache = [];

    private TypeRef(string typeName, string assemblyName, string? codeBase = null)
    {
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        CodeBase = codeBase;
    }

    /// <summary>
    /// Returns the full name of this type.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Returns the full name of the assembly containing this type.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// Returns the code base of the assembly containing this type, if any.
    /// </summary>
    public string? CodeBase { get; }

    /// <summary>
    /// Returns the assembly-qualified name of this type.
    /// </summary>
    public string AssemblyQualifiedName { get => field ??= $"{TypeName}, {AssemblyName}"; private set; }

    public override bool Equals(object? obj)
        => Equals(obj as TypeRef);

    public bool Equals(TypeRef? other)
        => other is not null &&
           TypeName == other.TypeName &&
           AssemblyName == other.AssemblyName &&
           CodeBase == other.CodeBase;

    public override int GetHashCode()
    {
        var comparer = StringComparer.Ordinal;

        var hashCode = 2037759866;
        unchecked
        {
            hashCode = hashCode * -1521134295 + comparer.GetHashCode(TypeName);
            hashCode = hashCode * -1521134295 + comparer.GetHashCode(AssemblyName);

            if (CodeBase is string codeBase)
            {
                hashCode = hashCode * -1521134295 + comparer.GetHashCode(codeBase);
            }
        }
        return hashCode;
    }

    public override string ToString() => TypeName;

    /// <summary>
    /// Constructs a <see cref="TypeRef"/> instance.
    /// </summary>
    /// <param name="typeName">The full name of this type.</param>
    /// <param name="assemblyName">The full name of the assembly containing this type.</param>
    /// <param name="codeBase">The code base of the assembly containing this type, if any.</param>
    public static TypeRef From(string typeName, string assemblyName, string? codeBase)
    {
        var key = (typeName, assemblyName, codeBase);

        if (s_cache.TryGetValue(key, out var result))
        {
            return result;
        }

        return s_cache.GetOrAdd(key, new TypeRef(typeName, assemblyName, codeBase));
    }

    /// <summary>
    /// Constructs a <see cref="TypeRef"/> from a <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to use.</param>
    public static TypeRef From(Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var typeName = type.FullName ?? throw new ArgumentException($"{nameof(type)} has null {nameof(type.FullName)} property.", nameof(type));
        var assemblyName = type.Assembly.FullName ?? throw new ArgumentException($"{nameof(type)} has null {nameof(type.Assembly)}.{nameof(type.Assembly.FullName)} property.", nameof(type));

#pragma warning disable SYSLIB0012 // Type or member is obsolete
        var codeBase = type.Assembly.CodeBase;
#pragma warning restore SYSLIB0012 // Type or member is obsolete

        return From(typeName, assemblyName, codeBase);
    }

    /// <summary>
    /// Constructs a <see cref="TypeRef"/> from a <see cref="Type"/> or returns <see langword="null"/>
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to use, or <see langword="null"/>.</param>
    public static TypeRef? FromOrNull(Type? type)
        => type is not null ? From(type) : null;

    public static TypeRef Of<T>() => From(typeof(T));
}
