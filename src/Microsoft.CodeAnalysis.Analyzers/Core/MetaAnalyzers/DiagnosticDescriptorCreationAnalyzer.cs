// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.ReleaseTracking;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed partial class DiagnosticDescriptorCreationAnalyzer : DiagnosticAnalyzer
    {
        private const string HelpLinkUriParameterName = "helpLinkUri";
        private const string CategoryParameterName = "category";
        private const string DiagnosticIdParameterName = "id";
        private const string CustomTagsParameterName = "customTags";
        private const string IsEnabledByDefaultParameterName = "isEnabledByDefault";
        private const string DefaultSeverityParameterName = "defaultSeverity";
        private const string RuleLevelParameterName = "ruleLevel";

        private static readonly ImmutableHashSet<string> CADiagnosticIdAllowedAssemblies = ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "Microsoft.CodeAnalysis.VersionCheckAnalyzer",
            "Microsoft.CodeAnalysis.NetAnalyzers",
            "Microsoft.CodeAnalysis.CSharp.NetAnalyzers",
            "Microsoft.CodeAnalysis.VisualBasic.NetAnalyzers",
            "Microsoft.CodeQuality.Analyzers",
            "Microsoft.CodeQuality.CSharp.Analyzers",
            "Microsoft.CodeQuality.VisualBasic.Analyzers",
            "Microsoft.NetCore.Analyzers",
            "Microsoft.NetCore.CSharp.Analyzers",
            "Microsoft.NetCore.VisualBasic.Analyzers",
            "Microsoft.NetFramework.Analyzers",
            "Microsoft.NetFramework.CSharp.Analyzers",
            "Microsoft.NetFramework.VisualBasic.Analyzers",
            "Text.Analyzers",
            "Text.CSharp.Analyzers",
            "Text.VisualBasic.Analyzers");

        private static readonly LocalizableString s_localizableUseLocalizableStringsTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseLocalizableStringsMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseLocalizableStringsDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableProvideHelpUriTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideHelpUriInDescriptorTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableProvideHelpUriMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideHelpUriInDescriptorMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableProvideHelpUriDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideHelpUriInDescriptorDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableDiagnosticIdMustBeAConstantTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DiagnosticIdMustBeAConstantTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDiagnosticIdMustBeAConstantMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DiagnosticIdMustBeAConstantMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDiagnosticIdMustBeAConstantDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DiagnosticIdMustBeAConstantDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableUseUniqueDiagnosticIdTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseUniqueDiagnosticIdTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseUniqueDiagnosticIdMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseUniqueDiagnosticIdMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseUniqueDiagnosticIdDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseUniqueDiagnosticIdDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableProvideCustomTagsTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideCustomTagsInDescriptorTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableProvideCustomTagsMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideCustomTagsInDescriptorMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableProvideCustomTagsDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideCustomTagsInDescriptorDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableDoNotUseReservedDiagnosticIdTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotUseReservedDiagnosticIdTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDoNotUseReservedDiagnosticIdMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotUseReservedDiagnosticIdMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDoNotUseReservedDiagnosticIdDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotUseReservedDiagnosticIdDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor UseLocalizableStringsInDescriptorRule = new DiagnosticDescriptor(
            DiagnosticIds.UseLocalizableStringsInDescriptorRuleId,
            s_localizableUseLocalizableStringsTitle,
            s_localizableUseLocalizableStringsMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisLocalization,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableUseLocalizableStringsDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor ProvideHelpUriInDescriptorRule = new DiagnosticDescriptor(
            DiagnosticIds.ProvideHelpUriInDescriptorRuleId,
            s_localizableProvideHelpUriTitle,
            s_localizableProvideHelpUriMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisDocumentation,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableProvideHelpUriDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor DiagnosticIdMustBeAConstantRule = new DiagnosticDescriptor(
            DiagnosticIds.DiagnosticIdMustBeAConstantRuleId,
            s_localizableDiagnosticIdMustBeAConstantTitle,
            s_localizableDiagnosticIdMustBeAConstantMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDiagnosticIdMustBeAConstantDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor UseUniqueDiagnosticIdRule = new DiagnosticDescriptor(
            DiagnosticIds.UseUniqueDiagnosticIdRuleId,
            s_localizableUseUniqueDiagnosticIdTitle,
            s_localizableUseUniqueDiagnosticIdMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableUseUniqueDiagnosticIdDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor ProvideCustomTagsInDescriptorRule = new DiagnosticDescriptor(
            DiagnosticIds.ProvideCustomTagsInDescriptorRuleId,
            s_localizableProvideCustomTagsTitle,
            s_localizableProvideCustomTagsMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisDocumentation,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: s_localizableProvideCustomTagsDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor DoNotUseReservedDiagnosticIdRule = new DiagnosticDescriptor(
            DiagnosticIds.DoNotUseReservedDiagnosticIdRuleId,
            s_localizableDoNotUseReservedDiagnosticIdTitle,
            s_localizableDoNotUseReservedDiagnosticIdMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDoNotUseReservedDiagnosticIdDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            UseLocalizableStringsInDescriptorRule,
            ProvideHelpUriInDescriptorRule,
            DiagnosticIdMustBeAConstantRule,
            DiagnosticIdMustBeInSpecifiedFormatRule,
            UseUniqueDiagnosticIdRule,
            UseCategoriesFromSpecifiedRangeRule,
            AnalyzerCategoryAndIdRangeFileInvalidRule,
            ProvideCustomTagsInDescriptorRule,
            DoNotUseReservedDiagnosticIdRule,
            DeclareDiagnosticIdInAnalyzerReleaseRule,
            UpdateDiagnosticIdInAnalyzerReleaseRule,
            RemoveUnshippedDeletedDiagnosticIdRule,
            RemoveShippedDeletedDiagnosticIdRule,
            UnexpectedAnalyzerDiagnosticForRemovedDiagnosticIdRule,
            RemoveDuplicateEntriesForAnalyzerReleaseRule,
            RemoveDuplicateEntriesBetweenAnalyzerReleasesRule,
            InvalidEntryInAnalyzerReleasesFileRule,
            InvalidHeaderInAnalyzerReleasesFileRule,
            InvalidUndetectedEntryInAnalyzerReleasesFileRule,
            InvalidRemovedOrChangedWithoutPriorNewEntryInAnalyzerReleasesFileRule,
            EnableAnalyzerReleaseTrackingRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? diagnosticDescriptorType = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticDescriptor);
                if (diagnosticDescriptorType == null)
                {
                    return;
                }

                // Try read the additional file containing the allowed categories, and corresponding ID ranges.
                var checkCategoryAndAllowedIds = TryGetCategoryAndAllowedIdsMap(
                    compilationContext.Options.AdditionalFiles,
                    compilationContext.CancellationToken,
                    out AdditionalText? diagnosticCategoryAndIdRangeTextOpt,
                    out ImmutableDictionary<string, ImmutableArray<(string? prefix, int start, int end)>>? categoryAndAllowedIdsMap,
                    out List<Diagnostic>? invalidFileDiagnostics);

                // Try read the additional files containing the shipped and unshipped analyzer releases.
                var isAnalyzerReleaseTracking = TryGetReleaseTrackingData(
                    compilationContext.Options.AdditionalFiles,
                    compilationContext.CancellationToken,
                    out var shippedData,
                    out var unshippedData,
                    out List<Diagnostic>? invalidReleaseFileEntryDiagnostics);

                var idToAnalyzerMap = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<Location>>>();
                var seenRuleIds = PooledConcurrentSet<string>.GetInstance();
                compilationContext.RegisterOperationAction(operationAnalysisContext =>
                {
                    var fieldInitializer = (IFieldInitializerOperation)operationAnalysisContext.Operation;
                    if (!TryGetDescriptorCreateMethodAndArguments(fieldInitializer, diagnosticDescriptorType, out var creationMethod, out var creationArguments))
                    {
                        return;
                    }

                    AnalyzeTitle(creationMethod, fieldInitializer, operationAnalysisContext.ReportDiagnostic);
                    AnalyzeHelpLinkUri(operationAnalysisContext, creationArguments, out var helpLink);
                    AnalyzeCustomTags(operationAnalysisContext, creationArguments);
                    var (isEnabledByDefault, defaultSeverity) = GetDefaultSeverityAndEnabledByDefault(operationAnalysisContext.Compilation, creationArguments);

                    string? category;
                    if (!TryAnalyzeCategory(operationAnalysisContext, creationArguments, checkCategoryAndAllowedIds,
                            diagnosticCategoryAndIdRangeTextOpt, categoryAndAllowedIdsMap, out category, out var allowedIdsInfoList))
                    {
                        allowedIdsInfoList = default;
                    }

                    var analyzerName = fieldInitializer.InitializedFields.First().ContainingType.Name;
                    AnalyzeRuleId(operationAnalysisContext, creationArguments,
                        isAnalyzerReleaseTracking, shippedData, unshippedData, seenRuleIds, diagnosticCategoryAndIdRangeTextOpt,
                        category, analyzerName, helpLink, isEnabledByDefault, defaultSeverity, allowedIdsInfoList, idToAnalyzerMap);

                }, OperationKind.FieldInitializer);

                compilationContext.RegisterCompilationEndAction(compilationEndContext =>
                {
                    // Report any invalid additional file diagnostics.
                    if (invalidFileDiagnostics != null)
                    {
                        foreach (var diagnostic in invalidFileDiagnostics)
                        {
                            compilationEndContext.ReportDiagnostic(diagnostic);
                        }
                    }

                    // Report diagnostics for duplicate diagnostic ID used across analyzers.
                    foreach (var kvp in idToAnalyzerMap)
                    {
                        var ruleId = kvp.Key;
                        var analyzerToDescriptorLocationsMap = kvp.Value;
                        if (analyzerToDescriptorLocationsMap.Count <= 1)
                        {
                            // ID used by a single analyzer.
                            continue;
                        }

                        ImmutableSortedSet<string> sortedAnalyzerNames = analyzerToDescriptorLocationsMap.Keys.ToImmutableSortedSet();
                        var skippedAnalyzerName = sortedAnalyzerNames[0];
                        foreach (var analyzerName in sortedAnalyzerNames.Skip(1))
                        {
                            var locations = analyzerToDescriptorLocationsMap[analyzerName];
                            foreach (var location in locations)
                            {
                                // Diagnostic Id '{0}' is already used by analyzer '{1}'. Please use a different diagnostic ID.
                                var diagnostic = Diagnostic.Create(UseUniqueDiagnosticIdRule, location, ruleId, skippedAnalyzerName);
                                compilationEndContext.ReportDiagnostic(diagnostic);
                            }
                        }
                    }

                    // Report analyzer release tracking invalid entry and compilation end diagnostics.
                    if (isAnalyzerReleaseTracking || invalidReleaseFileEntryDiagnostics != null)
                    {
                        RoslynDebug.Assert(shippedData != null);
                        RoslynDebug.Assert(unshippedData != null);

                        ReportAnalyzerReleaseTrackingDiagnostics(invalidReleaseFileEntryDiagnostics, shippedData, unshippedData, seenRuleIds, compilationEndContext);
                    }

                    seenRuleIds.Free();
                });
            });
        }

        private static bool TryGetDescriptorCreateMethodAndArguments(
            IFieldInitializerOperation fieldInitializer,
            INamedTypeSymbol diagnosticDescriptorType,
            out IMethodSymbol creationMethod,
            out ImmutableArray<IArgumentOperation> creationArguments)
        {
            (creationMethod, creationArguments) = fieldInitializer.Value switch
            {
                IObjectCreationOperation objectCreation when IsDescriptorConstructor(objectCreation.Constructor)
                    => (objectCreation.Constructor, objectCreation.Arguments),
                IInvocationOperation invocation when IsCreateHelper(invocation.TargetMethod)
                    => (invocation.TargetMethod, invocation.Arguments),
                _ => default
            };

            return creationMethod != null;

            bool IsDescriptorConstructor(IMethodSymbol method)
                => method.ContainingType.Equals(diagnosticDescriptorType);

            // Heuristic to identify helper methods to create DiagnosticDescriptor:
            //  "A method invocation that returns 'DiagnosticDescriptor' and has a first string parameter named 'id'"
            bool IsCreateHelper(IMethodSymbol method)
                => method.ReturnType.Equals(diagnosticDescriptorType) &&
                    !method.Parameters.IsEmpty &&
                    method.Parameters[0].Name == DiagnosticIdParameterName &&
                    method.Parameters[0].Type.SpecialType == SpecialType.System_String;
        }

        private static void AnalyzeTitle(IMethodSymbol descriptorCreationMethod, IFieldInitializerOperation creation, Action<Diagnostic> reportDiagnostic)
        {
            IParameterSymbol title = descriptorCreationMethod.Parameters.FirstOrDefault(p => p.Name == "title");
            if (title != null &&
                title.Type != null &&
                title.Type.SpecialType == SpecialType.System_String)
            {
                Diagnostic diagnostic = creation.Value.CreateDiagnostic(UseLocalizableStringsInDescriptorRule, WellKnownTypeNames.MicrosoftCodeAnalysisLocalizableString);
                reportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeHelpLinkUri(OperationAnalysisContext operationAnalysisContext, ImmutableArray<IArgumentOperation> creationArguments, out string? helpLink)
        {
            helpLink = null;

            // Find the matching argument for helpLinkUri
            foreach (var argument in creationArguments)
            {
                if (argument.Parameter.Name.Equals(HelpLinkUriParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    if (argument.Value.ConstantValue.HasValue)
                    {
                        helpLink = argument.Value.ConstantValue.Value as string;
                        if (helpLink == null)
                        {
                            Diagnostic diagnostic = argument.CreateDiagnostic(ProvideHelpUriInDescriptorRule);
                            operationAnalysisContext.ReportDiagnostic(diagnostic);
                        }
                    }

                    return;
                }
            }
        }

        private static void AnalyzeCustomTags(OperationAnalysisContext operationAnalysisContext, ImmutableArray<IArgumentOperation> creationArguments)
        {
            // Find the matching argument for customTags
            foreach (var argument in creationArguments)
            {
                if (argument.Parameter.Name.Equals(CustomTagsParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    if (argument.Value is IArrayCreationOperation arrayCreation &&
                        arrayCreation.DimensionSizes.Length == 1 &&
                        arrayCreation.DimensionSizes[0].ConstantValue.HasValue &&
                        arrayCreation.DimensionSizes[0].ConstantValue.Value is int size &&
                        size == 0)
                    {
                        Diagnostic diagnostic = argument.CreateDiagnostic(ProvideCustomTagsInDescriptorRule);
                        operationAnalysisContext.ReportDiagnostic(diagnostic);
                    }

                    return;
                }
            }
        }

        private static (bool? isEnabledByDefault, DiagnosticSeverity? defaultSeverity) GetDefaultSeverityAndEnabledByDefault(Compilation compilation, ImmutableArray<IArgumentOperation> creationArguments)
        {
            var diagnosticSeverityType = compilation.GetOrCreateTypeByMetadataName(typeof(DiagnosticSeverity).FullName);
            var ruleLevelType = compilation.GetOrCreateTypeByMetadataName(typeof(RuleLevel).FullName);

            bool? isEnabledByDefault = null;
            DiagnosticSeverity? defaultSeverity = null;

            foreach (var argument in creationArguments)
            {
                switch (argument.Parameter.Name)
                {
                    case IsEnabledByDefaultParameterName:
                        if (argument.Value.ConstantValue.HasValue)
                        {
                            isEnabledByDefault = (bool)argument.Value.ConstantValue.Value;
                        }

                        break;

                    case DefaultSeverityParameterName:
                        if (argument.Value is IFieldReferenceOperation fieldReference &&
                            fieldReference.Field.ContainingType.Equals(diagnosticSeverityType) &&
                            Enum.TryParse(fieldReference.Field.Name, out DiagnosticSeverity parsedSeverity))
                        {
                            defaultSeverity = parsedSeverity;
                        }

                        break;

                    case RuleLevelParameterName:
                        if (ruleLevelType != null &&
                            argument.Value is IFieldReferenceOperation fieldReference2 &&
                            fieldReference2.Field.ContainingType.Equals(ruleLevelType) &&
                            Enum.TryParse(fieldReference2.Field.Name, out RuleLevel parsedRuleLevel))
                        {
                            switch (parsedRuleLevel)
                            {
                                case RuleLevel.BuildWarning:
                                    defaultSeverity = DiagnosticSeverity.Warning;
                                    isEnabledByDefault = true;
                                    break;

                                case RuleLevel.IdeSuggestion:
                                    defaultSeverity = DiagnosticSeverity.Info;
                                    isEnabledByDefault = true;
                                    break;

                                case RuleLevel.IdeHidden_BulkConfigurable:
                                    defaultSeverity = DiagnosticSeverity.Hidden;
                                    isEnabledByDefault = true;
                                    break;

                                case RuleLevel.Disabled:
                                case RuleLevel.CandidateForRemoval:
                                    isEnabledByDefault = false;
                                    break;
                            }

                            return (isEnabledByDefault, defaultSeverity);
                        }

                        break;
                }
            }

            if (isEnabledByDefault == false)
            {
                defaultSeverity = null;
            }

            return (isEnabledByDefault, defaultSeverity);
        }

        private static void AnalyzeRuleId(
            OperationAnalysisContext operationAnalysisContext,
            ImmutableArray<IArgumentOperation> creationArguments,
            bool isAnalyzerReleaseTracking,
            ReleaseTrackingData? shippedData,
            ReleaseTrackingData? unshippedData,
            PooledConcurrentSet<string> seenRuleIds,
            AdditionalText? diagnosticCategoryAndIdRangeTextOpt,
            string? category,
            string analyzerName,
            string? helpLink,
            bool? isEnabledByDefault,
            DiagnosticSeverity? defaultSeverity,
            ImmutableArray<(string? prefix, int start, int end)> allowedIdsInfoListOpt,
            ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<Location>>> idToAnalyzerMap)
        {
            var analyzer = ((IFieldSymbol)operationAnalysisContext.ContainingSymbol).ContainingType.OriginalDefinition;
            string? ruleId = null;
            foreach (var argument in creationArguments)
            {
                if (argument.Parameter.Name.Equals(DiagnosticIdParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    // Check if diagnostic ID is a constant string.
                    if (argument.Value.ConstantValue.HasValue &&
                        argument.Value.Type != null &&
                        argument.Value.Type.SpecialType == SpecialType.System_String)
                    {
                        ruleId = (string)argument.Value.ConstantValue.Value;
                        seenRuleIds.Add(ruleId);

                        var location = argument.Value.Syntax.GetLocation();
                        static string GetAnalyzerName(INamedTypeSymbol a) => a.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                        // Factory methods to track declaration locations for every analyzer rule ID.
                        ConcurrentBag<Location> AddLocationFactory(string analyzerName)
                            => new ConcurrentBag<Location> { location };

                        ConcurrentBag<Location> UpdateLocationsFactory(string analyzerName, ConcurrentBag<Location> bag)
                        {
                            bag.Add(location);
                            return bag;
                        };

                        ConcurrentDictionary<string, ConcurrentBag<Location>> AddNamedTypeFactory(string r)
                        {
                            var dict = new ConcurrentDictionary<string, ConcurrentBag<Location>>();
                            dict.AddOrUpdate(
                                key: GetAnalyzerName(analyzer),
                                addValueFactory: AddLocationFactory,
                                updateValueFactory: UpdateLocationsFactory);
                            return dict;
                        };

                        ConcurrentDictionary<string, ConcurrentBag<Location>> UpdateNamedTypeFactory(string r, ConcurrentDictionary<string, ConcurrentBag<Location>> existingValue)
                        {
                            existingValue.AddOrUpdate(
                                key: GetAnalyzerName(analyzer),
                                addValueFactory: AddLocationFactory,
                                updateValueFactory: UpdateLocationsFactory);
                            return existingValue;
                        };

                        idToAnalyzerMap.AddOrUpdate(
                            key: ruleId,
                            addValueFactory: AddNamedTypeFactory,
                            updateValueFactory: UpdateNamedTypeFactory);

                        if (IsReservedDiagnosticId(ruleId, operationAnalysisContext.Compilation.AssemblyName))
                        {
                            operationAnalysisContext.ReportDiagnostic(argument.Value.Syntax.CreateDiagnostic(DoNotUseReservedDiagnosticIdRule, ruleId));
                        }

                        // If we have an additional file specifying required range and/or format for the ID, validate the ID.
                        if (!allowedIdsInfoListOpt.IsDefault)
                        {
                            AnalyzeAllowedIdsInfoList(ruleId, argument, diagnosticCategoryAndIdRangeTextOpt, category, allowedIdsInfoListOpt, operationAnalysisContext.ReportDiagnostic);
                        }

                        // If we have an additional file specifying required range and/or format for the ID, validate the ID.
                        if (isAnalyzerReleaseTracking)
                        {
                            RoslynDebug.Assert(shippedData != null);
                            RoslynDebug.Assert(unshippedData != null);

                            AnalyzeAnalyzerReleases(ruleId, argument, category, analyzerName, helpLink, isEnabledByDefault,
                                defaultSeverity, shippedData, unshippedData, operationAnalysisContext.ReportDiagnostic);
                        }
                        else if (shippedData == null && unshippedData == null)
                        {
                            var diagnostic = argument.CreateDiagnostic(EnableAnalyzerReleaseTrackingRule, ruleId);
                            operationAnalysisContext.ReportDiagnostic(diagnostic);
                        }
                    }
                    else
                    {
                        // Diagnostic Id for rule '{0}' must be a non-null constant.
                        string arg1 = ((IFieldInitializerOperation)operationAnalysisContext.Operation).InitializedFields.Single().Name;
                        var diagnostic = Diagnostic.Create(DiagnosticIdMustBeAConstantRule, argument.Value.Syntax.GetLocation(), arg1);
                        operationAnalysisContext.ReportDiagnostic(diagnostic);
                    }

                    return;
                }
            }
        }

        private static bool IsReservedDiagnosticId(string ruleId, string assemblyName)
        {
            if (ruleId.Length < 3)
            {
                return false;
            }

            var isCARule = ruleId.StartsWith("CA", StringComparison.Ordinal);

            if (!isCARule &&
                !ruleId.StartsWith("CS", StringComparison.Ordinal) &&
                !ruleId.StartsWith("BC", StringComparison.Ordinal))
            {
                return false;
            }

            if (!ruleId.Substring(2).All(c => char.IsDigit(c)))
            {
                return false;
            }

            return !isCARule || !CADiagnosticIdAllowedAssemblies.Contains(assemblyName);
        }
    }
}
