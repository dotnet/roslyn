// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed partial class DeclarePublicApiAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ConditionalWeakTable<AdditionalText, ApiData> s_additionalTextToApiData = new();

        internal const string Extension = ".txt";
        internal const string PublicShippedFileNamePrefix = "PublicAPI.Shipped";
        internal const string PublicShippedFileName = PublicShippedFileNamePrefix + Extension;
        internal const string InternalShippedFileNamePrefix = "InternalAPI.Shipped";
        internal const string InternalShippedFileName = InternalShippedFileNamePrefix + Extension;
        internal const string PublicUnshippedFileNamePrefix = "PublicAPI.Unshipped";
        internal const string PublicUnshippedFileName = PublicUnshippedFileNamePrefix + Extension;
        internal const string InternalUnshippedFileNamePrefix = "InternalAPI.Unshipped";
        internal const string InternalUnshippedFileName = InternalUnshippedFileNamePrefix + Extension;
        internal const string ApiNamePropertyBagKey = "APIName";
        internal const string ApiNameWithNullabilityPropertyBagKey = "APINameWithNullability";
        internal const string MinimalNamePropertyBagKey = "MinimalName";
        internal const string ApiNamesOfSiblingsToRemovePropertyBagKey = "ApiNamesOfSiblingsToRemove";
        internal const string ApiNamesOfSiblingsToRemovePropertyBagValueSeparator = ";;";
        internal const string RemovedApiPrefix = "*REMOVED*";
        internal const string NullableEnable = "#nullable enable";
        internal const string InvalidReasonShippedCantHaveRemoved = "The shipped API file can't have removed members";
        internal const string InvalidReasonMisplacedNullableEnable = "The '#nullable enable' marker can only appear as the first line in the shipped API file";
        internal const string ApiIsShippedPropertyBagKey = "APIIsShipped";
        internal const string FileName = "FileName";

        private const char ObliviousMarker = '~';

        /// <summary>
        /// Boolean option to configure if public API analyzer should bail out silently if public API files are missing.
        /// </summary>
        private const string BaseEditorConfigPath = "dotnet_public_api_analyzer";
        private const string BailOnMissingPublicApiFilesEditorConfigOptionName = $"{BaseEditorConfigPath}.require_api_files";
        private const string NamespaceToIgnoreInTrackingEditorConfigOptionName = $"{BaseEditorConfigPath}.skip_namespaces";

        internal static readonly SymbolDisplayFormat ShortSymbolNameFormat =
            new(
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
            new(
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

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // Analyzer needs to get callbacks for generated code, and might report diagnostics in generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
        {
            CheckAndRegisterImplementation(isPublic: true);
            CheckAndRegisterImplementation(isPublic: false);

            void CheckAndRegisterImplementation(bool isPublic)
            {
                var errors = new List<Diagnostic>();
                // Switch to "RegisterAdditionalFileAction" available in Microsoft.CodeAnalysis "3.8.x" to report additional file diagnostics: https://github.com/dotnet/roslyn-analyzers/issues/3918
                if (!TryGetAndValidateApiFiles(
                        compilationContext.Options,
                        compilationContext.Compilation,
                        isPublic,
                        compilationContext.CancellationToken,
                        errors,
                        out var shippedData,
                        out var unshippedData))
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

                RegisterImplActions(compilationContext, new Impl(compilationContext.Compilation, shippedData, unshippedData, isPublic, compilationContext.Options));
                return;

                static bool TryGetAndValidateApiFiles(
                    AnalyzerOptions options,
                    Compilation compilation,
                    bool isPublic,
                    CancellationToken cancellationToken,
                    List<Diagnostic> errors,
                    [NotNullWhen(true)] out ApiData? shippedData,
                    [NotNullWhen(true)] out ApiData? unshippedData)
                {
                    return TryGetApiData(options, compilation, isPublic, errors, cancellationToken, out shippedData, out unshippedData)
                           && ValidateApiFiles(shippedData, unshippedData, isPublic, errors);
                }

                static void RegisterImplActions(CompilationStartAnalysisContext compilationContext, Impl impl)
                {
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
            }
        }

        private static ApiData ReadApiData(List<(string path, SourceText sourceText)> data, bool isShippedApi)
        {
            var apiBuilder = ImmutableArray.CreateBuilder<ApiLine>();
            var removedBuilder = ImmutableArray.CreateBuilder<RemovedApiLine>();
            var maxNullableRank = -1;

            foreach (var (path, sourceText) in data)
            {
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
                        maxNullableRank = Math.Max(rank, maxNullableRank);
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
            }

            return new ApiData(apiBuilder.ToImmutable(), removedBuilder.ToImmutable(), maxNullableRank);
        }

        private static bool TryGetApiData(
            AnalyzerOptions analyzerOptions,
            Compilation compilation,
            bool isPublic,
            List<Diagnostic> errors,
            CancellationToken cancellationToken,
            [NotNullWhen(true)] out ApiData? shippedData,
            [NotNullWhen(true)] out ApiData? unshippedData)
        {
            GetApiText(
                analyzerOptions.AdditionalFiles, isPublic, cancellationToken, out shippedData, out unshippedData);

            if (shippedData == null && unshippedData == null)
            {
                if (TryGetEditorConfigOptionForMissingFiles(analyzerOptions, compilation, out var silentlyBailOutOnMissingApiFiles) &&
                    silentlyBailOutOnMissingApiFiles)
                {
                    shippedData = null;
                    unshippedData = null;
                    return false;
                }

                // Bootstrapping public API files.
                (shippedData, unshippedData) = (ApiData.Empty, ApiData.Empty);
                return true;
            }

            if (shippedData != null && unshippedData != null)
                return true;

            var missingFileName = (shippedData, isPublic) switch
            {
                (null, isPublic: true) => PublicShippedFileName,
                (null, isPublic: false) => InternalShippedFileName,
                (not null, isPublic: true) => PublicUnshippedFileName,
                (not null, isPublic: false) => InternalUnshippedFileName
            };
            errors.Add(Diagnostic.Create(isPublic ? PublicApiFileMissing : InternalApiFileMissing, Location.None, missingFileName));
            shippedData = null;
            unshippedData = null;
            return false;
        }

        private static bool TryGetEditorConfigOption(AnalyzerOptions analyzerOptions, SyntaxTree tree, string optionName, out string optionValue)
        {
            optionValue = "";
            try
            {
                var provider = analyzerOptions.GetType().GetRuntimeProperty("AnalyzerConfigOptionsProvider")?.GetValue(analyzerOptions);
                if (provider == null)
                {
                    return false;
                }

                var getOptionsMethod = provider.GetType().GetRuntimeMethods().FirstOrDefault(m => m.Name == "GetOptions");
                if (getOptionsMethod == null)
                {
                    return false;
                }

                var options = getOptionsMethod.Invoke(provider, new object[] { tree });
                var tryGetValueMethod = options.GetType().GetRuntimeMethods().FirstOrDefault(m => m.Name == "TryGetValue");
                if (tryGetValueMethod == null)
                {
                    return false;
                }

                // bool TryGetValue(string key, out string value);
                var parameters = new object?[] { optionName, null };
                if (tryGetValueMethod.Invoke(options, parameters) is not bool hasOption ||
                    !hasOption)
                {
                    return false;
                }

                if (parameters[1] is not string value)
                {
                    return false;
                }

                optionValue = value;
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

        private static bool TryGetEditorConfigOptionForMissingFiles(AnalyzerOptions analyzerOptions, Compilation compilation, out bool optionValue)
        {
            optionValue = false;

            return compilation.SyntaxTrees.FirstOrDefault() is { } tree
                   && TryGetEditorConfigOption(analyzerOptions, tree, BailOnMissingPublicApiFilesEditorConfigOptionName, out string value)
                   && bool.TryParse(value, out optionValue);
        }

        private static bool TryGetEditorConfigOptionForSkippedNamespaces(AnalyzerOptions analyzerOptions, SyntaxTree tree, out ImmutableArray<string> skippedNamespaces)
        {
            skippedNamespaces = ImmutableArray<string>.Empty;
            if (!TryGetEditorConfigOption(analyzerOptions, tree, NamespaceToIgnoreInTrackingEditorConfigOptionName, out var namespacesString) || string.IsNullOrWhiteSpace(namespacesString))
            {
                return false;
            }

            var namespaceStrings = namespacesString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (namespaceStrings.Length == 0)
            {
                return false;
            }

            skippedNamespaces = namespaceStrings.ToImmutableArray();
            return true;
        }

        private static GetApiText(
            ImmutableArray<AdditionalText> additionalTexts,
            bool isPublic,
            CancellationToken cancellationToken,
            [NotNullWhen(returnValue: true)] out ApiData? shippedData,
            [NotNullWhen(returnValue: true)] out ApiData? unshippedData)
        {
            //shippedData = ReadApiData(shippedText, isShippedApi: true);
            //unshippedData = ReadApiData(unshippedText, isShippedApi: false);
            //return true;

            shippedData = null;
            unshippedData = null;

            foreach (var additionalText in additionalTexts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Can stop once we find the shipped and unshipped files.
                if (shippedData != null && unshippedData != null)
                    break;

                var file = new PublicApiFile(additionalText.Path, isPublic);

                // if it's not an api file (quick filename check), we can just immediately ignore.
                if (!file.IsApiFile)
                    continue;

                if (!s_additionalTextToApiData.TryGetValue(additionalText, out var apiData))
                {
                    apiData = ReadApiData(additionalText.Path, additionalText.GetText(cancellationToken), file.IsShipping);
                    apiData = s_additionalTextToApiData.GetValue(additionalText, _ => apiData);
                }

                if (file.IsShipping)
                    shippedData = apiData;
                else
                    unshippedData = apiData;
            }

            return shippedData != null && unshippedData != null;
        }

        private static bool ValidateApiFiles(ApiData shippedData, ApiData unshippedData, bool isPublic, List<Diagnostic> errors)
        {
            var descriptor = isPublic ? PublicApiFilesInvalid : InternalApiFilesInvalid;
            if (!shippedData.RemovedApiList.IsEmpty)
            {
                errors.Add(Diagnostic.Create(descriptor, Location.None, InvalidReasonShippedCantHaveRemoved));
            }

            if (shippedData.NullableRank > 0)
            {
                // '#nullable enable' must be on the first line
                errors.Add(Diagnostic.Create(descriptor, Location.None, InvalidReasonMisplacedNullableEnable));
            }

            if (unshippedData.NullableRank > 0)
            {
                // '#nullable enable' must be on the first line
                errors.Add(Diagnostic.Create(descriptor, Location.None, InvalidReasonMisplacedNullableEnable));
            }

            var publicApiMap = new Dictionary<string, ApiLine>(StringComparer.Ordinal);
            ValidateApiList(publicApiMap, shippedData.ApiList, isPublic, errors);
            ValidateApiList(publicApiMap, unshippedData.ApiList, isPublic, errors);

            return errors.Count == 0;
        }

        private static void ValidateApiList(Dictionary<string, ApiLine> publicApiMap, ImmutableArray<ApiLine> apiList, bool isPublic, List<Diagnostic> errors)
        {
            foreach (ApiLine cur in apiList)
            {
                string textWithoutOblivious = cur.Text.TrimStart(ObliviousMarker);
                if (publicApiMap.TryGetValue(textWithoutOblivious, out ApiLine existingLine))
                {
                    LinePositionSpan existingLinePositionSpan = existingLine.SourceText.Lines.GetLinePositionSpan(existingLine.Span);
                    Location existingLocation = Location.Create(existingLine.Path, existingLine.Span, existingLinePositionSpan);

                    LinePositionSpan duplicateLinePositionSpan = cur.SourceText.Lines.GetLinePositionSpan(cur.Span);
                    Location duplicateLocation = Location.Create(cur.Path, cur.Span, duplicateLinePositionSpan);
                    errors.Add(Diagnostic.Create(isPublic ? DuplicateSymbolInPublicApiFiles : DuplicateSymbolInInternalApiFiles, duplicateLocation, new[] { existingLocation }, cur.Text));
                }
                else
                {
                    publicApiMap.Add(textWithoutOblivious, cur);
                }
            }
        }
    }
}
