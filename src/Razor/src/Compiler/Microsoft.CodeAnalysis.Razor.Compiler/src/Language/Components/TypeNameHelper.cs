// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class TypeNameHelper
{
    private const string GlobalPrefix = "global::";

    private static readonly ImmutableHashSet<ReadOnlyMemory<char>> PredefinedTypeNames = new[]
    {
        "bool".AsMemory(),
        "int".AsMemory(),
        "string".AsMemory(),
        "float".AsMemory(),
        "double".AsMemory(),
        "decimal".AsMemory(),
        "byte".AsMemory(),
        "short".AsMemory(),
        "long".AsMemory(),
        "char".AsMemory(),
        "object".AsMemory(),
        "dynamic".AsMemory(),
        "uint".AsMemory(),
        "ushort".AsMemory(),
        "ulong".AsMemory(),
        "sbyte".AsMemory(),
        "nint".AsMemory(),
        "nuint".AsMemory(),
    }.ToImmutableHashSet(NameComparer.Instance);

    internal static string GetGloballyQualifiedNameIfNeeded(string typeName)
    {
        if (typeName.Length == 0)
        {
            return typeName;
        }

        if (typeName.StartsWith(GlobalPrefix, StringComparison.Ordinal))
        {
            return typeName;
        }

        // Mitigation for https://github.com/dotnet/razor-compiler/issues/332. When we add a reference to Roslyn
        // at this layer, we can do this property by using ParseTypeName and then rewriting the tree. For now, we
        // just skip prefixing tuples.
        if (typeName[0] == '(')
        {
            return typeName;
        }

        // Fast path, if the length doesn't fall within that of the
        // builtin c# types, then we can add global without further checks.
        if (typeName.Length is < 3 or > 7)
        {
            return GlobalPrefix + typeName;
        }

        if (PredefinedTypeNames.Contains(typeName.AsMemory()))
        {
            return typeName;
        }

        return GlobalPrefix + typeName;
    }

    public static void WriteGloballyQualifiedName(CodeWriter codeWriter, string typeName)
    {
        if (typeName == null)
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        WriteGloballyQualifiedName(codeWriter, typeName.AsMemory());
    }

    internal static void WriteGloballyQualifiedName(CodeWriter codeWriter, ReadOnlyMemory<char> typeName)
    {
        WriteGlobalPrefixIfNeeded(codeWriter, typeName);
        codeWriter.Write(typeName);
    }

    /// <summary>
    /// Writes "global::" if the typename doesn't already start with it and isn't a predefined type.
    /// </summary>
    internal static void WriteGlobalPrefixIfNeeded(CodeWriter codeWriter, ReadOnlyMemory<char> typeName)
    {
        if (typeName.Length == 0)
        {
            return;
        }

        var typeNameSpan = typeName.Span;

        if (typeNameSpan.StartsWith(GlobalPrefix.AsSpan(), StringComparison.Ordinal))
        {
            return;
        }

        // Mitigation for https://github.com/dotnet/razor-compiler/issues/332. When we add a reference to Roslyn
        // at this layer, we can do this property by using ParseTypeName and then rewriting the tree. For now, we
        // just skip prefixing tuples.
        if (typeNameSpan[0] == '(')
        {
            return;
        }

        // Fast path, if the length doesn't fall within that of the
        // builtin c# types, then we can add global without further checks.
        if (typeNameSpan.Length < 3 || typeNameSpan.Length > 7)
        {
            codeWriter.Write(GlobalPrefix);
            return;
        }

        if (PredefinedTypeNames.Contains(typeName))
        {
            return;
        }

        codeWriter.Write(GlobalPrefix);
    }

    internal static ReadOnlyMemory<char> GetNonGenericTypeName(string typeName, out ReadOnlyMemory<char> genericTypeParameterList)
    {
        var memory = typeName.AsMemory();
        var index = memory.Span.IndexOf('<');

        genericTypeParameterList = index == -1
            ? default
            : memory[index..];
        return index == -1 ? memory : memory[..index];
    }
}
