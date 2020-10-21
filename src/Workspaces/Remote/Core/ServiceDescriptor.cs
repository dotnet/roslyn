// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Pipelines;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.ServiceHub.Framework;
using Nerdbank.Streams;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Describes Roslyn remote brokered service. 
    /// Adds Roslyn specific JSON converters and RPC settings to the default implementation.
    /// </summary>
    internal sealed class ServiceDescriptor : ServiceJsonRpcDescriptor
    {
        private static readonly JsonRpcTargetOptions s_jsonRpcTargetOptions = new JsonRpcTargetOptions()
        {
            // Do not allow JSON-RPC to automatically subscribe to events and remote their calls.
            NotifyClientOfEvents = false,

            // Only allow public methods (may be on internal types) to be invoked remotely.
            AllowNonPublicInvocation = false
        };

        // Enables remote APIs to pass Stream as parameter.
        private static readonly MultiplexingStream.Options s_multiplexingStreamOptions = new MultiplexingStream.Options
        {
            ProtocolMajorVersion = 3
        }.GetFrozenCopy();

        private ServiceDescriptor(ServiceMoniker serviceMoniker, Type? clientInterface)
            : base(serviceMoniker, clientInterface, Formatters.MessagePack, MessageDelimiters.BigEndianInt32LengthHeader, s_multiplexingStreamOptions)
        {
        }

        private ServiceDescriptor(ServiceDescriptor copyFrom)
          : base(copyFrom)
        {
        }

        public static ServiceDescriptor CreateRemoteServiceDescriptor(string serviceName, Type? clientInterface)
            => new ServiceDescriptor(new ServiceMoniker(serviceName), clientInterface);

        public static ServiceDescriptor CreateInProcServiceDescriptor(string serviceName)
            => new ServiceDescriptor(new ServiceMoniker(serviceName), clientInterface: null);

        protected override ServiceRpcDescriptor Clone()
            => new ServiceDescriptor(this);

        protected override IJsonRpcMessageFormatter CreateFormatter()
            => ConfigureFormatter((MessagePackFormatter)base.CreateFormatter());

        private static readonly MessagePackSerializerOptions s_options = StandardResolverAllowPrivate.Options
            .WithSecurity(MessagePackSecurity.UntrustedData.WithHashCollisionResistant(false))
            .WithResolver(CompositeResolver.Create(
                MessagePackFormatters.GetFormatters(),
                new IFormatterResolver[] { ImmutableCollectionMessagePackResolver.Instance, StandardResolverAllowPrivate.Instance }));

        private static MessagePackFormatter ConfigureFormatter(MessagePackFormatter formatter)
        {
            // See https://github.com/neuecc/messagepack-csharp.
            formatter.SetMessagePackSerializerOptions(s_options);
            return formatter;
        }

        protected override JsonRpcConnection CreateConnection(JsonRpc jsonRpc)
        {
            jsonRpc.CancelLocallyInvokedMethodsWhenConnectionIsClosed = true;
            var connection = base.CreateConnection(jsonRpc);
            connection.LocalRpcTargetOptions = s_jsonRpcTargetOptions;
            return connection;
        }

        internal static class TestAccessor
        {
            public static MessagePackSerializerOptions Options => s_options;
        }
    }
}
