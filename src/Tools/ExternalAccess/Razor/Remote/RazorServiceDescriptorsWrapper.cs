// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime;
using MessagePack;
using MessagePack.Formatters;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Framework;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal readonly struct RazorServiceDescriptorsWrapper
    {
        internal readonly ServiceDescriptors UnderlyingObject;

        /// <summary>
        /// Creates a service descriptor set for services using MessagePack serialization.
        /// </summary>
        public RazorServiceDescriptorsWrapper(
            string componentName,
            Func<string, string> featureDisplayNameProvider,
            ImmutableArray<IMessagePackFormatter> additionalFormatters,
            ImmutableArray<IFormatterResolver> additionalResolvers,
            IEnumerable<(Type serviceInterface, Type? callbackInterface)> interfaces)
            => UnderlyingObject = new ServiceDescriptors(componentName, featureDisplayNameProvider, new RemoteSerializationOptions(additionalFormatters, additionalResolvers), interfaces);

        /// <summary>
        /// Creates a service descriptor set for services using JSON serialization.
        /// </summary>
        public RazorServiceDescriptorsWrapper(
            string componentName,
            Func<string, string> featureDisplayNameProvider,
            ImmutableArray<JsonConverter> jsonConverters,
            IEnumerable<(Type serviceInterface, Type? callbackInterface)> interfaces)
            => UnderlyingObject = new ServiceDescriptors(componentName, featureDisplayNameProvider, new RemoteSerializationOptions(jsonConverters), interfaces);

        /// <summary>
        /// To be called from a service factory in OOP.
        /// </summary>
        public ServiceJsonRpcDescriptor GetDescriptorForServiceFactory(Type serviceInterface)
            => UnderlyingObject.GetServiceDescriptorForServiceFactory(serviceInterface);

        public MessagePackSerializerOptions MessagePackOptions
            => UnderlyingObject.Options.MessagePackOptions;

        public ImmutableArray<JsonConverter> JsonConverters
            => UnderlyingObject.Options.JsonConverters;
    }
}
