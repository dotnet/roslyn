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
internal sealed partial record class TypeRef
{
    private string? _assemblyQualifiedName;

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
    public string AssemblyQualifiedName
        => _assemblyQualifiedName ??= $"{TypeName}, {AssemblyName}";

    /// <summary>
    /// Constructs a <see cref="TypeRef"/> instance.
    /// </summary>
    /// <param name="typeName">The full name of this type.</param>
    /// <param name="assemblyName">The full name of the assembly containing this type.</param>
    /// <param name="codeBase">The code base of the assembly containing this type, if any.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public TypeRef(string typeName, string assemblyName, string? codeBase = null)
    {
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        CodeBase = codeBase;
    }

    public override string ToString() => TypeName;

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

        return new(typeName, assemblyName, codeBase);
    }

    public static TypeRef? FromOrNull(Type? type)
        => type is not null ? From(type) : null;

    public static TypeRef Of<T>() => From(typeof(T));
}
