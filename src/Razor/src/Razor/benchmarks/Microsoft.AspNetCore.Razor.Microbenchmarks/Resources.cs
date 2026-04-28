// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

internal static class Resources
{
    private static readonly Dictionary<(string Name, string? Folder), string> s_textMap = new();
    private static readonly Dictionary<(string Name, string? Folder), byte[]> s_bytesMap = new();

    private static string GetResourceName(string name, string? folder)
        => folder is not null
            ? $"{typeof(Resources).Namespace}.Resources.{folder}.{name}"
            : $"{typeof(Resources).Namespace}.Resources.{name}";

    private static Stream GetResourceStream(string name, string? folder = null)
    {
        var resourceName = GetResourceName(name, folder);

        return Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find resource: {resourceName}");
    }

    public static string GetResourceText(string name, string? folder = null)
    {
        lock (s_textMap)
        {
            var key = (name, folder);

            if (s_textMap.TryGetValue(key, out var value))
            {
                return value;
            }

            using var stream = GetResourceStream(name, folder);
            using var reader = new StreamReader(stream);

            value = reader.ReadToEnd();

            s_textMap.Add(key, value);

            return value;
        }
    }

    public static byte[] GetResourceBytes(string name, string? folder = null)
    {
        lock (s_bytesMap)
        {
            var key = (name, folder);

            if (s_bytesMap.TryGetValue(key, out var value))
            {
                return value;
            }

            using var stream = GetResourceStream(name, folder);

            value = new byte[stream.Length];
#if NET
            stream.ReadExactly(value);
#else
            stream.Read(value, 0, value.Length);
#endif

            s_bytesMap.Add(key, value);

            return value;
        }
    }
}
