// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.ServiceHub.Framework;
using Newtonsoft.Json;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class ServiceDescriptor : ServiceJsonRpcDescriptor
    {
        private ServiceDescriptor(ServiceMoniker serviceMoniker, Type? clientInterface)
            : base(serviceMoniker, clientInterface, Formatters.UTF8, MessageDelimiters.HttpLikeHeaders)
        {
        }

        private ServiceDescriptor(ServiceDescriptor copyFrom)
          : base(copyFrom)
        {
        }

        public static ServiceDescriptor CreateRemoteServiceDescriptor(WellKnownServiceHubService service, Type? clientInterface, bool isRemoteHost64Bit)
            => new ServiceDescriptor(
                new ServiceMoniker(new RemoteServiceName(service).ToString(isRemoteHost64Bit)),
                clientInterface);

        public static ServiceDescriptor CreateInProcServiceDescriptor(string serviceName)
            => new ServiceDescriptor(new ServiceMoniker(serviceName), clientInterface: null);

        protected override ServiceRpcDescriptor Clone()
            => new ServiceDescriptor(this);

        protected override IJsonRpcMessageFormatter CreateFormatter()
            => ConfigureFormatter((JsonMessageFormatter)base.CreateFormatter());

        internal static JsonMessageFormatter ConfigureFormatter(JsonMessageFormatter formatter)
        {
            // disable interpreting of strings as DateTime during deserialization:
            formatter.JsonSerializer.DateParseHandling = DateParseHandling.None;

            formatter.JsonSerializer.Converters.Add(AggregateJsonConverter.Instance);
            return formatter;
        }
    }
}
