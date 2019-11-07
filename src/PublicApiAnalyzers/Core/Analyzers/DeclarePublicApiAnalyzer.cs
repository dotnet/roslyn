// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Analyzer.Utilities;
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
        internal const string MinimalNamePropertyBagKey = "MinimalName";
        internal const string PublicApiNamesOfSiblingsToRemovePropertyBagKey = "PublicApiNamesOfSiblingsToRemove";
        internal const string PublicApiNamesOfSiblingsToRemovePropertyBagValueSeparator = ";;";
        internal const string RemovedApiPrefix = "*REMOVED*";
        internal const string InvalidReasonShippedCantHaveRemoved = "The shipped API file can't have removed members";

        internal static readonly DiagnosticDescriptor DeclareNewApiRule = new DiagnosticDescriptor(
            id: DiagnosticIds.DeclarePublicApiRuleId,
            title: PublicApiAnalyzerResources.DeclarePublicApiTitle,
            messageFormat: PublicApiAnalyzerResources.DeclarePublicApiMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: PublicApiAnalyzerResources.DeclarePublicApiDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor RemoveDeletedApiRule = new DiagnosticDescriptor(
            id: DiagnosticIds.RemoveDeletedApiRuleId,
            title: PublicApiAnalyzerResources.RemoveDeletedApiTitle,
            messageFormat: PublicApiAnalyzerResources.RemoveDeletedApiMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: PublicApiAnalyzerResources.RemoveDeletedApiDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor ExposedNoninstantiableType = new DiagnosticDescriptor(
            id: DiagnosticIds.ExposedNoninstantiableTypeRuleId,
            title: PublicApiAnalyzerResources.ExposedNoninstantiableTypeTitle,
            messageFormat: PublicApiAnalyzerResources.ExposedNoninstantiableTypeMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor PublicApiFilesInvalid = new DiagnosticDescriptor(
            id: DiagnosticIds.PublicApiFilesInvalid,
            title: PublicApiAnalyzerResources.PublicApiFilesInvalidTitle,
            messageFormat: PublicApiAnalyzerResources.PublicApiFilesInvalidMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor DuplicateSymbolInApiFiles = new DiagnosticDescriptor(
            id: DiagnosticIds.DuplicatedSymbolInPublicApiFiles,
            title: PublicApiAnalyzerResources.DuplicateSymbolsInPublicApiFilesTitle,
            messageFormat: PublicApiAnalyzerResources.DuplicateSymbolsInPublicApiFilesMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor AvoidMultipleOverloadsWithOptionalParameters = new DiagnosticDescriptor(
            id: DiagnosticIds.AvoidMultipleOverloadsWithOptionalParameters,
            title: PublicApiAnalyzerResources.AvoidMultipleOverloadsWithOptionalParametersTitle,
            messageFormat: PublicApiAnalyzerResources.AvoidMultipleOverloadsWithOptionalParametersMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            helpLinkUri: @"https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor OverloadWithOptionalParametersShouldHaveMostParameters = new DiagnosticDescriptor(
            id: DiagnosticIds.OverloadWithOptionalParametersShouldHaveMostParameters,
            title: PublicApiAnalyzerResources.OverloadWithOptionalParametersShouldHaveMostParametersTitle,
            messageFormat: PublicApiAnalyzerResources.OverloadWithOptionalParametersShouldHaveMostParametersMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            helpLinkUri: @"https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md",
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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DeclareNewApiRule, RemoveDeletedApiRule, ExposedNoninstantiableType,
                PublicApiFilesInvalid, DuplicateSymbolInApiFiles, AvoidMultipleOverloadsWithOptionalParameters,
                OverloadWithOptionalParametersShouldHaveMostParameters);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // Analyzer needs to get callbacks for generated code, and might report diagnostics in generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
        {
            var additionalFiles = compilationContext.Options.AdditionalFiles;

            if (!TryGetApiData(additionalFiles, compilationContext.CancellationToken, out ApiData shippedData, out ApiData unshippedData))
            {
                return;
            }

            if (!ValidateApiFiles(shippedData, unshippedData, out List<Diagnostic> errors))
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
            ImmutableArray<ApiLine>.Builder apiBuilder = ImmutableArray.CreateBuilder<ApiLine>();
            ImmutableArray<RemovedApiLine>.Builder removedBuilder = ImmutableArray.CreateBuilder<RemovedApiLine>();

            foreach (TextLine line in sourceText.Lines)
            {
                string text = line.ToString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var apiLine = new ApiLine(text, line.Span, sourceText, path, isShippedApi);
                if (text.StartsWith(RemovedApiPrefix, StringComparison.Ordinal))
                {
                    string removedtext = text.Substring(RemovedApiPrefix.Length);
                    removedBuilder.Add(new RemovedApiLine(removedtext, apiLine));
                }
                else
                {
                    apiBuilder.Add(apiLine);
                }
            }

            return new ApiData(apiBuilder.ToImmutable(), removedBuilder.ToImmutable());
        }

        private static bool TryGetApiData(ImmutableArray<AdditionalText> additionalTexts, CancellationToken cancellationToken, out ApiData shippedData, out ApiData unshippedData)
        {
            if (!TryGetApiText(additionalTexts, cancellationToken, out var shippedText, out var unshippedText))
            {
                shippedData = default;
                unshippedData = default;
                return false;
            }

            shippedData = ReadApiData(shippedText.Path, shippedText.GetText(cancellationToken), isShippedApi: true);
            unshippedData = ReadApiData(unshippedText.Path, unshippedText.GetText(cancellationToken), isShippedApi: false);
            return true;
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

        private static bool ValidateApiFiles(ApiData shippedData, ApiData unshippedData, out List<Diagnostic> errors)
        {
            errors = new List<Diagnostic>();
            if (shippedData.RemovedApiList.Length > 0)
            {
                errors.Add(Diagnostic.Create(PublicApiFilesInvalid, Location.None, InvalidReasonShippedCantHaveRemoved));
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
