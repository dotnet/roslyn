// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ProjectTelemetry;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Service descriptors of brokered Roslyn ServiceHub services.
    /// </summary>
    internal static class ServiceDescriptors
    {
        private static readonly ImmutableDictionary<Type, (ServiceDescriptor descriptor32, ServiceDescriptor descriptor64)> s_descriptors = ImmutableDictionary.CreateRange(new[]
        {
            CreateDescriptors(typeof(IRemoteTodoCommentsService), callbackInterface: typeof(ITodoCommentsListener)),
            CreateDescriptors(typeof(IRemoteDesignerAttributeService), callbackInterface: typeof(IDesignerAttributeListener)),
            CreateDescriptors(typeof(IRemoteProjectTelemetryService), callbackInterface: typeof(IProjectTelemetryListener)),
            CreateDescriptors(typeof(IRemoteDiagnosticAnalyzerService)),
            CreateDescriptors(typeof(IRemoteSemanticClassificationService)),
            CreateDescriptors(typeof(IRemoteSemanticClassificationCacheService)),
        });

        private static KeyValuePair<Type, (ServiceDescriptor, ServiceDescriptor)> CreateDescriptors(Type serviceInterface, Type? callbackInterface = null)
        {
            Contract.ThrowIfFalse(serviceInterface.IsInterface);
            Contract.ThrowIfFalse(callbackInterface == null || callbackInterface.IsInterface);
            Contract.ThrowIfFalse(serviceInterface.Name[0] == 'I');

            var serviceName = RemoteServiceName.Prefix + serviceInterface.Name.Substring(1);

            var descriptor32 = ServiceDescriptor.CreateRemoteServiceDescriptor(serviceName, callbackInterface);
            var descriptor64 = ServiceDescriptor.CreateRemoteServiceDescriptor(serviceName + RemoteServiceName.Suffix64, callbackInterface);
            return new(serviceInterface, (descriptor32, descriptor64));
        }

        public static ServiceRpcDescriptor GetServiceDescriptor(Type serviceType, bool isRemoteHost64Bit)
        {
            var (descriptor32, descriptor64) = s_descriptors[serviceType];
            return isRemoteHost64Bit ? descriptor64 : descriptor32;
        }
    }
}
