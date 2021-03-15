// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private static readonly JsonRpcTargetOptions s_jsonRpcTargetOptions = new()
        {
            // Do not allow JSON-RPC to automatically subscribe to events and remote their calls.
            NotifyClientOfEvents = false,

            // Only allow public methods (may be on internal types) to be invoked remotely.
            AllowNonPublicInvocation = false
        };

        private readonly Func<string, string> _featureDisplayNameProvider;
        private readonly RemoteSerializationOptions _serializationOptions;

        private ServiceDescriptor(ServiceMoniker serviceMoniker, RemoteSerializationOptions serializationOptions, Func<string, string> displayNameProvider, Type? clientInterface)
            : base(serviceMoniker, clientInterface, serializationOptions.Formatter, serializationOptions.MessageDelimiters, serializationOptions.MultiplexingStreamOptions)
        {
            _featureDisplayNameProvider = displayNameProvider;
            _serializationOptions = serializationOptions;
        }

        private ServiceDescriptor(ServiceDescriptor copyFrom)
          : base(copyFrom)
        {
            _featureDisplayNameProvider = copyFrom._featureDisplayNameProvider;
            _serializationOptions = copyFrom._serializationOptions;
        }

        public static ServiceDescriptor CreateRemoteServiceDescriptor(string serviceName, RemoteSerializationOptions options, Func<string, string> featureDisplayNameProvider, Type? clientInterface)
            => new(new ServiceMoniker(serviceName), options, featureDisplayNameProvider, clientInterface);

        public static ServiceDescriptor CreateInProcServiceDescriptor(string serviceName, Func<string, string> featureDisplayNameProvider)
            => new(new ServiceMoniker(serviceName), RemoteSerializationOptions.Default, featureDisplayNameProvider, clientInterface: null);

        protected override ServiceRpcDescriptor Clone()
            => new ServiceDescriptor(this);

        protected override IJsonRpcMessageFormatter CreateFormatter()
            => _serializationOptions.ConfigureFormatter(base.CreateFormatter());

        protected override JsonRpcConnection CreateConnection(JsonRpc jsonRpc)
        {
            jsonRpc.CancelLocallyInvokedMethodsWhenConnectionIsClosed = true;
            var connection = base.CreateConnection(jsonRpc);
            connection.LocalRpcTargetOptions = s_jsonRpcTargetOptions;
            return connection;
        }

        internal string GetFeatureDisplayName()
            => _featureDisplayNameProvider(Moniker.Name);
    }
}
