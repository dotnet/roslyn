// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.ProjectTelemetry;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.CodeAnalysis.ValueTracking;
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

        internal const string ComponentName = "LanguageServices";

        private const string InterfaceNamePrefix = "IRemote";
        private const string InterfaceNameSuffix = "Service";

        internal const string Prefix = "roslyn";
        private const string Suffix64 = "64";
        private const string SuffixServerGC = "S";
        private const string SuffixCoreClr = "Core";

        public static readonly ServiceDescriptors Instance = new(ComponentName, GetFeatureDisplayName, RemoteSerializationOptions.Default, new (Type, Type?)[]
        {
            (typeof(IRemoteAssetSynchronizationService), null),
            (typeof(IRemoteAsynchronousOperationListenerService), null),
            (typeof(IRemoteTodoCommentsDiscoveryService), typeof(IRemoteTodoCommentsDiscoveryService.ICallback)),
            (typeof(IRemoteDesignerAttributeDiscoveryService), typeof(IRemoteDesignerAttributeDiscoveryService.ICallback)),
            (typeof(IRemoteProjectTelemetryService), typeof(IRemoteProjectTelemetryService.ICallback)),
            (typeof(IRemoteDiagnosticAnalyzerService), null),
            (typeof(IRemoteSemanticClassificationService), null),
            (typeof(IRemoteSemanticClassificationCacheService), null),
            (typeof(IRemoteDocumentHighlightsService), null),
            (typeof(IRemoteEncapsulateFieldService), null),
            (typeof(IRemoteRenamerService), null),
            (typeof(IRemoteConvertTupleToStructCodeRefactoringService), null),
            (typeof(IRemoteSymbolFinderService), typeof(IRemoteSymbolFinderService.ICallback)),
            (typeof(IRemoteFindUsagesService), typeof(IRemoteFindUsagesService.ICallback)),
            (typeof(IRemoteNavigateToSearchService), typeof(IRemoteNavigateToSearchService.ICallback)),
            (typeof(IRemoteNavigationBarItemService), null),
            (typeof(IRemoteMissingImportDiscoveryService), typeof(IRemoteMissingImportDiscoveryService.ICallback)),
            (typeof(IRemoteSymbolSearchUpdateService), typeof(IRemoteSymbolSearchUpdateService.ICallback)),
            (typeof(IRemoteExtensionMethodImportCompletionService), null),
            (typeof(IRemoteDependentTypeFinderService), null),
            (typeof(IRemoteGlobalNotificationDeliveryService), null),
            (typeof(IRemoteCodeLensReferencesService), null),
            (typeof(IRemoteEditAndContinueService), typeof(IRemoteEditAndContinueService.ICallback)),
            (typeof(IRemoteValueTrackingService), null),
            (typeof(IRemoteInheritanceMarginService), null),
            (typeof(IRemoteUnusedReferenceAnalysisService), null),
            (typeof(IRemoteProcessTelemetryService), null),
            (typeof(IRemoteCompilationAvailableService), null),
        });

        internal readonly RemoteSerializationOptions Options;
        private readonly ImmutableDictionary<Type, (ServiceDescriptor descriptor64, ServiceDescriptor descriptor64ServerGC, ServiceDescriptor descriptorCoreClr64, ServiceDescriptor descriptorCoreClr64ServerGC)> _descriptors;
        private readonly string _componentName;
        private readonly Func<string, string> _featureDisplayNameProvider;

        public ServiceDescriptors(
            string componentName,
            Func<string, string> featureDisplayNameProvider,
            RemoteSerializationOptions serializationOptions,
            IEnumerable<(Type serviceInterface, Type? callbackInterface)> interfaces)
        {
            Options = serializationOptions;
            _componentName = componentName;
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
            => ServiceNameTopLevelPrefix + _componentName + "." + GetServiceName(serviceInterface);

        private (ServiceDescriptor, ServiceDescriptor, ServiceDescriptor, ServiceDescriptor) CreateDescriptors(Type serviceInterface, Type? callbackInterface)
        {
            Contract.ThrowIfFalse(callbackInterface == null || callbackInterface.IsInterface);

            var qualifiedServiceName = GetQualifiedServiceName(serviceInterface);
            var descriptor64 = ServiceDescriptor.CreateRemoteServiceDescriptor(qualifiedServiceName + Suffix64, Options, _featureDisplayNameProvider, callbackInterface);
            var descriptor64ServerGC = ServiceDescriptor.CreateRemoteServiceDescriptor(qualifiedServiceName + Suffix64 + SuffixServerGC, Options, _featureDisplayNameProvider, callbackInterface);
            var descriptorCoreClr64 = ServiceDescriptor.CreateRemoteServiceDescriptor(qualifiedServiceName + SuffixCoreClr + Suffix64, Options, _featureDisplayNameProvider, callbackInterface);
            var descriptorCoreClr64ServerGC = ServiceDescriptor.CreateRemoteServiceDescriptor(qualifiedServiceName + SuffixCoreClr + Suffix64 + SuffixServerGC, Options, _featureDisplayNameProvider, callbackInterface);

            return (descriptor64, descriptor64ServerGC, descriptorCoreClr64, descriptorCoreClr64ServerGC);
        }

        public ServiceDescriptor GetServiceDescriptorForServiceFactory(Type serviceType)
            => GetServiceDescriptor(serviceType, isRemoteHostServerGC: GCSettings.IsServerGC, isRemoteHostCoreClr: RemoteHostOptions.IsCurrentProcessRunningOnCoreClr());

        public ServiceDescriptor GetServiceDescriptor(Type serviceType, bool isRemoteHostServerGC, bool isRemoteHostCoreClr)
        {
            var (descriptor64, descriptor64ServerGC, descriptorCoreClr64, descriptorCoreClr64ServerGC) = _descriptors[serviceType];
            return (isRemoteHostServerGC, isRemoteHostCoreClr) switch
            {
                (false, false) => descriptor64,
                (false, true) => descriptorCoreClr64,
                (true, false) => descriptor64ServerGC,
                (true, true) => descriptorCoreClr64ServerGC,
            };
        }

        internal static string GetFeatureDisplayName(string qualifiedServiceName)
        {
            var prefixLength = qualifiedServiceName.LastIndexOf('.') + 1;
            Contract.ThrowIfFalse(prefixLength > 0);

            int suffixLength;
            if (qualifiedServiceName.EndsWith(SuffixCoreClr + Suffix64, StringComparison.Ordinal))
            {
                suffixLength = SuffixCoreClr.Length + Suffix64.Length;
            }
            else if (qualifiedServiceName.EndsWith(SuffixCoreClr + Suffix64 + SuffixServerGC, StringComparison.Ordinal))
            {
                suffixLength = SuffixCoreClr.Length + Suffix64.Length + SuffixServerGC.Length;
            }
            else if (qualifiedServiceName.EndsWith(Suffix64, StringComparison.Ordinal))
            {
                suffixLength = Suffix64.Length;
            }
            else if (qualifiedServiceName.EndsWith(Suffix64 + SuffixServerGC, StringComparison.Ordinal))
            {
                suffixLength = Suffix64.Length + SuffixServerGC.Length;
            }
            else
            {
                suffixLength = 0;
            }

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

            public ImmutableDictionary<Type, (ServiceDescriptor descriptor64, ServiceDescriptor descriptor64ServerGC, ServiceDescriptor descriptorCoreClr64, ServiceDescriptor descriptorCoreClr64ServerGC)> Descriptors
                => _serviceDescriptors._descriptors;
        }
    }
}
