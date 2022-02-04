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

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingServiceDescriptorsWrapper
    {
        internal readonly ServiceDescriptors UnderlyingObject;

        public UnitTestingServiceDescriptorsWrapper(
            string componentName,
            Func<string, string> featureDisplayNameProvider,
            ImmutableArray<IMessagePackFormatter> additionalFormatters,
            ImmutableArray<IFormatterResolver> additionalResolvers,
            IEnumerable<(Type serviceInterface, Type? callbackInterface)> interfaces)
            => UnderlyingObject = new ServiceDescriptors(componentName, featureDisplayNameProvider, new RemoteSerializationOptions(additionalFormatters, additionalResolvers), interfaces);

        /// <summary>
        /// To be called from a service factory in OOP.
        /// </summary>
        public ServiceJsonRpcDescriptor GetDescriptorForServiceFactory(Type serviceInterface)
            => UnderlyingObject.GetServiceDescriptorForServiceFactory(serviceInterface);

        public MessagePackSerializerOptions MessagePackOptions
            => UnderlyingObject.Options.MessagePackOptions;
    }
}
