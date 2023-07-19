// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Describes Roslyn remote brokered service. 
    /// Adds Roslyn specific JSON converters and RPC settings to the default implementation.
    /// </summary>
    internal sealed class ServiceDescriptor : ServiceJsonRpcDescriptor
    {
        /// <summary>
        /// Brokered services must be defined in Microsoft.VisualStudio service namespace in order to be considered first party.
        /// </summary>
        internal const string ServiceNameTopLevelPrefix = "Microsoft.VisualStudio.";

        private static readonly JsonRpcTargetOptions s_jsonRpcTargetOptions = new()
        {
            // Do not allow JSON-RPC to automatically subscribe to events and remote their calls.
            NotifyClientOfEvents = false,

            // Only allow public methods (may be on internal types) to be invoked remotely.
            AllowNonPublicInvocation = false
        };

        internal readonly string ComponentName;
        internal readonly string SimpleName;

        private readonly Func<string, string> _featureDisplayNameProvider;
        private readonly RemoteSerializationOptions _serializationOptions;

        private ServiceDescriptor(
            ServiceMoniker serviceMoniker,
            string componentName,
            string simpleName,
            RemoteSerializationOptions serializationOptions,
            Func<string, string> displayNameProvider,
            Type? clientInterface)
            : base(serviceMoniker, clientInterface, serializationOptions.Formatter, serializationOptions.MessageDelimiters, serializationOptions.MultiplexingStreamOptions)
        {
            ComponentName = componentName;
            SimpleName = simpleName;
            _featureDisplayNameProvider = displayNameProvider;
            _serializationOptions = serializationOptions;
        }

        private ServiceDescriptor(ServiceDescriptor copyFrom)
          : base(copyFrom)
        {
            ComponentName = copyFrom.ComponentName;
            SimpleName = copyFrom.SimpleName;
            _featureDisplayNameProvider = copyFrom._featureDisplayNameProvider;
            _serializationOptions = copyFrom._serializationOptions;
        }

        public static ServiceDescriptor CreateRemoteServiceDescriptor(string componentName, string simpleName, string suffix, RemoteSerializationOptions options, Func<string, string> featureDisplayNameProvider, Type? clientInterface)
            => new(CreateMoniker(componentName, simpleName, suffix), componentName, simpleName, options, featureDisplayNameProvider, clientInterface);

        public static ServiceDescriptor CreateInProcServiceDescriptor(string componentName, string simpleName, string suffix, Func<string, string> featureDisplayNameProvider)
            => new(CreateMoniker(componentName, simpleName, suffix), componentName, simpleName, RemoteSerializationOptions.Default, featureDisplayNameProvider, clientInterface: null);

        private static ServiceMoniker CreateMoniker(string componentName, string simpleName, string suffix)
            => new(ServiceNameTopLevelPrefix + componentName + "." + simpleName + suffix);

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
            => _featureDisplayNameProvider(SimpleName);
    }
}
