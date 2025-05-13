// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.ServiceHub.Framework;
using Nerdbank.Streams;
using Newtonsoft.Json;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Wraps MessagePack or JSON serialization options/converters.
/// </summary>
internal readonly struct RemoteSerializationOptions
{
    internal static readonly RemoteSerializationOptions Default = new([], []);

    // Enables remote APIs to pass Stream as parameter.
    private static readonly MultiplexingStream.Options s_multiplexingStreamOptions = new MultiplexingStream.Options
    {
        ProtocolMajorVersion = 3
    }.GetFrozenCopy();

    private readonly object _options;

    public RemoteSerializationOptions(ImmutableArray<IMessagePackFormatter> additionalFormatters, ImmutableArray<IFormatterResolver> additionalResolvers)
        => _options = StandardResolverAllowPrivate.Options
            .WithSecurity(MessagePackSecurity.UntrustedData.WithHashCollisionResistant(false))
            .WithResolver(MessagePackFormatters.CreateResolver(additionalFormatters, additionalResolvers));

    public RemoteSerializationOptions(ImmutableArray<JsonConverter> jsonConverters)
        => _options = jsonConverters;

    public RemoteSerializationOptions(ImmutableArray<System.Text.Json.Serialization.JsonConverter> jsonConverters)
        => _options = jsonConverters;

    public MessagePackSerializerOptions MessagePackOptions => (MessagePackSerializerOptions)_options;
    public ImmutableArray<JsonConverter> JsonConverters => (ImmutableArray<JsonConverter>)_options;
    public ImmutableArray<System.Text.Json.Serialization.JsonConverter> SystemTextJsonConverters => (ImmutableArray<System.Text.Json.Serialization.JsonConverter>)_options;

    public ServiceJsonRpcDescriptor.Formatters Formatter
        => _options switch
        {
            MessagePackSerializerOptions => ServiceJsonRpcDescriptor.Formatters.MessagePack,
            ImmutableArray<JsonConverter> => ServiceJsonRpcDescriptor.Formatters.UTF8,
            ImmutableArray<System.Text.Json.Serialization.JsonConverter> => ServiceJsonRpcDescriptor.Formatters.UTF8SystemTextJson,
            _ => throw new InvalidOperationException()
        };

    public ServiceJsonRpcDescriptor.MessageDelimiters MessageDelimiters
       => _options is MessagePackSerializerOptions
           ? ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader
           : ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders;

    public MultiplexingStream.Options? MultiplexingStreamOptions
        => _options is MessagePackSerializerOptions ? s_multiplexingStreamOptions : null;

    internal IJsonRpcMessageFormatter ConfigureFormatter(IJsonRpcMessageFormatter formatter)
    {
        if (formatter is MessagePackFormatter messagePackFormatter)
        {
            // See https://github.com/neuecc/messagepack-csharp.
            messagePackFormatter.SetMessagePackSerializerOptions(MessagePackOptions);
        }
        else if (formatter is SystemTextJsonFormatter stjFormatter)
        {
            var converters = stjFormatter.JsonSerializerOptions.Converters;

            foreach (var converter in SystemTextJsonConverters)
            {
                converters.Add(converter);
            }
        }
        else
        {
            var converters = ((JsonMessageFormatter)formatter).JsonSerializer.Converters;

            foreach (var converter in JsonConverters)
            {
                converters.Add(converter);
            }
        }

        return formatter;
    }
}
