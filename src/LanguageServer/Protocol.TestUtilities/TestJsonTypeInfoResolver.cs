// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Roslyn.Test.Utilities;

/// <summary>
/// Allows synthetic test contracts without hiding missing generated product metadata.
/// </summary>
public sealed class TestJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    private static readonly ConcurrentDictionary<Assembly, byte> s_testAssemblies = new();
    private readonly DefaultJsonTypeInfoResolver _resolver = new();

    public TestJsonTypeInfoResolver(params Assembly[] testAssemblies)
    {
        foreach (var assembly in testAssemblies)
            RegisterTestAssembly(assembly);
    }

    public static void RegisterTestAssembly(Assembly assembly)
        => s_testAssemblies.TryAdd(assembly, 0);

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        => IsTestContract(type) ? _resolver.GetTypeInfo(type, options) : null;

    private static bool IsTestContract(Type type)
    {
        if (type.Assembly == typeof(TestJsonTypeInfoResolver).Assembly || s_testAssemblies.ContainsKey(type.Assembly))
            return true;

        if (type.HasElementType && IsTestContract(type.GetElementType()!))
            return true;

        foreach (var argument in type.GenericTypeArguments)
        {
            if (IsTestContract(argument))
                return true;
        }

        return false;
    }
}
