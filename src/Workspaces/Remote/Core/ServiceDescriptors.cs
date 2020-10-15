// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Resources;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.ProjectTelemetry;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.TodoComments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Service descriptors of brokered Roslyn ServiceHub services.
    /// </summary>
    internal sealed class ServiceDescriptors
    {
        /// <summary>
        /// Brokered services must be defined in Microsoft.VisualStudio service namespace in order to be considered first party.
        /// </summary>
        internal const string ServiceNameTopLevelPrefix = "Microsoft.VisualStudio.";

        internal const string ServiceNameComponentLevelPrefix = "LanguageServices.";

        private const string InterfaceNamePrefix = "IRemote";
        private const string InterfaceNameSuffix = "Service";

        public static readonly ServiceDescriptors Instance = new(ServiceNameComponentLevelPrefix, GetFeatureDisplayName, new[]

        private readonly ImmutableDictionary<Type, (ServiceDescriptor descriptor32, ServiceDescriptor descriptor64, ServiceDescriptor descriptor64ServerGC)> _descriptors;
        private readonly string _componentLevelPrefix;
        private readonly Func<string, string> _featureDisplayNameProvider;

        public ServiceDescriptors(
            string componentLevelPrefix,
            Func<string, string> featureDisplayNameProvider,
            IEnumerable<(Type serviceInterface, Type? callbackInterface)> interfaces)
        {
            _componentLevelPrefix = componentLevelPrefix;
            _featureDisplayNameProvider = featureDisplayNameProvider;
            _descriptors = interfaces.ToImmutableDictionary(i => i.serviceInterface, i => CreateDescriptors(i.serviceInterface, i.callbackInterface));
        }

        internal static string GetServiceName(Type serviceInterface)
        {
            Contract.ThrowIfFalse(serviceInterface.IsInterface);
            var interfaceName = serviceInterface.Name;
            Contract.ThrowIfFalse(interfaceName.StartsWith(InterfaceNamePrefix, StringComparison.Ordinal));
            Contract.ThrowIfFalse(interfaceName.EndsWith(InterfaceNameSuffix, StringComparison.Ordinal));

            return interfaceName.Substring(InterfaceNamePrefix.Length, interfaceName.Length - InterfaceNamePrefix.Length - InterfaceNameSuffix.Length);
        }

        internal string GetQualifiedServiceName(Type serviceInterface)
            => ServiceNameTopLevelPrefix + _componentLevelPrefix + GetServiceName(serviceInterface);


        private (ServiceDescriptor, ServiceDescriptor) CreateDescriptors(Type serviceInterface, Type? callbackInterface = null)
        {
            Contract.ThrowIfFalse(callbackInterface == null || callbackInterface.IsInterface);

            var qualifiedServiceName = GetQualifiedServiceName(serviceInterface);
            var descriptor32 = ServiceDescriptor.CreateRemoteServiceDescriptor(qualifiedServiceName, _featureDisplayNameProvider, callbackInterface);
            var descriptor64 = ServiceDescriptor.CreateRemoteServiceDescriptor(qualifiedServiceName + RemoteServiceName.Suffix64, _featureDisplayNameProvider, callbackInterface);
            var descriptor64ServerGC = ServiceDescriptor.CreateRemoteServiceDescriptor(qualifiedServiceName + RemoteServiceName.Suffix64 + RemoteServiceName.SuffixServerGC, _featureDisplayNameProvider, callbackInterface);
            return (descriptor32, descriptor64, descriptor64ServerGC);
        }

        public static ServiceDescriptor GetServiceDescriptor(Type serviceType, bool isRemoteHost64Bit, bool isRemoteHostServerGC)
        {
            var (descriptor32, descriptor64, descriptor64ServerGC) = _descriptors[serviceType];
            return (isRemoteHost64Bit, isRemoteHostServerGC) switch
            {
                (true, false) => descriptor64,
                (true, true) => descriptor64ServerGC,
                _ => descriptor32,
            };
        }

        internal static string GetFeatureDisplayName(string qualifiedServiceName)
        {
            var prefixLength = qualifiedServiceName.LastIndexOf('.') + 1;
            Contract.ThrowIfFalse(prefixLength > 0);

            var suffixLength = qualifiedServiceName.EndsWith(RemoteServiceName.Suffix64, StringComparison.Ordinal) ? RemoteServiceName.Suffix64.Length : 0;
            var shortName = qualifiedServiceName.Substring(prefixLength, qualifiedServiceName.Length - prefixLength - suffixLength);

            return RemoteWorkspacesResources.GetResourceString("FeatureName_" + shortName);
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly ServiceDescriptors _serviceDescriptors;

            internal TestAccessor(ServiceDescriptors serviceDescriptors)
                => _serviceDescriptors = serviceDescriptors;

            public ImmutableDictionary<Type, (ServiceDescriptor descriptor32, ServiceDescriptor descriptor64)> Descriptors
                => _serviceDescriptors._descriptors;
        }
    }
}
