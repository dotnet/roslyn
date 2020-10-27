// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    internal static class ServiceDescriptors
    {
        /// <summary>
        /// Brokered services must be defined in Microsoft.VisualStudio service namespace in order to be considered first party.
        /// </summary>
        internal const string ServiceNamePrefix = "Microsoft.VisualStudio.LanguageServices.";

        private const string InterfaceNamePrefix = "IRemote";
        private const string InterfaceNameSuffix = "Service";

        internal static readonly ImmutableDictionary<Type, (ServiceDescriptor descriptor32, ServiceDescriptor descriptor64, ServiceDescriptor descriptor64ServerGC)> Descriptors = ImmutableDictionary.CreateRange(new[]
        {
            CreateDescriptors(typeof(IRemoteAssetSynchronizationService)),
            CreateDescriptors(typeof(IRemoteAsynchronousOperationListenerService)),
            CreateDescriptors(typeof(IRemoteTodoCommentsDiscoveryService), callbackInterface: typeof(IRemoteTodoCommentsDiscoveryService.ICallback)),
            CreateDescriptors(typeof(IRemoteDesignerAttributeDiscoveryService), callbackInterface: typeof(IRemoteDesignerAttributeDiscoveryService.ICallback)),
            CreateDescriptors(typeof(IRemoteProjectTelemetryService), callbackInterface: typeof(IRemoteProjectTelemetryService.ICallback)),
            CreateDescriptors(typeof(IRemoteDiagnosticAnalyzerService)),
            CreateDescriptors(typeof(IRemoteSemanticClassificationService)),
            CreateDescriptors(typeof(IRemoteSemanticClassificationCacheService)),
            CreateDescriptors(typeof(IRemoteDocumentHighlightsService)),
            CreateDescriptors(typeof(IRemoteEncapsulateFieldService)),
            CreateDescriptors(typeof(IRemoteRenamerService)),
            CreateDescriptors(typeof(IRemoteConvertTupleToStructCodeRefactoringService)),
            CreateDescriptors(typeof(IRemoteSymbolFinderService), callbackInterface: typeof(IRemoteSymbolFinderService.ICallback)),
            CreateDescriptors(typeof(IRemoteFindUsagesService), callbackInterface: typeof(IRemoteFindUsagesService.ICallback)),
            CreateDescriptors(typeof(IRemoteNavigateToSearchService)),
            CreateDescriptors(typeof(IRemoteMissingImportDiscoveryService), callbackInterface: typeof(IRemoteMissingImportDiscoveryService.ICallback)),
            CreateDescriptors(typeof(IRemoteSymbolSearchUpdateService), callbackInterface: typeof(IRemoteSymbolSearchUpdateService.ICallback)),
            CreateDescriptors(typeof(IRemoteExtensionMethodImportCompletionService)),
            CreateDescriptors(typeof(IRemoteDependentTypeFinderService)),
            CreateDescriptors(typeof(IRemoteGlobalNotificationDeliveryService)),
            CreateDescriptors(typeof(IRemoteCodeLensReferencesService)),
        });

        internal static string GetServiceName(Type serviceInterface)
        {
            Contract.ThrowIfFalse(serviceInterface.IsInterface);
            var interfaceName = serviceInterface.Name;
            Contract.ThrowIfFalse(interfaceName.StartsWith(InterfaceNamePrefix, StringComparison.Ordinal));
            Contract.ThrowIfFalse(interfaceName.EndsWith(InterfaceNameSuffix, StringComparison.Ordinal));

            return interfaceName.Substring(InterfaceNamePrefix.Length, interfaceName.Length - InterfaceNamePrefix.Length - InterfaceNameSuffix.Length);
        }

        internal static string GetQualifiedServiceName(Type serviceInterface)
            => ServiceNamePrefix + GetServiceName(serviceInterface);

        private static KeyValuePair<Type, (ServiceDescriptor, ServiceDescriptor, ServiceDescriptor)> CreateDescriptors(Type serviceInterface, Type? callbackInterface = null)
        {
            Contract.ThrowIfFalse(callbackInterface == null || callbackInterface.IsInterface);

            var serviceName = GetQualifiedServiceName(serviceInterface);
            var descriptor32 = ServiceDescriptor.CreateRemoteServiceDescriptor(serviceName, callbackInterface);
            var descriptor64 = ServiceDescriptor.CreateRemoteServiceDescriptor(serviceName + RemoteServiceName.Suffix64, callbackInterface);
            var descriptor64ServerGC = ServiceDescriptor.CreateRemoteServiceDescriptor(serviceName + RemoteServiceName.Suffix64 + RemoteServiceName.SuffixServerGC, callbackInterface);
            return new(serviceInterface, (descriptor32, descriptor64, descriptor64ServerGC));
        }

        public static ServiceDescriptor GetServiceDescriptor(Type serviceType, bool isRemoteHost64Bit, bool isRemoteHostServerGC)
        {
            var (descriptor32, descriptor64, descriptor64ServerGC) = Descriptors[serviceType];
            return (isRemoteHost64Bit, isRemoteHostServerGC) switch
            {
                (true, false) => descriptor64,
                (true, true) => descriptor64ServerGC,
                _ => descriptor32,
            };
        }

        internal static string GetFeatureName(Type serviceInterface)
            => RemoteWorkspacesResources.GetResourceString("FeatureName_" + GetServiceName(serviceInterface));
    }
}
