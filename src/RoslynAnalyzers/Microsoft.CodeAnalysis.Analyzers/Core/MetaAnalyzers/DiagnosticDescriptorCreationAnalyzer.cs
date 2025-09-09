// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Helpers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.ReleaseTracking;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;
    using PooledFieldToCustomTagsConcurrentDictionary = PooledConcurrentDictionary<IFieldSymbol, ImmutableArray<string>>;
    using PooledFieldToResourceNameAndFileNameConcurrentDictionary = PooledConcurrentDictionary<IFieldSymbol, (string nameOfResource, string resourceFileName)>;
    using PooledLocalizabeStringsConcurrentDictionary = PooledConcurrentDictionary<INamedTypeSymbol, PooledConcurrentSet<(IFieldSymbol field, IArgumentOperation argument)>>;
    using PooledResourcesDataValueConcurrentDictionary = PooledConcurrentDictionary<string, ImmutableDictionary<string, (string value, Location location)>>;

    /// <summary>
    /// RS1007 <inheritdoc cref="UseLocalizableStringsInDescriptorTitle"/>
    /// RS1015 <inheritdoc cref="ProvideHelpUriInDescriptorTitle"/>
    /// RS1017 <inheritdoc cref="DiagnosticIdMustBeAConstantTitle"/>
    /// RS1019 <inheritdoc cref="UseUniqueDiagnosticIdTitle"/>
    /// RS1028 <inheritdoc cref="ProvideCustomTagsInDescriptorTitle"/>
    /// RS1029 <inheritdoc cref="DoNotUseReservedDiagnosticIdTitle"/>
    /// RS1031 <inheritdoc cref="DefineDiagnosticTitleCorrectlyTitle"/>
    /// RS1032 <inheritdoc cref="DefineDiagnosticMessageCorrectlyTitle"/>
    /// RS1033 <inheritdoc cref="DefineDiagnosticDescriptionCorrectlyTitle"/>
    /// </summary>
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
        private const string CompilationEndWellKnownDiagnosticTag = "CompilationEnd" /*WellKnownDiagnosticTags.CompilationEnd*/;

        internal const string DefineDescriptorArgumentCorrectlyFixValue = nameof(DefineDescriptorArgumentCorrectlyFixValue);
        private const string DefineDescriptorArgumentCorrectlyFixAdditionalDocumentLocationInfo = nameof(DefineDescriptorArgumentCorrectlyFixAdditionalDocumentLocationInfo);
        private const string AdditionalDocumentLocationInfoSeparator = ";;";

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

        public static readonly DiagnosticDescriptor UseLocalizableStringsInDescriptorRule = new(
            DiagnosticIds.UseLocalizableStringsInDescriptorRuleId,
            CreateLocalizableResourceString(nameof(UseLocalizableStringsInDescriptorTitle)),
            CreateLocalizableResourceString(nameof(UseLocalizableStringsInDescriptorMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisLocalization,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(UseLocalizableStringsInDescriptorDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor ProvideHelpUriInDescriptorRule = new(
            DiagnosticIds.ProvideHelpUriInDescriptorRuleId,
            CreateLocalizableResourceString(nameof(ProvideHelpUriInDescriptorTitle)),
            CreateLocalizableResourceString(nameof(ProvideHelpUriInDescriptorMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDocumentation,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(ProvideHelpUriInDescriptorDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor DiagnosticIdMustBeAConstantRule = new(
            DiagnosticIds.DiagnosticIdMustBeAConstantRuleId,
            CreateLocalizableResourceString(nameof(DiagnosticIdMustBeAConstantTitle)),
            CreateLocalizableResourceString(nameof(DiagnosticIdMustBeAConstantMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DiagnosticIdMustBeAConstantDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor UseUniqueDiagnosticIdRule = new(
            DiagnosticIds.UseUniqueDiagnosticIdRuleId,
            CreateLocalizableResourceString(nameof(UseUniqueDiagnosticIdTitle)),
            CreateLocalizableResourceString(nameof(UseUniqueDiagnosticIdMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(UseUniqueDiagnosticIdDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        public static readonly DiagnosticDescriptor ProvideCustomTagsInDescriptorRule = new(
            DiagnosticIds.ProvideCustomTagsInDescriptorRuleId,
            CreateLocalizableResourceString(nameof(ProvideCustomTagsInDescriptorTitle)),
            CreateLocalizableResourceString(nameof(ProvideCustomTagsInDescriptorMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDocumentation,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(ProvideCustomTagsInDescriptorDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor DoNotUseReservedDiagnosticIdRule = new(
            DiagnosticIds.DoNotUseReservedDiagnosticIdRuleId,
            CreateLocalizableResourceString(nameof(DoNotUseReservedDiagnosticIdTitle)),
            CreateLocalizableResourceString(nameof(DoNotUseReservedDiagnosticIdMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotUseReservedDiagnosticIdDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor DefineDiagnosticTitleCorrectlyRule = new(
            DiagnosticIds.DefineDiagnosticTitleCorrectlyRuleId,
            CreateLocalizableResourceString(nameof(DefineDiagnosticTitleCorrectlyTitle)),
            CreateLocalizableResourceString(nameof(DefineDiagnosticTitleCorrectlyMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor DefineDiagnosticMessageCorrectlyRule = new(
            DiagnosticIds.DefineDiagnosticMessageCorrectlyRuleId,
            CreateLocalizableResourceString(nameof(DefineDiagnosticMessageCorrectlyTitle)),
            CreateLocalizableResourceString(nameof(DefineDiagnosticMessageCorrectlyMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor DefineDiagnosticDescriptionCorrectlyRule = new(
            DiagnosticIds.DefineDiagnosticDescriptionCorrectlyRuleId,
            CreateLocalizableResourceString(nameof(DefineDiagnosticDescriptionCorrectlyTitle)),
            CreateLocalizableResourceString(nameof(DefineDiagnosticDescriptionCorrectlyMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor AddCompilationEndCustomTagRule = new(
            DiagnosticIds.AddCompilationEndCustomTagRuleId,
            CreateLocalizableResourceString(nameof(AddCompilationEndCustomTagTitle)),
            CreateLocalizableResourceString(nameof(AddCompilationEndCustomTagMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(AddCompilationEndCustomTagDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
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
            EnableAnalyzerReleaseTrackingRule,
            DefineDiagnosticTitleCorrectlyRule,
            DefineDiagnosticMessageCorrectlyRule,
            DefineDiagnosticDescriptionCorrectlyRule,
            AddCompilationEndCustomTagRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                if (!compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticDescriptor, out var diagnosticDescriptorType) ||
                    !compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisLocalizableString, out var localizableResourceType) ||
                    !compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisLocalizableResourceString, out var localizableResourceStringType) ||
                    !compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsCompilationEndAnalysisContext, out var compilationEndContextType) ||
                    !compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnostic, out var diagnosticType))
                {
                    return;
                }

                // Try read the additional file containing the allowed categories, and corresponding ID ranges.
                var checkCategoryAndAllowedIds = TryGetCategoryAndAllowedIdsMap(
                    compilationContext.Options.AdditionalFiles,
                    compilationContext.CancellationToken,
                    out AdditionalText? diagnosticCategoryAndIdRangeText,
                    out ImmutableDictionary<string, ImmutableArray<(string? prefix, int start, int end)>>? categoryAndAllowedIdsMap,
                    out List<Diagnostic>? invalidFileDiagnostics);

                // Try read the additional files containing the shipped and unshipped analyzer releases.
                var isAnalyzerReleaseTracking = TryGetReleaseTrackingData(
                    compilationContext.Options.AdditionalFiles,
                    compilationContext.CancellationToken,
                    out var shippedData,
                    out var unshippedData,
                    out List<Diagnostic>? invalidReleaseFileEntryDiagnostics);

                PooledLocalizabeStringsConcurrentDictionary? localizableTitles = null;
                PooledLocalizabeStringsConcurrentDictionary? localizableMessages = null;
                PooledLocalizabeStringsConcurrentDictionary? localizableDescriptions = null;
                PooledResourcesDataValueConcurrentDictionary? resourcesDataValueMap = null;

                var analyzeResourceStrings = HasResxAdditionalFiles(compilationContext.Options);
                if (analyzeResourceStrings)
                {
                    localizableTitles = PooledLocalizabeStringsConcurrentDictionary.GetInstance();
                    localizableMessages = PooledLocalizabeStringsConcurrentDictionary.GetInstance();
                    localizableDescriptions = PooledLocalizabeStringsConcurrentDictionary.GetInstance();
                    resourcesDataValueMap = PooledResourcesDataValueConcurrentDictionary.GetInstance();
                }

                var idToAnalyzerMap = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<Location>>>();
                var seenRuleIds = PooledConcurrentSet<string>.GetInstance();
                var customTagsMap = PooledFieldToCustomTagsConcurrentDictionary.GetInstance(SymbolEqualityComparer.Default);
                compilationContext.RegisterOperationAction(operationAnalysisContext =>
                {
                    var fieldInitializer = (IFieldInitializerOperation)operationAnalysisContext.Operation;
                    if (!TryGetDescriptorCreateMethodAndArguments(fieldInitializer, diagnosticDescriptorType, out var creationMethod, out var creationArguments))
                    {
                        return;
                    }

                    var containingType = operationAnalysisContext.ContainingSymbol.ContainingType;
                    AnalyzeTitle(operationAnalysisContext, creationArguments, fieldInitializer, containingType,
                        localizableTitles, resourcesDataValueMap, localizableResourceType, localizableResourceStringType);
                    AnalyzeMessage(operationAnalysisContext, creationArguments, containingType,
                        localizableMessages, resourcesDataValueMap, localizableResourceType, localizableResourceStringType);
                    AnalyzeDescription(operationAnalysisContext, creationArguments, containingType,
                        localizableDescriptions, resourcesDataValueMap, localizableResourceType, localizableResourceStringType);
                    AnalyzeHelpLinkUri(operationAnalysisContext, creationArguments, out var helpLink);
                    AnalyzeCustomTags(operationAnalysisContext, creationArguments, fieldInitializer, customTagsMap);
                    var (isEnabledByDefault, defaultSeverity) = GetDefaultSeverityAndEnabledByDefault(operationAnalysisContext.Compilation, creationArguments);

                    if (!TryAnalyzeCategory(operationAnalysisContext, creationArguments, checkCategoryAndAllowedIds,
                            diagnosticCategoryAndIdRangeText, categoryAndAllowedIdsMap, out var category, out var allowedIdsInfoList))
                    {
                        allowedIdsInfoList = default;
                    }

                    var analyzerName = fieldInitializer.InitializedFields.First().ContainingType.Name;
                    AnalyzeRuleId(operationAnalysisContext, creationArguments,
                        isAnalyzerReleaseTracking, shippedData, unshippedData, seenRuleIds, diagnosticCategoryAndIdRangeText,
                        category, analyzerName, helpLink, isEnabledByDefault, defaultSeverity, allowedIdsInfoList, idToAnalyzerMap);

                }, OperationKind.FieldInitializer);

                if (analyzeResourceStrings)
                {
                    compilationContext.RegisterSymbolStartAction(context =>
                    {
                        var symbolToResourceMap = PooledFieldToResourceNameAndFileNameConcurrentDictionary.GetInstance(SymbolEqualityComparer.Default);
                        context.RegisterOperationAction(context =>
                        {
                            var fieldInitializer = (IFieldInitializerOperation)context.Operation;
                            if (TryGetLocalizableResourceStringCreation(fieldInitializer.Value, localizableResourceStringType,
                                    out var nameOfLocalizableResource, out var resourceFileName))
                            {
                                foreach (var field in fieldInitializer.InitializedFields)
                                {
                                    symbolToResourceMap.TryAdd(field, (nameOfLocalizableResource, resourceFileName));
                                }
                            }
                        }, OperationKind.FieldInitializer);

                        context.RegisterSymbolEndAction(context =>
                        {
                            RoslynDebug.Assert(localizableTitles != null);
                            RoslynDebug.Assert(localizableMessages != null);
                            RoslynDebug.Assert(localizableDescriptions != null);
                            RoslynDebug.Assert(resourcesDataValueMap != null);

                            var namedType = (INamedTypeSymbol)context.Symbol;

                            AnalyzeLocalizableStrings(localizableTitles, AnalyzeTitleCore, symbolToResourceMap, namedType,
                                resourcesDataValueMap, context.Options, context.ReportDiagnostic, context.CancellationToken);
                            AnalyzeLocalizableStrings(localizableMessages, AnalyzeMessageCore, symbolToResourceMap, namedType,
                                resourcesDataValueMap, context.Options, context.ReportDiagnostic, context.CancellationToken);
                            AnalyzeLocalizableStrings(localizableDescriptions, AnalyzeDescriptionCore, symbolToResourceMap, namedType,
                                resourcesDataValueMap, context.Options, context.ReportDiagnostic, context.CancellationToken);

                            symbolToResourceMap.Free(context.CancellationToken);
                        });
                    }, SymbolKind.NamedType);
                }

                // Flag descriptor fields that are used to report compilation end diagnostics,
                // but do not have the required 'WellKnownDiagnosticTags.CompilationEnd' custom tag.
                // See https://github.com/dotnet/roslyn-analyzers/issues/6282 for details.
                if (compilationEndContextType.GetMembers(DiagnosticWellKnownNames.ReportDiagnosticName).FirstOrDefault() is IMethodSymbol compilationEndReportDiagnosticMethod)
                {
                    var diagnosticCreateMethods = diagnosticType.GetMembers("Create").OfType<IMethodSymbol>()
                        .Where(m => m.IsPublic() && m.Parameters.Length > 0 && SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, diagnosticDescriptorType))
                        .ToImmutableHashSet(SymbolEqualityComparer.Default);
                    compilationContext.RegisterSymbolStartAction(context =>
                    {
                        var localsToDescriptorsMap = PooledConcurrentDictionary<ILocalSymbol, PooledConcurrentSet<IFieldSymbol>>.GetInstance(SymbolEqualityComparer.Default);
                        var localsUsedForCompilationEndReportDiagnostic = PooledConcurrentSet<ILocalSymbol>.GetInstance(SymbolEqualityComparer.Default);
                        var fieldsUsedForCompilationEndReportDiagnostic = PooledConcurrentSet<IFieldSymbol>.GetInstance(SymbolEqualityComparer.Default);

                        context.RegisterOperationAction(context =>
                        {
                            var invocation = (IInvocationOperation)context.Operation;
                            if (invocation.Arguments.IsEmpty)
                                return;

                            if (SymbolEqualityComparer.Default.Equals(invocation.TargetMethod, compilationEndReportDiagnosticMethod) &&
                                    invocation.Arguments[0].Value.WalkDownConversion() is ILocalReferenceOperation localReference)
                            {
                                // Code pattern such as:
                                //      var diagnostic = Diagnostic.Create(field, ...);
                                //      context.ReportDiagnostic(diagnostic);
                                localsUsedForCompilationEndReportDiagnostic.Add(localReference.Local);
                            }
                            else if (diagnosticCreateMethods.Contains(invocation.TargetMethod))
                            {
                                if (invocation.Arguments[0].Value.WalkDownConversion() is IFieldReferenceOperation fieldReference)
                                {
                                    // Code pattern such as:
                                    //      'Diagnostic.Create(field, ...)'
                                    if (invocation.GetAncestor<IInvocationOperation>(OperationKind.Invocation,
                                                inv => SymbolEqualityComparer.Default.Equals(inv.TargetMethod, compilationEndReportDiagnosticMethod)) is not null)
                                    {
                                        // Code pattern such as:
                                        //      'context.ReportDiagnostic(Diagnostic.Create(field, ...));'
                                        fieldsUsedForCompilationEndReportDiagnostic.Add(fieldReference.Field);
                                    }
                                    else
                                    {
                                        switch (invocation.Parent)
                                        {
                                            case IVariableInitializerOperation variableInitializer:
                                                // Code pattern such as:
                                                //      'var diagnostic = Diagnostic.Create(field, ...);'
                                                if (variableInitializer.GetAncestor<IVariableDeclarationOperation>(OperationKind.VariableDeclaration) is { } variableDeclaration)
                                                {
                                                    foreach (var local in variableDeclaration.GetDeclaredVariables())
                                                    {
                                                        AddToLocalsToDescriptorsMap(local, fieldReference.Field, localsToDescriptorsMap);
                                                    }
                                                }

                                                break;

                                            case ISimpleAssignmentOperation simpleAssignment:
                                                // Code pattern such as:
                                                //      'diagnostic = Diagnostic.Create(field, ...);'
                                                if (simpleAssignment.Target is ILocalReferenceOperation localReferenceTarget)
                                                {
                                                    AddToLocalsToDescriptorsMap(localReferenceTarget.Local, fieldReference.Field, localsToDescriptorsMap);
                                                }

                                                break;
                                        }
                                    }
                                }

                                static void AddToLocalsToDescriptorsMap(ILocalSymbol local, IFieldSymbol field, PooledConcurrentDictionary<ILocalSymbol, PooledConcurrentSet<IFieldSymbol>> localsToDescriptorsMap)
                                {
                                    localsToDescriptorsMap.AddOrUpdate(local,
                                        addValueFactory: _ =>
                                        {
                                            var set = PooledConcurrentSet<IFieldSymbol>.GetInstance(SymbolEqualityComparer.Default);
                                            set.Add(field);
                                            return set;
                                        },
                                        updateValueFactory: (_, fields) =>
                                        {
                                            fields.Add(field);
                                            return fields;
                                        });
                                }
                            }
                        }, OperationKind.Invocation);

                        context.RegisterSymbolEndAction(context =>
                        {
                            foreach (var local in localsUsedForCompilationEndReportDiagnostic)
                            {
                                if (localsToDescriptorsMap.TryGetValue(local, out var fields))
                                {
                                    foreach (var field in fields)
                                        AnalyzeField(field);
                                }
                            }

                            foreach (var field in fieldsUsedForCompilationEndReportDiagnostic)
                            {
                                AnalyzeField(field);
                            }

                            foreach (var value in localsToDescriptorsMap.Values)
                                value.Free(context.CancellationToken);
                            localsToDescriptorsMap.Free(context.CancellationToken);
                            localsUsedForCompilationEndReportDiagnostic.Free(context.CancellationToken);
                            fieldsUsedForCompilationEndReportDiagnostic.Free(context.CancellationToken);

                            void AnalyzeField(IFieldSymbol field)
                            {
                                if (customTagsMap.TryGetValue(field, out var customTags) &&
                                    !customTags.IsDefault &&
                                    !customTags.Contains(CompilationEndWellKnownDiagnosticTag) &&
                                    !field.Locations.IsEmpty &&
                                    field.Locations[0].IsInSource)
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(AddCompilationEndCustomTagRule, field.Locations[0], field.Name));
                                }
                            }
                        });
                    }, SymbolKind.NamedType);
                }

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

                    seenRuleIds.Free(compilationEndContext.CancellationToken);
                    if (analyzeResourceStrings)
                    {
                        RoslynDebug.Assert(localizableTitles != null);
                        RoslynDebug.Assert(localizableMessages != null);
                        RoslynDebug.Assert(localizableDescriptions != null);
                        RoslynDebug.Assert(resourcesDataValueMap != null);

                        FreeLocalizableStringsMap(localizableTitles, compilationEndContext.CancellationToken);
                        FreeLocalizableStringsMap(localizableMessages, compilationEndContext.CancellationToken);
                        FreeLocalizableStringsMap(localizableDescriptions, compilationEndContext.CancellationToken);
                        resourcesDataValueMap.Free(compilationEndContext.CancellationToken);
                    }

                    customTagsMap.Free(compilationEndContext.CancellationToken);
                });
            });

            static void FreeLocalizableStringsMap(PooledLocalizabeStringsConcurrentDictionary localizableStrings, CancellationToken cancellationToken)
            {
                foreach (var builder in localizableStrings.Values)
                {
                    builder.Free(cancellationToken);
                }

                localizableStrings.Free(cancellationToken);
            }
        }

        private static bool TryGetDescriptorCreateMethodAndArguments(
            IFieldInitializerOperation fieldInitializer,
            INamedTypeSymbol diagnosticDescriptorType,
            [NotNullWhen(returnValue: true)] out IMethodSymbol? creationMethod,
            [NotNullWhen(returnValue: true)] out ImmutableArray<IArgumentOperation> creationArguments)
        {
            (creationMethod, creationArguments) = fieldInitializer.Value.WalkDownConversion() switch
            {
                IObjectCreationOperation objectCreation when IsDescriptorConstructor(objectCreation.Constructor)
                    => (objectCreation.Constructor, objectCreation.Arguments),
                IInvocationOperation invocation when IsCreateHelper(invocation.TargetMethod)
                    => (invocation.TargetMethod, invocation.Arguments),
                _ => default
            };

            return creationMethod != null;

            bool IsDescriptorConstructor(IMethodSymbol? method)
                => SymbolEqualityComparer.Default.Equals(method?.ContainingType, diagnosticDescriptorType);

            // Heuristic to identify helper methods to create DiagnosticDescriptor:
            //  "A method invocation that returns 'DiagnosticDescriptor' and has a first string parameter named 'id'"
            bool IsCreateHelper(IMethodSymbol method)
                => SymbolEqualityComparer.Default.Equals(method.ReturnType, diagnosticDescriptorType) &&
                    !method.Parameters.IsEmpty &&
                    method.Parameters[0].Name == DiagnosticIdParameterName &&
                    method.Parameters[0].Type.SpecialType == SpecialType.System_String;
        }

        private static bool TryGetLocalizableResourceStringCreation(
            IOperation operation,
            INamedTypeSymbol localizableResourceStringType,
            [NotNullWhen(returnValue: true)] out string? nameOfLocalizableResource,
            [NotNullWhen(returnValue: true)] out string? resourceFileName)
        {
            return TryGetConstructorCreation(out nameOfLocalizableResource, out resourceFileName) ||
                TryGetHelperMethodCreation(out nameOfLocalizableResource, out resourceFileName);

            //  Local functions

            //  Attempts to get the resource and file name for the creation of a localizable resource string using the
            //  constructor on LocalizableResourceString
            bool TryGetConstructorCreation([NotNullWhen(true)] out string? nameOfLocalizableResource, [NotNullWhen(true)] out string? resourceFileName)
            {
                if (operation.WalkDownConversion() is IObjectCreationOperation objectCreation &&
                    SymbolEqualityComparer.Default.Equals(objectCreation.Constructor?.ContainingType, localizableResourceStringType) &&
                    objectCreation.Arguments.Length >= 3 &&
                    objectCreation.Arguments.GetArgumentForParameterAtIndex(0) is { } firstParamArgument &&
                    firstParamArgument.Parameter?.Type.SpecialType == SpecialType.System_String &&
                    firstParamArgument.Value.ConstantValue.HasValue &&
                    firstParamArgument.Value.ConstantValue.Value is string nameOfResource &&
                    objectCreation.Arguments.GetArgumentForParameterAtIndex(2) is { } thirdParamArgument &&
                    thirdParamArgument.Value is ITypeOfOperation typeOfOperation &&
                    typeOfOperation.TypeOperand is { } typeOfType)
                {
                    nameOfLocalizableResource = nameOfResource;
                    resourceFileName = typeOfType.Name;
                    return true;
                }

                nameOfLocalizableResource = null;
                resourceFileName = null;
                return false;
            }

            //  Attempts to get the resource and file name for the creation of a localizable resource string using a
            //  helper method on the resource class. For an operation to be considered a helper method invocation, it must
            //  - Be an invocation of a static method
            //  - Method must have return type 'LocalizableResourceString'
            //  - Method must have single 'string' parameter
            //  - Argument must be a compile-time constant (typically a nameof operation on one of the resource class's properties).
            bool TryGetHelperMethodCreation([NotNullWhen(true)] out string? nameOfLocalizableResource, [NotNullWhen(true)] out string? resourceFileName)
            {
                if (operation.WalkDownConversion() is IInvocationOperation invocation &&
                    invocation.TargetMethod.ReturnType.Equals(localizableResourceStringType) &&
                    invocation.Arguments.Length == 1 &&
                    invocation.Arguments[0].Parameter?.Type.SpecialType == SpecialType.System_String &&
                    invocation.Arguments[0].Value.ConstantValue.HasValue &&
                    invocation.Arguments[0].Value.ConstantValue.Value is string nameOfResource)
                {
                    nameOfLocalizableResource = nameOfResource;
                    resourceFileName = invocation.TargetMethod.ContainingType.Name;
                    return true;
                }

                nameOfLocalizableResource = null;
                resourceFileName = null;
                return false;
            }
        }

        private static void AnalyzeTitle(
            OperationAnalysisContext operationAnalysisContext,
            ImmutableArray<IArgumentOperation> creationArguments,
            IFieldInitializerOperation creation,
            INamedTypeSymbol containingType,
            PooledLocalizabeStringsConcurrentDictionary? localizableTitles,
            PooledResourcesDataValueConcurrentDictionary? resourceDataValueMap,
            INamedTypeSymbol localizableStringType,
            INamedTypeSymbol localizableResourceStringType)
        {
            IArgumentOperation? titleArgument = creationArguments.FirstOrDefault(a => a.Parameter?.Name.Equals("title", StringComparison.OrdinalIgnoreCase) == true);
            if (titleArgument != null)
            {
                if (titleArgument.Parameter?.Type.SpecialType == SpecialType.System_String)
                {
                    operationAnalysisContext.ReportDiagnostic(creation.Value.CreateDiagnostic(UseLocalizableStringsInDescriptorRule, WellKnownTypeNames.MicrosoftCodeAnalysisLocalizableString));
                }

                AnalyzeDescriptorArgument(operationAnalysisContext, titleArgument,
                    AnalyzeTitleCore, containingType, localizableTitles, resourceDataValueMap,
                    localizableStringType, localizableResourceStringType);
            }
        }

        private static void AnalyzeTitleCore(string title, IArgumentOperation argumentOperation, Location fixLocation, Action<Diagnostic> reportDiagnostic)
        {
            var hasLeadingOrTrailingWhitespaces = HasLeadingOrTrailingWhitespaces(title);
            if (hasLeadingOrTrailingWhitespaces)
            {
                title = RemoveLeadingAndTrailingWhitespaces(title);
            }

            var isMultiSentences = IsMultiSentences(title);
            var endsWithPeriod = EndsWithPeriod(title);
            var containsLineReturn = ContainsLineReturn(title);

            if (isMultiSentences || endsWithPeriod || containsLineReturn || hasLeadingOrTrailingWhitespaces)
            {
                // Leading and trailing spaces were already fixed
                var fixedTitle = endsWithPeriod ? RemoveTrailingPeriod(title) : title;
                fixedTitle = isMultiSentences ? FixMultiSentences(fixedTitle) : fixedTitle;
                fixedTitle = containsLineReturn ? FixLineReturns(fixedTitle, allowMultisentences: false) : fixedTitle;

                ReportDefineDiagnosticArgumentCorrectlyDiagnostic(DefineDiagnosticTitleCorrectlyRule,
                    argumentOperation, fixedTitle, fixLocation, reportDiagnostic);
            }
        }

        private static void ReportDefineDiagnosticArgumentCorrectlyDiagnostic(
            DiagnosticDescriptor descriptor,
            IArgumentOperation argumentOperation,
            string fixValue,
            Location fixLocation,
            Action<Diagnostic> reportDiagnostic)
        {
            // Additional location in an additional document does not seem to be preserved
            // from analyzer to code fix due to a Roslyn bug: https://github.com/dotnet/roslyn/issues/46377
            // We workaround this bug by passing additional document file path and location span as strings.

            var additionalLocations = ImmutableArray<Location>.Empty;
            var properties = ImmutableDictionary<string, string?>.Empty.Add(DefineDescriptorArgumentCorrectlyFixValue, fixValue);
            if (fixLocation.IsInSource)
            {
                additionalLocations = additionalLocations.Add(fixLocation);
            }
            else
            {
                var span = fixLocation.SourceSpan;
                properties = properties.Add(DefineDescriptorArgumentCorrectlyFixAdditionalDocumentLocationInfo, $"{span.Start}{AdditionalDocumentLocationInfoSeparator}{span.Length}{AdditionalDocumentLocationInfoSeparator}{fixLocation.GetLineSpan().Path}");
            }

            reportDiagnostic(argumentOperation.CreateDiagnostic(descriptor, additionalLocations, properties));
        }

        internal static bool TryGetAdditionalDocumentLocationInfo(Diagnostic diagnostic,
            [NotNullWhen(returnValue: true)] out string? filePath,
            [NotNullWhen(returnValue: true)] out TextSpan? fileSpan)
        {
            Debug.Assert(diagnostic.Id is DiagnosticIds.DefineDiagnosticTitleCorrectlyRuleId or
                DiagnosticIds.DefineDiagnosticMessageCorrectlyRuleId or
                DiagnosticIds.DefineDiagnosticDescriptionCorrectlyRuleId);

            filePath = null;
            fileSpan = null;
            if (!diagnostic.Properties.TryGetValue(DefineDescriptorArgumentCorrectlyFixAdditionalDocumentLocationInfo, out var locationInfo)
                || locationInfo is null)
            {
                return false;
            }

            var parts = locationInfo.Split([AdditionalDocumentLocationInfoSeparator], StringSplitOptions.None);
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], out var spanSpart) ||
                !int.TryParse(parts[1], out var spanLength))
            {
                return false;
            }

            fileSpan = new TextSpan(spanSpart, spanLength);
            filePath = parts[2];
            return !string.IsNullOrEmpty(filePath);
        }

        private static void AnalyzeMessage(
            OperationAnalysisContext operationAnalysisContext,
            ImmutableArray<IArgumentOperation> creationArguments,
            INamedTypeSymbol containingType,
            PooledLocalizabeStringsConcurrentDictionary? localizableMessages,
            PooledResourcesDataValueConcurrentDictionary? resourceDataValueMap,
            INamedTypeSymbol localizableStringType,
            INamedTypeSymbol localizableResourceStringType)
        {
            var messageArgument = creationArguments.FirstOrDefault(a => a.Parameter?.Name.Equals("messageFormat", StringComparison.OrdinalIgnoreCase) == true);
            if (messageArgument != null)
            {
                AnalyzeDescriptorArgument(operationAnalysisContext, messageArgument,
                    AnalyzeMessageCore, containingType, localizableMessages, resourceDataValueMap,
                    localizableStringType, localizableResourceStringType);
            }
        }

        private static void AnalyzeMessageCore(string message, IArgumentOperation argumentOperation, Location fixLocation, Action<Diagnostic> reportDiagnostic)
        {
            var hasLeadingOrTrailingWhitespaces = HasLeadingOrTrailingWhitespaces(message);
            if (hasLeadingOrTrailingWhitespaces)
            {
                message = RemoveLeadingAndTrailingWhitespaces(message);
            }

            var isMultiSentences = IsMultiSentences(message);
            var endsWithPeriod = EndsWithPeriod(message);
            var containsLineReturn = ContainsLineReturn(message);

            if (isMultiSentences ^ endsWithPeriod || containsLineReturn || hasLeadingOrTrailingWhitespaces)
            {
                // Leading and trailing spaces were already fixed
                var fixedMessage = containsLineReturn ? FixLineReturns(message, allowMultisentences: true) : message;
                isMultiSentences = IsMultiSentences(fixedMessage);
                endsWithPeriod = EndsWithPeriod(fixedMessage);

                if (isMultiSentences ^ endsWithPeriod)
                {
                    fixedMessage = endsWithPeriod ? RemoveTrailingPeriod(fixedMessage) : fixedMessage + ".";
                }

                ReportDefineDiagnosticArgumentCorrectlyDiagnostic(DefineDiagnosticMessageCorrectlyRule,
                    argumentOperation, fixedMessage, fixLocation, reportDiagnostic);
            }
        }

        private static void AnalyzeDescription(
            OperationAnalysisContext operationAnalysisContext,
            ImmutableArray<IArgumentOperation> creationArguments,
            INamedTypeSymbol containingType,
            PooledLocalizabeStringsConcurrentDictionary? localizableDescriptions,
            PooledResourcesDataValueConcurrentDictionary? resourceDataValueMap,
            INamedTypeSymbol localizableStringType,
            INamedTypeSymbol localizableResourceStringType)
        {
            IArgumentOperation? descriptionArgument = creationArguments.FirstOrDefault(a => a.Parameter?.Name.Equals("description", StringComparison.OrdinalIgnoreCase) == true);
            if (descriptionArgument != null)
            {
                AnalyzeDescriptorArgument(operationAnalysisContext, descriptionArgument,
                    AnalyzeDescriptionCore, containingType, localizableDescriptions, resourceDataValueMap,
                    localizableStringType, localizableResourceStringType);
            }
        }

        private static void AnalyzeDescriptionCore(string description, IArgumentOperation argumentOperation, Location fixLocation, Action<Diagnostic> reportDiagnostic)
        {
            var hasLeadingOrTrailingWhitespaces = HasLeadingOrTrailingWhitespaces(description);
            if (hasLeadingOrTrailingWhitespaces)
            {
                description = RemoveLeadingAndTrailingWhitespaces(description);
            }

            var endsWithPunctuation = EndsWithPunctuation(description);

            if (!endsWithPunctuation || hasLeadingOrTrailingWhitespaces)
            {
                var fixedDescription = !endsWithPunctuation ? description + "." : description;

                ReportDefineDiagnosticArgumentCorrectlyDiagnostic(DefineDiagnosticDescriptionCorrectlyRule,
                    argumentOperation, fixedDescription, fixLocation, reportDiagnostic);
            }
        }

        private static void AnalyzeDescriptorArgument(
            OperationAnalysisContext operationAnalysisContext,
            IArgumentOperation argument,
            Action<string, IArgumentOperation, Location, Action<Diagnostic>> analyzeStringValueCore,
            INamedTypeSymbol containingType,
            PooledLocalizabeStringsConcurrentDictionary? localizableStringsMap,
            PooledResourcesDataValueConcurrentDictionary? resourceDataValueMap,
            INamedTypeSymbol localizableStringType,
            INamedTypeSymbol localizableResourceStringType)
        {
            if (TryGetNonEmptyConstantStringValue(argument, out var argumentValue, out var argumentValueLocation))
            {
                analyzeStringValueCore(argumentValue, argument, argumentValueLocation, operationAnalysisContext.ReportDiagnostic);
            }
            else if (localizableStringsMap != null &&
                SymbolEqualityComparer.Default.Equals(argument.Parameter?.Type, localizableStringType))
            {
                RoslynDebug.Assert(resourceDataValueMap != null);

                if (TryGetLocalizableResourceStringCreation(argument.Value, localizableResourceStringType,
                        out var nameOfLocalizableResource, out var resourceFileName))
                {
                    AnalyzeLocalizableDescriptorArgument(analyzeStringValueCore, nameOfLocalizableResource, resourceFileName,
                        argument, resourceDataValueMap, operationAnalysisContext.Options,
                        operationAnalysisContext.ReportDiagnostic, operationAnalysisContext.CancellationToken);
                }
                else
                {
                    var value = argument.Value.WalkDownConversion();
                    if (value is IFieldReferenceOperation fieldReference &&
                        fieldReference.Field.Type.DerivesFrom(localizableStringType, baseTypesOnly: true))
                    {
                        var builder = localizableStringsMap.GetOrAdd(containingType, _ => PooledConcurrentSet<(IFieldSymbol, IArgumentOperation)>.GetInstance());
                        builder.Add((fieldReference.Field, argument));
                    }
                }
            }
        }

        private static void AnalyzeLocalizableDescriptorArgument(
            Action<string, IArgumentOperation, Location, Action<Diagnostic>> analyzeStringValueCore,
            string nameOfLocalizableResource,
            string resourceFileName,
            IArgumentOperation argument,
            PooledResourcesDataValueConcurrentDictionary resourceDataValueMap,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken)
        {
            var map = GetOrCreateResourceMap(options, resourceFileName, resourceDataValueMap, cancellationToken);
            if (map.TryGetValue(nameOfLocalizableResource, out var resourceStringTuple))
            {
                analyzeStringValueCore(resourceStringTuple.value, argument, resourceStringTuple.location, reportDiagnostic);
            }
        }

        private static void AnalyzeLocalizableStrings(
            PooledLocalizabeStringsConcurrentDictionary localizableStringsMap,
            Action<string, IArgumentOperation, Location, Action<Diagnostic>> analyzeLocalizableStringValueCore,
            PooledFieldToResourceNameAndFileNameConcurrentDictionary symbolToResourceMap,
            INamedTypeSymbol namedType,
            PooledResourcesDataValueConcurrentDictionary resourceDataValueMap,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken)
        {
            if (localizableStringsMap.TryRemove(namedType, out var localizableFieldsWithOriginalArguments))
            {
                foreach (var (field, argument) in localizableFieldsWithOriginalArguments)
                {
                    if (symbolToResourceMap.TryGetValue(field, out var resourceTuple))
                    {
                        AnalyzeLocalizableDescriptorArgument(analyzeLocalizableStringValueCore, resourceTuple.nameOfResource, resourceTuple.resourceFileName,
                            argument, resourceDataValueMap, options, reportDiagnostic, cancellationToken);
                    }
                }

                localizableFieldsWithOriginalArguments.Dispose();
            }
        }

        private static bool TryGetNonEmptyConstantStringValue(
            IArgumentOperation argumentOperation,
            [NotNullWhen(true)] out string? value,
            [NotNullWhen(true)] out Location? valueLocation)
        {
            value = null;
            valueLocation = null;

            IOperation valueOperation;
            var argumentValueOperation = argumentOperation.Value.WalkDownConversion();
            if (argumentValueOperation is ILiteralOperation literalOperation)
            {
                valueOperation = literalOperation;
            }
            else if (argumentValueOperation is IFieldReferenceOperation fieldReferenceOperation &&
                fieldReferenceOperation.Syntax.SyntaxTree == argumentValueOperation.Syntax.SyntaxTree &&
                fieldReferenceOperation.Field.DeclaringSyntaxReferences.Length == 1 &&
                fieldReferenceOperation.Field.DeclaringSyntaxReferences[0].GetSyntax() is { } fieldDeclaration &&
                fieldDeclaration.SyntaxTree == argumentValueOperation.Syntax.SyntaxTree &&
                GetFieldInitializer(fieldDeclaration, argumentValueOperation.SemanticModel!) is { } fieldInitializer &&
                fieldInitializer.Value.WalkDownConversion() is ILiteralOperation fieldInitializerLiteral)
            {
                valueOperation = fieldInitializerLiteral;
            }
            else
            {
                valueOperation = argumentValueOperation;
            }

            if (!TryGetNonEmptyConstantStringValueCore(valueOperation, out var literalValue))
            {
                return false;
            }

            value = literalValue;
            valueLocation = valueOperation.Syntax.GetLocation();
            return true;

            static IFieldInitializerOperation? GetFieldInitializer(SyntaxNode fieldDeclaration, SemanticModel model)
            {
                if (fieldDeclaration.Language == LanguageNames.VisualBasic)
                {
                    // For VB, the field initializer is on the parent node.
                    fieldDeclaration = fieldDeclaration.Parent!;
                }

                foreach (var node in fieldDeclaration.DescendantNodes())
                {
                    if (model.GetOperation(node) is IFieldInitializerOperation initializer)
                    {
                        return initializer;
                    }
                }

                return null;
            }
        }

        private static bool TryGetNonEmptyConstantStringValueCore(IOperation operation, [NotNullWhen(returnValue: true)] out string? literalValue)
        {
            if (operation.ConstantValue.HasValue &&
                operation.ConstantValue.Value is string value &&
                !string.IsNullOrEmpty(value))
            {
                literalValue = value;
                return true;
            }

            literalValue = null;
            return false;
        }

        // Assumes that a string is a multi-sentences if it contains a period followed by a whitespace ('. ').
        private const string MultiSentenceSeparator = ". ";

        private static bool IsMultiSentences(string s)
            => s.Contains(MultiSentenceSeparator);

        private static string FixMultiSentences(string s)
        {
            Debug.Assert(IsMultiSentences(s));
            var index = s.IndexOf(MultiSentenceSeparator, StringComparison.OrdinalIgnoreCase);
            return s[..index];
        }

        private static bool EndsWithPeriod(string s)
            => s[^1] == '.';

        private static string RemoveTrailingPeriod(string s)
        {
            Debug.Assert(EndsWithPeriod(s));
            return s[0..^1];
        }

        private static bool ContainsLineReturn(string s)
            => s.Contains("\r") || s.Contains("\n");

        private static string FixLineReturns(string s, bool allowMultisentences)
        {
            Debug.Assert(ContainsLineReturn(s));

            var parts = s.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
            if (!allowMultisentences)
            {
                return parts[0];
            }

            var builder = new StringBuilder();
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (!EndsWithPeriod(part))
                {
                    part += ".";
                }

                if (part.TrimEnd().Length == part.Length &&
                    i < parts.Length - 1)
                {
                    part += " ";
                }

                builder.Append(part);
            }

            return builder.ToString();
        }

        private static bool EndsWithPunctuation(string s)
        {
            var lastChar = s[^1];

            return lastChar.Equals('.') || lastChar.Equals('!') || lastChar.Equals('?');
        }

        private static bool HasLeadingOrTrailingWhitespaces(string s)
            => s.Trim().Length != s.Length;

        private static string RemoveLeadingAndTrailingWhitespaces(string s)
        {
            Debug.Assert(HasLeadingOrTrailingWhitespaces(s));
            return s.Trim();
        }

        private static void AnalyzeHelpLinkUri(
            OperationAnalysisContext operationAnalysisContext,
            ImmutableArray<IArgumentOperation> creationArguments,
            out string? helpLink)
        {
            helpLink = null;

            // Find the matching argument for helpLinkUri
            foreach (var argument in creationArguments)
            {
                if (argument.Parameter?.Name.Equals(HelpLinkUriParameterName, StringComparison.OrdinalIgnoreCase) == true)
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

        private static void AnalyzeCustomTags(
            OperationAnalysisContext operationAnalysisContext,
            ImmutableArray<IArgumentOperation> creationArguments,
            IFieldInitializerOperation fieldInitializerOperation,
            PooledFieldToCustomTagsConcurrentDictionary customTagsMap)
        {
            // Default to indicate unknown set of custom tags.
            ImmutableArray<string> customTags = default;

            try
            {
                // Find the matching argument for customTags
                var argument = creationArguments.FirstOrDefault(
                    a => a.Parameter?.Name.Equals(CustomTagsParameterName, StringComparison.OrdinalIgnoreCase) == true);
                if (argument is null ||
                    argument.Value is not IArrayCreationOperation arrayCreation ||
                    arrayCreation.DimensionSizes.Length != 1)
                {
                    return;
                }

                if (arrayCreation.DimensionSizes[0].ConstantValue.HasValue &&
                    arrayCreation.DimensionSizes[0].ConstantValue.Value is int size &&
                    size == 0)
                {
                    Diagnostic diagnostic = argument.CreateDiagnostic(ProvideCustomTagsInDescriptorRule);
                    operationAnalysisContext.ReportDiagnostic(diagnostic);

                    customTags = ImmutableArray<string>.Empty;
                }
                else if (arrayCreation.Initializer is IArrayInitializerOperation arrayInitializer &&
                    arrayInitializer.ElementValues.All(element => element.ConstantValue.HasValue && element.ConstantValue.Value is string))
                {
                    customTags = arrayInitializer.ElementValues.SelectAsArray(element => (string)element.ConstantValue.Value!);
                }
            }
            finally
            {
                AddCustomTags(customTags, fieldInitializerOperation, customTagsMap);
            }

            static void AddCustomTags(
                ImmutableArray<string> customTags,
                IFieldInitializerOperation fieldInitializerOperation,
                PooledFieldToCustomTagsConcurrentDictionary customTagsMap)
            {
                foreach (var field in fieldInitializerOperation.InitializedFields)
                {
                    customTagsMap[field] = customTags;
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
                switch (argument.Parameter?.Name)
                {
                    case IsEnabledByDefaultParameterName:
                        if (argument.Value.ConstantValue.HasValue &&
                            argument.Value.ConstantValue.Value is bool value)
                        {
                            isEnabledByDefault = value;
                        }

                        break;

                    case DefaultSeverityParameterName:
                        if (argument.Value is IFieldReferenceOperation fieldReference &&
                            SymbolEqualityComparer.Default.Equals(fieldReference.Field.ContainingType, diagnosticSeverityType) &&
                            Enum.TryParse(fieldReference.Field.Name, out DiagnosticSeverity parsedSeverity))
                        {
                            defaultSeverity = parsedSeverity;
                        }

                        break;

                    case RuleLevelParameterName:
                        if (ruleLevelType != null &&
                            argument.Value is IFieldReferenceOperation fieldReference2 &&
                            SymbolEqualityComparer.Default.Equals(fieldReference2.Field.ContainingType, ruleLevelType) &&
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
            AdditionalText? diagnosticCategoryAndIdRangeText,
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
                if (argument.Parameter?.Name.Equals(DiagnosticIdParameterName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Check if diagnostic ID is a constant string.
                    if (argument.Value.ConstantValue.HasValue &&
                        argument.Value.Type != null &&
                        argument.Value.Type.SpecialType == SpecialType.System_String &&
                        argument.Value.ConstantValue.Value is string value)
                    {
                        ruleId = value;
                        seenRuleIds.Add(ruleId);

                        var location = argument.Value.Syntax.GetLocation();
                        static string GetAnalyzerName(INamedTypeSymbol a) => a.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                        // Factory methods to track declaration locations for every analyzer rule ID.
                        ConcurrentBag<Location> AddLocationFactory(string analyzerName)
                            => [location];

                        ConcurrentBag<Location> UpdateLocationsFactory(string analyzerName, ConcurrentBag<Location> bag)
                        {
                            bag.Add(location);
                            return bag;
                        }

                        ConcurrentDictionary<string, ConcurrentBag<Location>> AddNamedTypeFactory(string r)
                        {
                            var dict = new ConcurrentDictionary<string, ConcurrentBag<Location>>();
                            dict.AddOrUpdate(
                                key: GetAnalyzerName(analyzer),
                                addValueFactory: AddLocationFactory,
                                updateValueFactory: UpdateLocationsFactory);
                            return dict;
                        }

                        ConcurrentDictionary<string, ConcurrentBag<Location>> UpdateNamedTypeFactory(string r, ConcurrentDictionary<string, ConcurrentBag<Location>> existingValue)
                        {
                            existingValue.AddOrUpdate(
                                key: GetAnalyzerName(analyzer),
                                addValueFactory: AddLocationFactory,
                                updateValueFactory: UpdateLocationsFactory);
                            return existingValue;
                        }

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
                            AnalyzeAllowedIdsInfoList(ruleId, argument, diagnosticCategoryAndIdRangeText, category, allowedIdsInfoListOpt, operationAnalysisContext.ReportDiagnostic);
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
                        var diagnostic = argument.Value.CreateDiagnostic(DiagnosticIdMustBeAConstantRule, arg1);
                        operationAnalysisContext.ReportDiagnostic(diagnostic);
                    }

                    return;
                }
            }
        }

        private static bool IsReservedDiagnosticId(string ruleId, string? assemblyName)
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

            if (!ruleId[2..].All(char.IsDigit))
            {
                return false;
            }

            if (!isCARule)
            {
                // This is a reserved compiler diagnostic ID (CS or BC prefix)
                return true;
            }

            if (assemblyName is null)
            {
                // This is a reserved code analysis ID (CA prefix) being reported from an unspecified assembly
                return true;
            }

            return !CADiagnosticIdAllowedAssemblies.Contains(assemblyName);
        }
    }
}
