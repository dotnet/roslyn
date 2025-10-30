// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeFixes.FullyQualify;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.LegacySolutionEvents;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.RelatedDocuments;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.SemanticSearch;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.CodeAnalysis.ValueTracking;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Service descriptors of brokered Roslyn ServiceHub services.
/// </summary>
internal sealed class ServiceDescriptors
{
    internal const string ComponentName = "LanguageServices";

    private const string InterfaceNamePrefix = "IRemote";
    private const string InterfaceNameSuffix = "Service";

    private const string Suffix64 = "64";
    private const string SuffixServerGC = "S";
    private const string SuffixCoreClr = "Core";

    public static readonly ServiceDescriptors Instance = new(ComponentName, GetFeatureDisplayName, RemoteSerializationOptions.Default, new (Type, Type?)[]
    {
        (typeof(IRemoteAssetSynchronizationService), null),
        (typeof(IRemoteAsynchronousOperationListenerService), null),
        (typeof(IRemoteCodeLensReferencesService), null),
        (typeof(IRemoteConvertTupleToStructCodeRefactoringService), null),
        (typeof(IRemoteCopilotChangeAnalysisService), null),
        (typeof(IRemoteCopilotProposalAdjusterService), null),
        (typeof(IRemoteDependentTypeFinderService), null),
        (typeof(IRemoteDesignerAttributeDiscoveryService), typeof(IRemoteDesignerAttributeDiscoveryService.ICallback)),
        (typeof(IRemoteDiagnosticAnalyzerService), null),
        (typeof(IRemoteDocumentHighlightsService), null),
        (typeof(IRemoteEditAndContinueService), typeof(IRemoteEditAndContinueService.ICallback)),
        (typeof(IRemoteEncapsulateFieldService), null),
        (typeof(IRemoteExtensionMessageHandlerService), null),
        (typeof(IRemoteExtensionMethodImportCompletionService), null),
        (typeof(IRemoteFindUsagesService), typeof(IRemoteFindUsagesService.ICallback)),
        (typeof(IRemoteFullyQualifyService), null),
        (typeof(IRemoteInheritanceMarginService), null),
        (typeof(IRemoteKeepAliveService), null),
        (typeof(IRemoteLegacySolutionEventsAggregationService), null),
        (typeof(IRemoteMissingImportDiscoveryService), typeof(IRemoteMissingImportDiscoveryService.ICallback)),
        (typeof(IRemoteNavigateToSearchService), typeof(IRemoteNavigateToSearchService.ICallback)),
        (typeof(IRemoteNavigationBarItemService), null),
        (typeof(IRemoteProcessTelemetryService), null),
        (typeof(IRemoteInitializationService), null),
        (typeof(IRemoteRelatedDocumentsService), typeof(IRemoteRelatedDocumentsService.ICallback)),
        (typeof(IRemoteRenamerService), null),
        (typeof(IRemoteSemanticClassificationService), null),
        (typeof(IRemoteSemanticSearchService), typeof(IRemoteSemanticSearchService.ICallback)),
        (typeof(IRemoteSourceGenerationService), null),
        (typeof(IRemoteStackTraceExplorerService), null),
        (typeof(IRemoteSymbolFinderService), typeof(IRemoteSymbolFinderService.ICallback)),
        (typeof(IRemoteSymbolSearchUpdateService), null),
        (typeof(IRemoteTaskListService), null),
        (typeof(IRemoteUnitTestingSearchService), null),
        (typeof(IRemoteUnusedReferenceAnalysisService), null),
        (typeof(IRemoteValueTrackingService), null),
    });

    internal readonly RemoteSerializationOptions Options;
    private readonly ImmutableDictionary<Type, (ServiceDescriptor descriptorCoreClr64, ServiceDescriptor descriptorCoreClr64ServerGC)> _descriptors;
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

    internal static string GetSimpleName(Type serviceInterface)
    {
        Contract.ThrowIfFalse(serviceInterface.IsInterface);
        var interfaceName = serviceInterface.Name;
        Contract.ThrowIfFalse(interfaceName.StartsWith(InterfaceNamePrefix, StringComparison.Ordinal));
        Contract.ThrowIfFalse(interfaceName.EndsWith(InterfaceNameSuffix, StringComparison.Ordinal));

        return interfaceName.Substring(InterfaceNamePrefix.Length, interfaceName.Length - InterfaceNamePrefix.Length - InterfaceNameSuffix.Length);
    }

    private (ServiceDescriptor descriptorCoreClr64, ServiceDescriptor descriptorCoreClr64ServerGC) CreateDescriptors(Type serviceInterface, Type? callbackInterface)
    {
        Contract.ThrowIfFalse(callbackInterface == null || callbackInterface.IsInterface);

        var simpleName = GetSimpleName(serviceInterface);
        var descriptorCoreClr64 = ServiceDescriptor.CreateRemoteServiceDescriptor(_componentName, simpleName, SuffixCoreClr + Suffix64, Options, _featureDisplayNameProvider, callbackInterface);
        var descriptorCoreClr64ServerGC = ServiceDescriptor.CreateRemoteServiceDescriptor(_componentName, simpleName, SuffixCoreClr + Suffix64 + SuffixServerGC, Options, _featureDisplayNameProvider, callbackInterface);

        return (descriptorCoreClr64, descriptorCoreClr64ServerGC);
    }

    public static bool IsCurrentProcessRunningOnCoreClr()
        => !RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework") &&
           !RuntimeInformation.FrameworkDescription.StartsWith(".NET Native");

    public ServiceDescriptor GetServiceDescriptorForServiceFactory(Type serviceType)
        => GetServiceDescriptor(serviceType, RemoteProcessConfiguration.ServerGC);

    public ServiceDescriptor GetServiceDescriptor(Type serviceType, RemoteProcessConfiguration configuration)
    {
        if (!_descriptors.TryGetValue(serviceType, out var descriptor))
        {
            throw ExceptionUtilities.UnexpectedValue(serviceType);
        }

        var (descriptorCoreClr64, descriptorCoreClr64ServerGC) = descriptor;
        return (configuration & RemoteProcessConfiguration.ServerGC) switch
        {
            0 => descriptorCoreClr64,
            RemoteProcessConfiguration.ServerGC => descriptorCoreClr64ServerGC,
            _ => throw ExceptionUtilities.Unreachable()
        };
    }

    /// <summary>
    /// <paramref name="serviceName"/> is a short service name, e.g. "EditAndContinue".
    /// </summary>
    internal static string GetFeatureDisplayName(string serviceName)
        => RemoteWorkspacesResources.GetResourceString("FeatureName_" + serviceName);

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor
    {
        private readonly ServiceDescriptors _serviceDescriptors;

        internal TestAccessor(ServiceDescriptors serviceDescriptors)
            => _serviceDescriptors = serviceDescriptors;

        public ImmutableDictionary<Type, (ServiceDescriptor descriptorCoreClr64, ServiceDescriptor descriptorCoreClr64ServerGC)> Descriptors
            => _serviceDescriptors._descriptors;
    }
}
