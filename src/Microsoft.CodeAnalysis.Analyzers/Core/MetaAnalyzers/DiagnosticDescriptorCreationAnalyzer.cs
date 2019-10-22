// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DiagnosticDescriptorCreationAnalyzer : DiagnosticAnalyzer
    {
        private const string HelpLinkUriParameterName = "helpLinkUri";
        private const string CategoryParameterName = "category";
        private const string DiagnosticIdParameterName = "id";

        private const string DiagnosticCategoryAndIdRangeFile = "DiagnosticCategoryAndIdRanges.txt";
        private static readonly (string prefix, int start, int end) s_defaultAllowedIdsInfo = (null, -1, -1);

        private static readonly LocalizableString s_localizableUseLocalizableStringsTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseLocalizableStringsMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseLocalizableStringsDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseLocalizableStringsInDescriptorDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableProvideHelpUriTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideHelpUriInDescriptorTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableProvideHelpUriMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideHelpUriInDescriptorMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableProvideHelpUriDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ProvideHelpUriInDescriptorDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableDiagnosticIdMustBeAConstantTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DiagnosticIdMustBeAConstantTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDiagnosticIdMustBeAConstantMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DiagnosticIdMustBeAConstantMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDiagnosticIdMustBeAConstantDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DiagnosticIdMustBeAConstantDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableDiagnosticIdMustBeInSpecifiedFormatTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DiagnosticIdMustBeInSpecifiedFormatTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDiagnosticIdMustBeInSpecifiedFormatMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DiagnosticIdMustBeInSpecifiedFormatMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDiagnosticIdMustBeInSpecifiedFormatDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DiagnosticIdMustBeInSpecifiedFormatDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableUseUniqueDiagnosticIdTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseUniqueDiagnosticIdTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseUniqueDiagnosticIdMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseUniqueDiagnosticIdMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseUniqueDiagnosticIdDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseUniqueDiagnosticIdDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableUseCategoriesFromSpecifiedRangeTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseCategoriesFromSpecifiedRangeTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseCategoriesFromSpecifiedRangeMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseCategoriesFromSpecifiedRangeMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableUseCategoriesFromSpecifiedRangeDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UseCategoriesFromSpecifiedRangeDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        private static readonly LocalizableString s_localizableAnalyzerCategoryAndIdRangeFileInvalidTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AnalyzerCategoryAndIdRangeFileInvalidTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableAnalyzerCategoryAndIdRangeFileInvalidMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AnalyzerCategoryAndIdRangeFileInvalidMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableAnalyzerCategoryAndIdRangeFileInvalidDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.AnalyzerCategoryAndIdRangeFileInvalidDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor UseLocalizableStringsInDescriptorRule = new DiagnosticDescriptor(
            DiagnosticIds.UseLocalizableStringsInDescriptorRuleId,
            s_localizableUseLocalizableStringsTitle,
            s_localizableUseLocalizableStringsMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisLocalization,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: s_localizableUseLocalizableStringsDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor ProvideHelpUriInDescriptorRule = new DiagnosticDescriptor(
            DiagnosticIds.ProvideHelpUriInDescriptorRuleId,
            s_localizableProvideHelpUriTitle,
            s_localizableProvideHelpUriMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisDocumentation,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: s_localizableProvideHelpUriDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor DiagnosticIdMustBeAConstantRule = new DiagnosticDescriptor(
            DiagnosticIds.DiagnosticIdMustBeAConstantRuleId,
            s_localizableDiagnosticIdMustBeAConstantTitle,
            s_localizableDiagnosticIdMustBeAConstantMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: s_localizableDiagnosticIdMustBeAConstantDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor DiagnosticIdMustBeInSpecifiedFormatRule = new DiagnosticDescriptor(
            DiagnosticIds.DiagnosticIdMustBeInSpecifiedFormatRuleId,
            s_localizableDiagnosticIdMustBeInSpecifiedFormatTitle,
            s_localizableDiagnosticIdMustBeInSpecifiedFormatMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: s_localizableDiagnosticIdMustBeInSpecifiedFormatDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor UseUniqueDiagnosticIdRule = new DiagnosticDescriptor(
            DiagnosticIds.UseUniqueDiagnosticIdRuleId,
            s_localizableUseUniqueDiagnosticIdTitle,
            s_localizableUseUniqueDiagnosticIdMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: s_localizableUseUniqueDiagnosticIdDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor UseCategoriesFromSpecifiedRangeRule = new DiagnosticDescriptor(
            DiagnosticIds.UseCategoriesFromSpecifiedRangeRuleId,
            s_localizableUseCategoriesFromSpecifiedRangeTitle,
            s_localizableUseCategoriesFromSpecifiedRangeMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: s_localizableUseCategoriesFromSpecifiedRangeDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor AnalyzerCategoryAndIdRangeFileInvalidRule = new DiagnosticDescriptor(
            DiagnosticIds.AnalyzerCategoryAndIdRangeFileInvalidRuleId,
            s_localizableAnalyzerCategoryAndIdRangeFileInvalidTitle,
            s_localizableAnalyzerCategoryAndIdRangeFileInvalidMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisDesign,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: s_localizableAnalyzerCategoryAndIdRangeFileInvalidDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            UseLocalizableStringsInDescriptorRule,
            ProvideHelpUriInDescriptorRule,
            DiagnosticIdMustBeAConstantRule,
            DiagnosticIdMustBeInSpecifiedFormatRule,
            UseUniqueDiagnosticIdRule,
            UseCategoriesFromSpecifiedRangeRule,
            AnalyzerCategoryAndIdRangeFileInvalidRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol diagnosticDescriptorType = compilationContext.Compilation.GetOrCreateTypeByMetadataName(DiagnosticAnalyzerCorrectnessAnalyzer.DiagnosticDescriptorFullName);
                if (diagnosticDescriptorType == null)
                {
                    return;
                }

                // Try read the additional file containing the allowed categories, and corresponding ID ranges.
                var checkCategoryAndAllowedIds = TryGetCategoryAndAllowedIdsMap(
                    compilationContext.Options.AdditionalFiles,
                    compilationContext.CancellationToken,
                    out AdditionalText additionalTextOpt,
                    out ImmutableDictionary<string, ImmutableArray<(string prefix, int start, int end)>> categoryAndAllowedIdsMap,
                    out List<Diagnostic> invalidFileDiagnostics);

                var idToAnalyzerMap = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<Location>>>();
                compilationContext.RegisterOperationAction(operationAnalysisContext =>
                {
                    if (!(((IFieldInitializerOperation)operationAnalysisContext.Operation).Value is IObjectCreationOperation objectCreation))
                    {
                        return;
                    }

                    var ctor = objectCreation.Constructor;
                    if (ctor == null ||
                        !diagnosticDescriptorType.Equals(ctor.ContainingType) ||
                        !diagnosticDescriptorType.InstanceConstructors.Any(c => c.Equals(ctor)))
                    {
                        return;
                    }

                    AnalyzeTitle(operationAnalysisContext, objectCreation);
                    AnalyzeHelpLinkUri(operationAnalysisContext, objectCreation);

                    string categoryOpt = null;
                    if (!checkCategoryAndAllowedIds ||
                        !TryAnalyzeCategory(operationAnalysisContext, objectCreation,
                            additionalTextOpt, categoryAndAllowedIdsMap, out categoryOpt, out var allowedIdsInfoList))
                    {
                        allowedIdsInfoList = default;
                    }

                    AnalyzeRuleId(operationAnalysisContext, objectCreation, additionalTextOpt,
                        categoryOpt, allowedIdsInfoList, idToAnalyzerMap);

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
                });
            });
        }

        private static void AnalyzeTitle(OperationAnalysisContext operationAnalysisContext, IObjectCreationOperation objectCreation)
        {
            IParameterSymbol title = objectCreation.Constructor.Parameters.FirstOrDefault(p => p.Name == "title");
            if (title != null &&
                title.Type != null &&
                title.Type.SpecialType == SpecialType.System_String)
            {
                Diagnostic diagnostic = Diagnostic.Create(UseLocalizableStringsInDescriptorRule, objectCreation.Syntax.GetLocation(), DiagnosticAnalyzerCorrectnessAnalyzer.LocalizableStringFullName);
                operationAnalysisContext.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeHelpLinkUri(OperationAnalysisContext operationAnalysisContext, IObjectCreationOperation objectCreation)
        {
            // Find the matching argument for helpLinkUri
            foreach (var argument in objectCreation.Arguments)
            {
                if (argument.Parameter.Name.Equals(HelpLinkUriParameterName, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (argument.Value.ConstantValue.HasValue && argument.Value.ConstantValue.Value == null)
                    {
                        Diagnostic diagnostic = Diagnostic.Create(ProvideHelpUriInDescriptorRule, argument.Syntax.GetLocation());
                        operationAnalysisContext.ReportDiagnostic(diagnostic);
                    }

                    return;
                }
            }
        }

        private static bool TryAnalyzeCategory(
            OperationAnalysisContext operationAnalysisContext,
            IObjectCreationOperation objectCreation,
            AdditionalText additionalText,
            ImmutableDictionary<string, ImmutableArray<(string prefix, int start, int end)>> categoryAndAllowedIdsInfoMap,
            out string category,
            out ImmutableArray<(string prefix, int start, int end)> allowedIdsInfoList)
        {
            category = null;
            allowedIdsInfoList = default;
            foreach (var argument in objectCreation.Arguments)
            {
                if (argument.Parameter.Name.Equals(CategoryParameterName, StringComparison.Ordinal))
                {
                    // Check if the category argument is a constant or refers to a string field.
                    if (argument.Value.ConstantValue.HasValue)
                    {
                        if (argument.Value.Type != null &&
                            argument.Value.Type.SpecialType == SpecialType.System_String)
                        {
                            category = (string)argument.Value.ConstantValue.Value;
                        }
                    }
                    else if (argument.Value is IFieldReferenceOperation fieldReference &&
                        fieldReference.Field.Type.SpecialType == SpecialType.System_String)
                    {
                        category = fieldReference.ConstantValue.HasValue ? (string)fieldReference.ConstantValue.Value : fieldReference.Field.Name;
                    }

                    // Check if the category is one of the allowed values.
                    if (category != null && categoryAndAllowedIdsInfoMap.TryGetValue(category, out allowedIdsInfoList))
                    {
                        return true;
                    }

                    // Category '{0}' is not from the allowed categories specified in the file '{1}'.
                    string arg1 = category ?? "<unknown>";
                    string arg2 = Path.GetFileName(additionalText.Path);
                    var diagnostic = Diagnostic.Create(UseCategoriesFromSpecifiedRangeRule, argument.Value.Syntax.GetLocation(), arg1, arg2);
                    operationAnalysisContext.ReportDiagnostic(diagnostic);
                    return false;
                }
            }

            return false;
        }

        private static void AnalyzeRuleId(
            OperationAnalysisContext operationAnalysisContext,
            IObjectCreationOperation objectCreation,
            AdditionalText additionalTextOpt,
            string categoryOpt,
            ImmutableArray<(string prefix, int start, int end)> allowedIdsInfoListOpt,
            ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentBag<Location>>> idToAnalyzerMap)
        {
            var analyzer = ((IFieldSymbol)operationAnalysisContext.ContainingSymbol).ContainingType.OriginalDefinition;
            string ruleId = null;
            foreach (var argument in objectCreation.Arguments)
            {
                if (argument.Parameter.Name.Equals(DiagnosticIdParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    // Check if diagnostic ID is a constant string.
                    if (argument.Value.ConstantValue.HasValue &&
                        argument.Value.Type != null &&
                        argument.Value.Type.SpecialType == SpecialType.System_String)
                    {
                        ruleId = (string)argument.Value.ConstantValue.Value;
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

                        // If we have an additional file specifying required range and/or format for the ID, validate the ID.
                        if (!allowedIdsInfoListOpt.IsDefault)
                        {
                            Debug.Assert(!allowedIdsInfoListOpt.IsEmpty);
                            Debug.Assert(categoryOpt != null);
                            Debug.Assert(additionalTextOpt != null);

                            var foundMatch = false;
                            static bool ShouldValidateRange((string prefix, int start, int end) range)
                                => range.start >= 0 && range.end >= 0;

                            // Check if ID matches any one of the required ranges.
                            foreach (var allowedIds in allowedIdsInfoListOpt)
                            {
                                Debug.Assert(allowedIds.prefix != null);

                                if (ruleId.StartsWith(allowedIds.prefix, StringComparison.Ordinal))
                                {
                                    if (ShouldValidateRange(allowedIds))
                                    {
                                        var suffix = ruleId.Substring(allowedIds.prefix.Length);
                                        if (int.TryParse(suffix, out int ruleIdInt) &&
                                            ruleIdInt >= allowedIds.start &&
                                            ruleIdInt <= allowedIds.end)
                                        {
                                            foundMatch = true;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        foundMatch = true;
                                        break;
                                    }
                                }
                            }

                            if (!foundMatch)
                            {
                                // Diagnostic Id '{0}' belonging to category '{1}' is not in the required range and/or format '{2}' specified in the file '{3}'.
                                string arg1 = ruleId;
                                string arg2 = categoryOpt;
                                string arg3 = string.Empty;
                                foreach (var range in allowedIdsInfoListOpt)
                                {
                                    if (arg3.Length != 0)
                                    {
                                        arg3 += ", ";
                                    }

                                    arg3 += !ShouldValidateRange(range) ? range.prefix + "XXXX" : $"{range.prefix}{range.start}-{range.prefix}{range.end}";
                                }

                                string arg4 = Path.GetFileName(additionalTextOpt.Path);
                                var diagnostic = Diagnostic.Create(DiagnosticIdMustBeInSpecifiedFormatRule, argument.Value.Syntax.GetLocation(), arg1, arg2, arg3, arg4);
                                operationAnalysisContext.ReportDiagnostic(diagnostic);
                            }
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

        private static bool TryGetCategoryAndAllowedIdsMap(
            ImmutableArray<AdditionalText> additionalFiles,
            CancellationToken cancellationToken,
            out AdditionalText additionalText,
            out ImmutableDictionary<string, ImmutableArray<(string prefix, int start, int end)>> categoryAndAllowedIdsMap,
            out List<Diagnostic> invalidFileDiagnostics)
        {
            invalidFileDiagnostics = null;
            categoryAndAllowedIdsMap = null;

            // Parse the additional file with allowed diagnostic categories and corresponding ID range.
            // Bail out if there is no such additional file or it contains at least one invalid entry.
            additionalText = TryGetCategoryAndAllowedIdsInfoFile(additionalFiles, cancellationToken);
            return additionalText != null &&
                TryParseCategoryAndAllowedIdsInfoFile(additionalText, cancellationToken, out categoryAndAllowedIdsMap, out invalidFileDiagnostics);
        }

        private static AdditionalText TryGetCategoryAndAllowedIdsInfoFile(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
        {
            StringComparer comparer = StringComparer.Ordinal;
            foreach (AdditionalText textFile in additionalFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(textFile.Path);
                if (comparer.Equals(fileName, DiagnosticCategoryAndIdRangeFile))
                {
                    return textFile;
                }
            }

            return null;
        }

        private static bool TryParseCategoryAndAllowedIdsInfoFile(
            AdditionalText additionalText,
            CancellationToken cancellationToken,
            out ImmutableDictionary<string, ImmutableArray<(string prefix, int start, int end)>> categoryAndAllowedIdsInfoMap,
            out List<Diagnostic> invalidFileDiagnostics)
        {
            // Parse the additional file with allowed diagnostic categories and corresponding ID range.
            // FORMAT:
            // 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'

            categoryAndAllowedIdsInfoMap = null;
            invalidFileDiagnostics = null;

            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<(string prefix, int start, int end)>>();
            var lines = additionalText.GetText(cancellationToken).Lines;
            foreach (var line in lines)
            {
                var contents = line.ToString();
                if (contents.Length == 0 || contents.StartsWith("#", StringComparison.Ordinal))
                {
                    // Ignore empty lines and comments.
                    continue;
                }

                var parts = contents.Split(':');
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i] = parts[i].Trim();
                }

                var isInvalidLine = false;
                string category = parts[0];
                if (parts.Length > 2 ||                 // We allow only 0 or 1 ':' separator in the line.
                    category.Any(char.IsWhiteSpace) ||  // We do not allow white spaces in category name.
                    builder.ContainsKey(category))      // We do not allow multiple lines with same category.
                {
                    isInvalidLine = true;
                }
                else
                {
                    if (parts.Length == 1)
                    {
                        // No ':' symbol, so the entry just specifies the category.
                        builder.Add(category, default);
                        continue;
                    }

                    // Entry with the following possible formats:
                    // 'Category': Comma separate list of 'StartId-EndId' or 'Id' or 'Prefix'
                    var ranges = parts[1].Split(',');

                    var infoList = ImmutableArray.CreateBuilder<(string prefix, int start, int end)>(ranges.Length);
                    for (int i = 0; i < ranges.Length; i++)
                    {
                        (string prefix, int start, int end) allowedIdsInfo = s_defaultAllowedIdsInfo;
                        string range = ranges[i].Trim();
                        if (!range.Contains('-'))
                        {
                            if (TryParseIdRangeEntry(range, out string prefix, out int start))
                            {
                                // Specific Id validation.
                                allowedIdsInfo.prefix = prefix;
                                allowedIdsInfo.start = start;
                                allowedIdsInfo.end = start;
                            }
                            else if (range.All(ch => char.IsLetter(ch)))
                            {
                                // Only prefix validation.
                                allowedIdsInfo.prefix = range;
                            }
                            else
                            {
                                isInvalidLine = true;
                                break;
                            }
                        }
                        else
                        {
                            // Prefix and start-end range validation.
                            var rangeParts = range.Split('-');
                            if (TryParseIdRangeEntry(rangeParts[0], out string prefix1, out int start) &&
                                TryParseIdRangeEntry(rangeParts[1], out string prefix2, out int end) &&
                                prefix1.Equals(prefix2, StringComparison.Ordinal))
                            {
                                allowedIdsInfo.prefix = prefix1;
                                allowedIdsInfo.start = start;
                                allowedIdsInfo.end = end;
                            }
                            else
                            {
                                isInvalidLine = true;
                                break;
                            }
                        }

                        infoList.Add(allowedIdsInfo);
                    }

                    if (!isInvalidLine)
                    {
                        builder.Add(category, infoList.ToImmutable());
                    }
                }

                if (isInvalidLine)
                {
                    // Invalid entry '{0}' in analyzer category and diagnostic ID range specification file '{1}'.
                    string arg1 = contents;
                    string arg2 = Path.GetFileName(additionalText.Path);
                    LinePositionSpan linePositionSpan = lines.GetLinePositionSpan(line.Span);
                    Location location = Location.Create(additionalText.Path, line.Span, linePositionSpan);
                    invalidFileDiagnostics ??= new List<Diagnostic>();
                    var diagnostic = Diagnostic.Create(AnalyzerCategoryAndIdRangeFileInvalidRule, location, arg1, arg2);
                    invalidFileDiagnostics.Add(diagnostic);
                }
            }

            categoryAndAllowedIdsInfoMap = builder.ToImmutable();
            return invalidFileDiagnostics == null;
        }

        private static bool TryParseIdRangeEntry(string entry, out string prefix, out int suffix)
        {
            // Parse an entry for diagnostic ID.
            // We require diagnostic ID to have an alphabetical prefix followed by a numerical suffix.
            prefix = string.Empty;
            suffix = -1;
            string suffixStr = string.Empty;
            bool seenDigit = false;
            foreach (char ch in entry)
            {
                bool isDigit = char.IsDigit(ch);
                if (seenDigit && !isDigit)
                {
                    return false;
                }

                if (isDigit)
                {
                    suffixStr += ch;
                    seenDigit = true;
                }
                else if (!char.IsLetter(ch))
                {
                    return false;
                }
                else
                {
                    prefix += ch;
                }
            }

            return prefix.Length > 0 && suffixStr.Length > 0 && int.TryParse(suffixStr, out suffix);
        }
    }
}
