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
internal readonly partial record struct TypeRef
{
    public static AbstractTypeRefResolver DefaultResolver { get; } = new DefaultResolverImpl();

    /// <summary>
    /// Returns the fully-qualified name of this type.
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

    public bool IsDefault => this == default;

    private TypeRef(string typeName, string assemblyName, string? codeBase)
    {
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        CodeBase = codeBase;
    }

    public bool Equals(TypeRef other)
    {
        return TypeName == other.TypeName
            && AssemblyName == other.AssemblyName
            && CodeBase == other.CodeBase;
    }

    public override int GetHashCode()
    {
        var hashCode = -201320956;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TypeName);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AssemblyName);

        if (CodeBase is string codeBase)
        {
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(codeBase);
        }

        return hashCode;
    }

    public override string ToString()
        => IsDefault ? "<DEFAULT>" : TypeName;

    public static TypeRef From(string typeName, string assemblyName, string? codeBase)
        => new(typeName, assemblyName, codeBase);

    public static TypeRef From(Type type) => new(
        type.FullName!,
        type.Assembly.FullName!,
#pragma warning disable SYSLIB0012 // Type or member is obsolete
        type.Assembly.CodeBase);
#pragma warning restore SYSLIB0012 // Type or member is obsolete

    public static TypeRef Of<T>() => From(typeof(T));
}
