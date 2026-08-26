// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.StaticWebAssets;

/// <summary>
/// Reads the static web assets IntelliSense manifest the SDK emits, which lists the keys that
/// resolve through <c>ResourceAssetCollection</c> at runtime:
/// <code>
/// { "Version": 1, "Assets": [ "app.css", "_framework/blazor.web.js" ] }
/// </code>
/// </summary>
internal static class StaticWebAssetsManifestReader
{
    private const int SupportedVersion = 1;

    /// <summary>
    /// Returns the asset keys in the manifest, or an empty array if it cannot be understood. A
    /// manifest from a newer SDK, or one that is malformed because a build is midway through
    /// writing it, costs completions rather than failing the request that asked for them.
    /// </summary>
    public static ImmutableArray<string> Read(SourceText text)
    {
        try
        {
            return ReadCore(text);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static ImmutableArray<string> ReadCore(SourceText text)
    {
        var reader = new Utf8JsonReader(GetUtf8Bytes(text), isFinalBlock: true, state: default);

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            return [];
        }

        var version = 0;
        var assets = ImmutableArray<string>.Empty;

        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.ValueTextEquals("Version"))
            {
                reader.Read();
                version = reader.TokenType == JsonTokenType.Number ? reader.GetInt32() : 0;
            }
            else if (reader.ValueTextEquals("Assets"))
            {
                assets = ReadAssets(ref reader);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        return version == SupportedVersion ? assets : [];
    }

    private static ImmutableArray<string> ReadAssets(ref Utf8JsonReader reader)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            return [];
        }

        using var builder = new PooledArrayBuilder<string>();

        while (reader.Read() && reader.TokenType == JsonTokenType.String)
        {
            if (reader.GetString() is { Length: > 0 } asset)
            {
                builder.Add(asset);
            }
        }

        return builder.ToImmutable();
    }

    private static byte[] GetUtf8Bytes(SourceText text)
    {
        // The manifest is a few KB even for asset-heavy apps, so materializing it is cheaper than
        // the machinery needed to feed the reader in chunks.
        var content = text.ToString();
        var bytes = new byte[Encoding.UTF8.GetByteCount(content)];
        Encoding.UTF8.GetBytes(content, 0, content.Length, bytes, 0);
        return bytes;
    }
}
