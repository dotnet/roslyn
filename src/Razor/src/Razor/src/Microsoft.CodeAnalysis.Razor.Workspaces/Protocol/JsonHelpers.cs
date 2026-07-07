// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

internal static class JsonHelpers
{
    /// <summary>
    /// Serializer options to use when serializing or deserializing a Roslyn LSP type
    /// </summary>
    internal static JsonSerializerOptions JsonSerializerOptions => ProtocolConversions.LspJsonSerializerOptions;

    /// <summary>
    /// Converts an LSP object to a different LSP object, either by casting or serializing and deserializing
    /// </summary>
    internal static TResult? Convert<TSource, TResult>(TSource? source)
    {
        if (source is TResult result)
        {
            return result;
        }

        return JsonSerializer.Deserialize<TResult>(JsonSerializer.SerializeToDocument(source, JsonSerializerOptions), JsonSerializerOptions);
    }

    /// <summary>
    /// Converts an array of LSP objects to a different LSP object, either by casting or serializing and deserializing
    /// </summary>
    internal static TResult[] ConvertAll<TSource, TResult>(TSource[] source)
    {
        using var results = new PooledArrayBuilder<TResult>(source.Length);
        foreach (var item in source)
        {
            if (Convert<TSource, TResult>(item) is { } converted)
            {
                results.Add(converted);
            }
            else
            {
                Debug.Fail("Could not convert item to expected type.");
            }
        }

        return results.ToArray();
    }
}
