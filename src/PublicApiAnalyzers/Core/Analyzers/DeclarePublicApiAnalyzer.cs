// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using DiagnosticIds = Roslyn.Diagnostics.Analyzers.RoslynDiagnosticIds;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed partial class DeclarePublicApiAnalyzer : DiagnosticAnalyzer
    {
        internal const string ShippedFileName = "PublicAPI.Shipped.txt";
        internal const string UnshippedFileName = "PublicAPI.Unshipped.txt";
        internal const string PublicApiNamePropertyBagKey = "PublicAPIName";
        internal const string PublicApiNameWithNullabilityPropertyBagKey = "PublicAPINameWithNullability";
        internal const string MinimalNamePropertyBagKey = "MinimalName";
        internal const string PublicApiNamesOfSiblingsToRemovePropertyBagKey = "PublicApiNamesOfSiblingsToRemove";
        internal const string PublicApiNamesOfSiblingsToRemovePropertyBagValueSeparator = ";;";
        internal const string RemovedApiPrefix = "*REMOVED*";
        internal const string NullableEnable = "#nullable enable";
        internal const string InvalidReasonShippedCantHaveRemoved = "The shipped API file can't have removed members";
        internal const string InvalidReasonMisplacedNullableEnable = "The '#nullable enable' marker can only appear as the first line in the shipped API file";
        internal const string PublicApiIsShippedPropertyBagKey = "PublicAPIIsShipped";

        /// <summary>
        /// Boolean option to configure if public API analyzer should bail out silently if public API files are missing.
        /// </summary>
        private const string BailOnMissingPublicApiFilesEditorConfigOptionName = "dotnet_public_api_analyzer.require_api_files";

        internal static readonly DiagnosticDescriptor DeclareNewApiRule = new DiagnosticDescriptor(
            id: DiagnosticIds.DeclarePublicApiRuleId,
            title: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.DeclarePublicApiTitle), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            messageFormat: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.DeclarePublicApiMessage), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.DeclarePublicApiDescription), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor AnnotateApiRule = new DiagnosticDescriptor(
            id: DiagnosticIds.AnnotatePublicApiRuleId,
            title: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.AnnotatePublicApiTitle), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            messageFormat: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.AnnotatePublicApiMessage), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.AnnotatePublicApiDescription), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor ObliviousApiRule = new DiagnosticDescriptor(
            id: DiagnosticIds.ObliviousPublicApiRuleId,
            title: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.ObliviousPublicApiTitle), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            messageFormat: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.ObliviousPublicApiMessage), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.ObliviousPublicApiDescription), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor RemoveDeletedApiRule = new DiagnosticDescriptor(
            id: DiagnosticIds.RemoveDeletedApiRuleId,
            title: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.RemoveDeletedApiTitle), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            messageFormat: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.RemoveDeletedApiMessage), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.RemoveDeletedApiDescription), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor ExposedNoninstantiableType = new DiagnosticDescriptor(
            id: DiagnosticIds.ExposedNoninstantiableTypeRuleId,
            title: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.ExposedNoninstantiableTypeTitle), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            messageFormat: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.ExposedNoninstantiableTypeMessage), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor PublicApiFilesInvalid = new DiagnosticDescriptor(
            id: DiagnosticIds.PublicApiFilesInvalid,
            title: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.PublicApiFilesInvalidTitle), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            messageFormat: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.PublicApiFilesInvalidMessage), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor PublicApiFileMissing = new DiagnosticDescriptor(
            id: DiagnosticIds.PublicApiFileMissing,
            title: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.PublicApiFileMissingTitle), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            messageFormat: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.PublicApiFileMissingMessage), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor DuplicateSymbolInApiFiles = new DiagnosticDescriptor(
            id: DiagnosticIds.DuplicatedSymbolInPublicApiFiles,
            title: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.DuplicateSymbolsInPublicApiFilesTitle), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            messageFormat: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.DuplicateSymbolsInPublicApiFilesMessage), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor AvoidMultipleOverloadsWithOptionalParameters = new DiagnosticDescriptor(
            id: DiagnosticIds.AvoidMultipleOverloadsWithOptionalParameters,
            title: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.AvoidMultipleOverloadsWithOptionalParametersTitle), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            messageFormat: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.AvoidMultipleOverloadsWithOptionalParametersMessage), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: @"https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor OverloadWithOptionalParametersShouldHaveMostParameters = new DiagnosticDescriptor(
            id: DiagnosticIds.OverloadWithOptionalParametersShouldHaveMostParameters,
            title: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.OverloadWithOptionalParametersShouldHaveMostParametersTitle), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            messageFormat: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.OverloadWithOptionalParametersShouldHaveMostParametersMessage), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: @"https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor ShouldAnnotateApiFilesRule = new DiagnosticDescriptor(
            id: DiagnosticIds.ShouldAnnotateApiFilesRuleId,
            title: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.ShouldAnnotateApiFilesTitle), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            messageFormat: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.ShouldAnnotateApiFilesMessage), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(PublicApiAnalyzerResources.ShouldAnnotateApiFilesDescription), PublicApiAnalyzerResources.ResourceManager, typeof(PublicApiAnalyzerResources)),
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly SymbolDisplayFormat ShortSymbolNameFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.None,
                parameterOptions:
                    SymbolDisplayParameterOptions.None,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.None);

        private const int IncludeNullableReferenceTypeModifier = 1 << 6;
        private const int IncludeNonNullableReferenceTypeModifier = 1 << 8;

        private static readonly SymbolDisplayFormat s_publicApiFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeConstantValue,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly SymbolDisplayFormat s_publicApiFormatWithNullability =
            s_publicApiFormat.WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                (SymbolDisplayMiscellaneousOptions)IncludeNullableReferenceTypeModifier |
                (SymbolDisplayMiscellaneousOptions)IncludeNonNullableReferenceTypeModifier);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DeclareNewApiRule, AnnotateApiRule, ObliviousApiRule, RemoveDeletedApiRule, ExposedNoninstantiableType,
                PublicApiFilesInvalid, PublicApiFileMissing, DuplicateSymbolInApiFiles, AvoidMultipleOverloadsWithOptionalParameters,
                OverloadWithOptionalParametersShouldHaveMostParameters, ShouldAnnotateApiFilesRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // Analyzer needs to get callbacks for generated code, and might report diagnostics in generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
        {
            var errors = new List<Diagnostic>();

            // Switch to "RegisterAdditionalFileAction" available in Microsoft.CodeAnalysis "3.8.x" to report additional file diagnostics: https://github.com/dotnet/roslyn-analyzers/issues/3918
            if (!TryGetApiData(compilationContext.Options, compilationContext.Compilation, errors, compilationContext.CancellationToken, out ApiData shippedData, out ApiData unshippedData) ||
                !ValidateApiFiles(shippedData, unshippedData, errors))
            {
                compilationContext.RegisterCompilationEndAction(context =>
                {
                    foreach (Diagnostic cur in errors)
                    {
                        context.ReportDiagnostic(cur);
                    }
                });

                return;
            }

            Debug.Assert(errors.Count == 0);

            var impl = new Impl(compilationContext.Compilation, shippedData, unshippedData);
            compilationContext.RegisterSymbolAction(
                impl.OnSymbolAction,
                SymbolKind.NamedType,
                SymbolKind.Event,
                SymbolKind.Field,
                SymbolKind.Method);
            compilationContext.RegisterSymbolAction(
                impl.OnPropertyAction,
                SymbolKind.Property);
            compilationContext.RegisterCompilationEndAction(impl.OnCompilationEnd);
        }

        private static ApiData ReadApiData(string path, SourceText sourceText, bool isShippedApi)
        {
            var apiBuilder = ImmutableArray.CreateBuilder<ApiLine>();
            var removedBuilder = ImmutableArray.CreateBuilder<RemovedApiLine>();
            var maxNullableRank = -1;

            int rank = -1;
            foreach (TextLine line in sourceText.Lines)
            {
                string text = line.ToString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                rank++;

                if (text == NullableEnable)
                {
                    maxNullableRank = rank;
                    continue;
                }

                var apiLine = new ApiLine(text, line.Span, sourceText, path, isShippedApi);
                if (text.StartsWith(RemovedApiPrefix, StringComparison.Ordinal))
                {
                    string removedtext = text[RemovedApiPrefix.Length..];
                    removedBuilder.Add(new RemovedApiLine(removedtext, apiLine));
                }
                else
                {
                    apiBuilder.Add(apiLine);
                }
            }

            return new ApiData(apiBuilder.ToImmutable(), removedBuilder.ToImmutable(), maxNullableRank);
        }

        private static bool TryGetApiData(AnalyzerOptions analyzerOptions, Compilation compilation, List<Diagnostic> errors, CancellationToken cancellationToken, out ApiData shippedData, out ApiData unshippedData)
        {
            if (!TryGetApiText(analyzerOptions.AdditionalFiles, cancellationToken, out var shippedText, out var unshippedText))
            {
                if (shippedText == null && unshippedText == null)
                {
                    if (TryGetEditorConfigOptionForMissingFiles(analyzerOptions, compilation, out var silentlyBailOutOnMissingApiFiles) &&
                        silentlyBailOutOnMissingApiFiles)
                    {
                        shippedData = default;
                        unshippedData = default;
                        return false;
                    }

                    // Bootstrapping public API files.
                    (shippedData, unshippedData) = (ApiData.Empty, ApiData.Empty);
                    return true;
                }

                var missingFileName = shippedText == null ? ShippedFileName : UnshippedFileName;
                errors.Add(Diagnostic.Create(PublicApiFileMissing, Location.None, missingFileName));
                shippedData = default;
                unshippedData = default;
                return false;
            }

            shippedData = ReadApiData(shippedText.Path, shippedText.GetText(cancellationToken), isShippedApi: true);
            unshippedData = ReadApiData(unshippedText.Path, unshippedText.GetText(cancellationToken), isShippedApi: false);
            return true;
        }

        private static bool TryGetEditorConfigOptionForMissingFiles(AnalyzerOptions analyzerOptions, Compilation compilation, out bool optionValue)
        {
            optionValue = false;
            try
            {
                var provider = analyzerOptions.GetType().GetRuntimeProperty("AnalyzerConfigOptionsProvider")?.GetValue(analyzerOptions);
                if (provider == null || !compilation.SyntaxTrees.Any())
                {
                    return false;
                }

                var getOptionsMethod = provider.GetType().GetRuntimeMethods().FirstOrDefault(m => m.Name == "GetOptions");
                if (getOptionsMethod == null)
                {
                    return false;
                }

                var options = getOptionsMethod.Invoke(provider, new object[] { compilation.SyntaxTrees.First() });
                var tryGetValueMethod = options.GetType().GetRuntimeMethods().FirstOrDefault(m => m.Name == "TryGetValue");
                if (tryGetValueMethod == null)
                {
                    return false;
                }

                // bool TryGetValue(string key, out string value);
                var parameters = new object?[] { BailOnMissingPublicApiFilesEditorConfigOptionName, null };
                if (tryGetValueMethod.Invoke(options, parameters) is not bool hasOption ||
                    !hasOption)
                {
                    return false;
                }

                if (parameters[1] is not string value ||
                    !bool.TryParse(value, out var boolValue))
                {
                    return false;
                }

                optionValue = boolValue;
                return true;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // Gracefully handle any exception from reflection.
                return false;
            }
        }

        private static bool TryGetApiText(
            ImmutableArray<AdditionalText> additionalTexts,
            CancellationToken cancellationToken,
            [NotNullWhen(returnValue: true)] out AdditionalText? shippedText,
            [NotNullWhen(returnValue: true)] out AdditionalText? unshippedText)
        {
            shippedText = null;
            unshippedText = null;

            StringComparer comparer = StringComparer.Ordinal;
            foreach (AdditionalText text in additionalTexts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(text.Path);
                if (comparer.Equals(fileName, ShippedFileName))
                {
                    shippedText = text;
                    continue;
                }

                if (comparer.Equals(fileName, UnshippedFileName))
                {
                    unshippedText = text;
                    continue;
                }
            }

            return shippedText != null && unshippedText != null;
        }

        private static bool ValidateApiFiles(ApiData shippedData, ApiData unshippedData, List<Diagnostic> errors)
        {
            if (!shippedData.RemovedApiList.IsEmpty)
            {
                errors.Add(Diagnostic.Create(PublicApiFilesInvalid, Location.None, InvalidReasonShippedCantHaveRemoved));
            }

            if (shippedData.NullableRank > 0)
            {
                // '#nullable enable' must be on the first line
                errors.Add(Diagnostic.Create(PublicApiFilesInvalid, Location.None, InvalidReasonMisplacedNullableEnable));
            }

            if (unshippedData.NullableRank > 0)
            {
                // '#nullable enable' must be on the first line
                errors.Add(Diagnostic.Create(PublicApiFilesInvalid, Location.None, InvalidReasonMisplacedNullableEnable));
            }

            var publicApiMap = new Dictionary<string, ApiLine>(StringComparer.Ordinal);
            ValidateApiList(publicApiMap, shippedData.ApiList, errors);
            ValidateApiList(publicApiMap, unshippedData.ApiList, errors);

            return errors.Count == 0;
        }

        private static void ValidateApiList(Dictionary<string, ApiLine> publicApiMap, ImmutableArray<ApiLine> apiList, List<Diagnostic> errors)
        {
            foreach (ApiLine cur in apiList)
            {
                if (publicApiMap.TryGetValue(cur.Text, out ApiLine existingLine))
                {
                    LinePositionSpan existingLinePositionSpan = existingLine.SourceText.Lines.GetLinePositionSpan(existingLine.Span);
                    Location existingLocation = Location.Create(existingLine.Path, existingLine.Span, existingLinePositionSpan);

                    LinePositionSpan duplicateLinePositionSpan = cur.SourceText.Lines.GetLinePositionSpan(cur.Span);
                    Location duplicateLocation = Location.Create(cur.Path, cur.Span, duplicateLinePositionSpan);
                    errors.Add(Diagnostic.Create(DuplicateSymbolInApiFiles, duplicateLocation, new[] { existingLocation }, cur.Text));
                }
                else
                {
                    publicApiMap.Add(cur.Text, cur);
                }
            }
        }
    }
}
