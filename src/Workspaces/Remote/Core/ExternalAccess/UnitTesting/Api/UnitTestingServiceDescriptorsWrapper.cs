// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MessagePack;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingServiceDescriptorsWrapper
    {
        internal readonly ServiceDescriptors UnderlyingObject;

        public UnitTestingServiceDescriptorsWrapper(
            string componentLevelPrefix,
            Func<string, string> featureDisplayNameProvider,
            IEnumerable<(Type serviceInterface, Type? callbackInterface)> interfaces)
            => UnderlyingObject = new ServiceDescriptors(componentLevelPrefix, featureDisplayNameProvider, interfaces);

        /// <summary>
        /// TO be called from a service factory in OOP.
        /// </summary>
        public ServiceJsonRpcDescriptor GetDescriptorForServiceFactory(Type serviceInterface)
            => UnderlyingObject.GetServiceDescriptor(serviceInterface, isRemoteHost64Bit: IntPtr.Size == 8);

        public static MessagePackSerializerOptions MessagePackOptions
            => ServiceDescriptor.Options;
    }
}
