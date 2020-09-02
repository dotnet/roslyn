// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.ServiceHub.Framework;
using Nerdbank.Streams;
using Newtonsoft.Json;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class ServiceDescriptor : ServiceJsonRpcDescriptor
    {
        // Enables remote APIs to pass Stream as parameter.
        private static readonly MultiplexingStream.Options s_multiplexingStreamOptions = new MultiplexingStream.Options
        {
            ProtocolMajorVersion = 3
        }.GetFrozenCopy();

        private ServiceDescriptor(ServiceMoniker serviceMoniker, Type? clientInterface)
            : base(serviceMoniker, clientInterface, Formatters.UTF8, MessageDelimiters.HttpLikeHeaders, s_multiplexingStreamOptions)
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
            => ConfigureFormatter((JsonMessageFormatter)base.CreateFormatter());

        internal static JsonMessageFormatter ConfigureFormatter(JsonMessageFormatter formatter)
        {
            formatter.JsonSerializer.Converters.Add(AggregateJsonConverter.Instance);
            return formatter;
        }

        protected override JsonRpcConnection CreateConnection(JsonRpc jsonRpc)
        {
            jsonRpc.CancelLocallyInvokedMethodsWhenConnectionIsClosed = true;
            return base.CreateConnection(jsonRpc);
        }
    }
}
