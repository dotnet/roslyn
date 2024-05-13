// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Provides helpers for serializing data to/from MEF metadata.
/// </summary>
internal static class MefSerialization
{
    public static byte[] Serialize(ImmutableArray<MethodHandlerDescriptor> descriptors)
    {
        using var stream = SerializableBytes.CreateWritableStream();
        using (var writer = new ObjectWriter(stream, leaveOpen: true))
        {
            writer.WriteArray(descriptors, (writer, value) =>
            {
                writer.WriteString(value.MethodName);
                writer.WriteString(value.Language);
                writer.WriteString(value.RequestTypeName);
                writer.WriteString(value.ResponseTypeName);
                writer.WriteString(value.RequestContextTypeName);
            });
        }

        stream.Position = 0;

        return stream.ToArray();
    }

    public static ImmutableArray<MethodHandlerDescriptor> DeserializeMethodHandlers(byte[] bytes)
    {
        using var stream = SerializableBytes.CreateReadableStream(bytes);
        using var reader = ObjectReader.TryGetReader(stream);

        if (reader == null)
        {
            return [];
        }

        return reader.ReadArray<MethodHandlerDescriptor>(reader =>
        {
            var methodName = reader.ReadRequiredString();
            var language = reader.ReadRequiredString();
            var requestTypeName = reader.ReadString();
            var responseTypeName = reader.ReadString();
            var requestContextTypeName = reader.ReadRequiredString();

            return new(methodName, language, requestTypeName, responseTypeName, requestContextTypeName);
        });
    }
}
