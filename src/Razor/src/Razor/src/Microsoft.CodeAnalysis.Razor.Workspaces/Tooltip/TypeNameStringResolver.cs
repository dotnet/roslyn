// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

internal static class TypeNameStringResolver
{
    private static readonly Dictionary<string, string> s_primitiveDisplayTypeNameLookups = new(StringComparer.Ordinal)
    {
        // These null suppressions are required for the VMR, which builds using v5.0 of Roslyn
        [typeof(byte).FullName!] = "byte",
        [typeof(sbyte).FullName!] = "sbyte",
        [typeof(int).FullName!] = "int",
        [typeof(uint).FullName!] = "uint",
        [typeof(short).FullName!] = "short",
        [typeof(ushort).FullName!] = "ushort",
        [typeof(long).FullName!] = "long",
        [typeof(ulong).FullName!] = "ulong",
        [typeof(float).FullName!] = "float",
        [typeof(double).FullName!] = "double",
        [typeof(char).FullName!] = "char",
        [typeof(bool).FullName!] = "bool",
        [typeof(object).FullName!] = "object",
        [typeof(string).FullName!] = "string",
        [typeof(decimal).FullName!] = "decimal",
    };

    public static bool TryGetSimpleName(string typeName, [NotNullWhen(returnValue: true)] out string? resolvedName)
    {
        if (typeName is null)
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        if (s_primitiveDisplayTypeNameLookups.TryGetValue(typeName, out var simpleName))
        {
            resolvedName = simpleName;
            return true;
        }

        resolvedName = null;
        return false;
    }
}
