// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Language;

internal readonly struct TypeNameObject
{
    private readonly struct TypeNameInfo(string fullName, string? @namespace, string? name, string? alias)
    {
        public string FullName { get; } = fullName;
        public string? Namespace { get; } = @namespace;
        public string? Name { get; } = name;
        public string? Alias { get; } = alias;
    }

    private static readonly ImmutableArray<TypeNameInfo> s_knownTypeNames;
    private static readonly FrozenDictionary<string, byte> s_typeNameToIndex;

    private static readonly int s_booleanIndex;
    private static readonly int s_stringIndex;

    static TypeNameObject()
    {
        var knownTypeNames = ImmutableArray.CreateBuilder<TypeNameInfo>();
        var typeNameToIndex = new Dictionary<string, byte>(StringComparer.Ordinal);

        Add<object>("object");
        s_booleanIndex = Add<bool>("bool");
        Add<int>("int");
        Add<long>("long");
        Add<short>("short");
        Add<byte>("byte");
        Add<sbyte>("sbyte");
        Add<uint>("uint");
        Add<ulong>("ulong");
        Add<ushort>("ushort");
        Add<float>("float");
        Add<double>("double");
        Add<decimal>("decimal");
        Add<char>("char");
        s_stringIndex = Add<string>("string");
        Add<System.Globalization.CultureInfo>();
        Add<Delegate>();
        Add<Type>();

        // Add any additional types here.

        s_knownTypeNames = knownTypeNames.ToImmutable();
        s_typeNameToIndex = typeNameToIndex.ToFrozenDictionary(StringComparer.Ordinal);

        int Add<T>(string? alias = null)
        {
            Debug.Assert(knownTypeNames.Count < byte.MaxValue, "Too many known type names to fit in a byte index.");

            var fullName = typeof(T).FullName!;
            var @namespace = typeof(T).Namespace;
            var name = typeof(T).Name;

            var index = (byte)knownTypeNames.Count;
            knownTypeNames.Add(new(fullName, @namespace, name, alias));
            typeNameToIndex.Add(fullName, index);

            if (alias is not null)
            {
                typeNameToIndex.Add(alias, index);
            }

            return index;
        }
    }

    private readonly byte? _index;
    private readonly TypeNameInfo? _info;

    public TypeNameObject(byte index)
    {
        Debug.Assert(index >= 0 && index < s_knownTypeNames.Length);

        _index = index;
        _info = null;
    }

    public TypeNameObject(string? stringValue)
    {
        Debug.Assert(stringValue is null || !s_typeNameToIndex.ContainsKey(stringValue));

        _index = null;
        _info = new(stringValue!, @namespace: null, name: null, alias: null);
    }

    private TypeNameObject(TypeNameInfo info, byte? index)
    {
        _index = index;
        _info = info;
    }

    public bool IsNull => _index is null && _info is null;

    public byte? Index => _index;

    public static TypeNameObject From(string? fullName)
    {
        if (fullName is null)
        {
            return default;
        }

        return From(fullName, namespaceName: null, name: null);
    }

    public static TypeNameObject From(string fullName, string? namespaceName, string? name)
    {
        if (s_typeNameToIndex.TryGetValue(fullName, out var index))
        {
            var info = s_knownTypeNames[index];
            return new(info, index);
        }

        return new(new(fullName, namespaceName, name, alias: null), index: null);
    }

    public static TypeNameObject From<T>()
        => From(typeof(T));

    public static TypeNameObject From(Type type)
    {
        var fullName = type.FullName!;

        if (s_typeNameToIndex.TryGetValue(fullName, out var index))
        {
            var info = s_knownTypeNames[index];
            return new(info, index);
        }

        var @namespace = type.Namespace;
        var name = type.Name;

        return new(new(fullName, @namespace, name, alias: null), index: null);
    }

    public static TypeNameObject From(INamedTypeSymbol namedTypeSymbol)
    {
        var fullName = namedTypeSymbol.GetFullName();

        if (s_typeNameToIndex.TryGetValue(fullName, out var index))
        {
            var info = s_knownTypeNames[index];
            return new(info, index);
        }

        var @namespace = namedTypeSymbol.ContainingNamespace.GetFullName();
        var name = namedTypeSymbol.Name;

        return new(new(fullName, @namespace, name, alias: null), index: null);
    }

    public bool IsBoolean => _index == s_booleanIndex;
    public bool IsString => _index == s_stringIndex;

    public readonly string? FullName
    {
        get
        {
            if (_info is { FullName: var fullName })
            {
                return fullName;
            }

            if (_index is byte index)
            {
                return s_knownTypeNames[index].FullName;
            }

            return null;
        }
    }

    public readonly string? Namespace
    {
        get
        {
            if (_info is { Namespace: var @namespace })
            {
                return @namespace;
            }

            if (_index is byte index)
            {
                return s_knownTypeNames[index].Namespace;
            }

            return null;
        }
    }

    public readonly string? Name
    {
        get
        {
            if (_info is { Name: var name })
            {
                return name;
            }

            if (_index is byte index)
            {
                return s_knownTypeNames[index].Name;
            }

            return null;
        }
    }

    public void AppendToChecksum(in Checksum.Builder builder)
    {
        if (_index is byte index)
        {
            builder.Append(index);
        }
        else if (_info is TypeNameInfo info)
        {
            builder.Append(info.FullName);
            builder.Append(info.Namespace);
            builder.Append(info.Name);
        }
        else
        {
            builder.AppendNull();
        }
    }
}
