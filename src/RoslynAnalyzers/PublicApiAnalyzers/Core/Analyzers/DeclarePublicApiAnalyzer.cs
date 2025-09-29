// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed partial class DeclarePublicApiAnalyzer : DiagnosticAnalyzer
    {
        private static readonly SourceTextValueProvider<ApiData> s_shippingApiDataProvider = new(static text => ReadApiData(text, isShippedApi: true));
        private static readonly SourceTextValueProvider<ApiData> s_nonShippingApiDataProvider = new(static text => ReadApiData(text, isShippedApi: false));

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
        private static readonly char[] ObliviousMarkerArray = [ObliviousMarker];

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

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            CheckAndRegisterImplementation(isPublic: true);
            CheckAndRegisterImplementation(isPublic: false);

            void CheckAndRegisterImplementation(bool isPublic)
            {
                var errors = new List<Diagnostic>();
                // Switch to "RegisterAdditionalFileAction" available in Microsoft.CodeAnalysis "3.8.x" to report additional file diagnostics: https://github.com/dotnet/roslyn-analyzers/issues/3918
                if (!TryGetAndValidateApiFiles(context, isPublic, errors, out var additionalFiles, out var shippedData, out var unshippedData))
                {
                    context.RegisterCompilationEndAction(context =>
                    {
                        foreach (Diagnostic cur in errors)
                        {
                            context.ReportDiagnostic(cur);
                        }
                    });

                    return;
                }

                Debug.Assert(errors.Count == 0);

                RegisterImplActions(context, new Impl(context.Compilation, additionalFiles, shippedData, unshippedData, isPublic, context.Options));
                return;

                bool TryGetAndValidateApiFiles(CompilationStartAnalysisContext context, bool isPublic, List<Diagnostic> errors, [NotNullWhen(true)] out ImmutableDictionary<AdditionalText, SourceText>? additionalFiles, [NotNullWhen(true)] out ApiData? shippedData, [NotNullWhen(true)] out ApiData? unshippedData)
                {
                    return TryGetApiData(context, isPublic, errors, out additionalFiles, out shippedData, out unshippedData)
                           && ValidateApiFiles(additionalFiles, shippedData, unshippedData, isPublic, errors);
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

        private static ApiData ReadApiData(SourceText sourceText, bool isShippedApi)
        {
            var apiBuilder = ArrayBuilder<ApiLine>.GetInstance();
            var removedBuilder = ArrayBuilder<RemovedApiLine>.GetInstance();
            var lastNullableLineNumber = -1;

            // current line we're on.  Note: we ignore whitespace lines when computing this.
            var lineNumber = -1;

            var additionalFileInfo = new AdditionalFileInfo(sourceText, isShippedApi);

            foreach (var line in sourceText.Lines)
            {
                // Skip whitespace.
                var text = line.ToString();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                lineNumber++;

                if (text == NullableEnable)
                {
                    lastNullableLineNumber = lineNumber;
                    continue;
                }

                var apiLine = new ApiLine(text, line.Span, additionalFileInfo);
                if (text.StartsWith(RemovedApiPrefix, StringComparison.Ordinal))
                {
                    var removedText = text[RemovedApiPrefix.Length..];
                    removedBuilder.Add(new RemovedApiLine(removedText, apiLine));
                }
                else
                {
                    apiBuilder.Add(apiLine);
                }
            }

            return new ApiData(apiBuilder.ToImmutableAndFree(), removedBuilder.ToImmutableAndFree(), lastNullableLineNumber);
        }

        private static bool TryGetApiData(CompilationStartAnalysisContext context, bool isPublic, List<Diagnostic> errors, [NotNullWhen(true)] out ImmutableDictionary<AdditionalText, SourceText>? additionalFiles, [NotNullWhen(true)] out ApiData? shippedData, [NotNullWhen(true)] out ApiData? unshippedData)
        {
            using var _1 = ArrayBuilder<ApiData>.GetInstance(out var allShippedData);
            using var _2 = ArrayBuilder<ApiData>.GetInstance(out var allUnshippedData);

            AddApiTexts(context, isPublic, out additionalFiles, allShippedData, allUnshippedData);

            // Both missing.
            if (allShippedData.Count == 0 && allUnshippedData.Count == 0)
            {
                if (TryGetEditorConfigOptionForMissingFiles(context.Options, context.Compilation, out var silentlyBailOutOnMissingApiFiles) &&
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

            // Both there. Succeed and return what was found.
            if (allShippedData.Count > 0 && allUnshippedData.Count > 0)
            {
                shippedData = Flatten(allShippedData);
                unshippedData = Flatten(allUnshippedData);
                return true;
            }

            // One missing.  Give custom error message depending on which it was.
            var missingFileName = (allShippedData.Count == 0, isPublic) switch
            {
                (true, isPublic: true) => PublicShippedFileName,
                (true, isPublic: false) => InternalShippedFileName,
                (false, isPublic: true) => PublicUnshippedFileName,
                (false, isPublic: false) => InternalUnshippedFileName
            };

            errors.Add(Diagnostic.Create(isPublic ? PublicApiFileMissing : InternalApiFileMissing, Location.None, missingFileName));
            shippedData = null;
            unshippedData = null;
            return false;

            // Takes potentially multiple ApiData instances, corresponding to different additional text files, and
            // flattens them into the final instance we will use when analyzing the compilation.
            static ApiData Flatten(ArrayBuilder<ApiData> allData)
            {
                Debug.Assert(allData.Count > 0);

                // The common case is that we will have one file corresponding to the shipped data, and one for the
                // unshipped data.  In that case, just return the instance directly.
                if (allData.Count == 1)
                    return allData[0];

                var apiBuilder = ArrayBuilder<ApiLine>.GetInstance();
                var removedBuilder = ArrayBuilder<RemovedApiLine>.GetInstance();

                for (int i = 0, n = allData.Count; i < n; i++)
                {
                    var data = allData[i];
                    apiBuilder.AddRange(data.ApiList);
                    removedBuilder.AddRange(data.RemovedApiList);
                }

                return new ApiData(
                    apiBuilder.ToImmutableAndFree(),
                    removedBuilder.ToImmutableAndFree(),
                    allData.Max(static d => d.NullableLineNumber));
            }
        }

        private static bool TryGetEditorConfigOption(AnalyzerOptions analyzerOptions, SyntaxTree tree, string optionName, out string optionValue)
        {
            optionValue = "";
            try
            {
                var provider = analyzerOptions.AnalyzerConfigOptionsProvider;
                if (provider == null)
                {
                    return false;
                }

                var options = provider.GetOptions(tree);

                // bool TryGetValue(string key, out string value);
                var parameters = new object?[] { optionName, null };
                if (!options.TryGetValue(optionName, out var value))
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

            var namespaceStrings = namespacesString.Split([','], StringSplitOptions.RemoveEmptyEntries);
            if (namespaceStrings.Length == 0)
            {
                return false;
            }

            skippedNamespaces = namespaceStrings.ToImmutableArray();
            return true;
        }

        [SuppressMessage("MicrosoftCodeAnalysisPerformance", "RS1012:Start action has no registered actions", Justification = "This is not a start action")]
        private static void AddApiTexts(
            CompilationStartAnalysisContext context,
            bool isPublic,
            out ImmutableDictionary<AdditionalText, SourceText> additionalFiles,
            ArrayBuilder<ApiData> allShippedData,
            ArrayBuilder<ApiData> allUnshippedData)
        {
            additionalFiles = ImmutableDictionary<AdditionalText, SourceText>.Empty;

            foreach (var additionalText in context.Options.AdditionalFiles)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var file = new PublicApiFile(additionalText.Path, isPublic);

                // if it's not an api file (quick filename check), we can just immediately ignore.
                if (!file.IsApiFile)
                    continue;

                var apiDataProvider = file.IsShipping ? s_shippingApiDataProvider : s_nonShippingApiDataProvider;
                var text = additionalText.GetText(context.CancellationToken);
                if (text is null)
                    continue;

                additionalFiles = additionalFiles.Add(additionalText, text);
                if (!context.TryGetValue(text, apiDataProvider, out var apiData))
                    continue;

                var resultList = file.IsShipping ? allShippedData : allUnshippedData;
                resultList.Add(apiData);
            }
        }

        private static bool ValidateApiFiles(ImmutableDictionary<AdditionalText, SourceText> additionalFiles, ApiData shippedData, ApiData unshippedData, bool isPublic, List<Diagnostic> errors)
        {
            var descriptor = isPublic ? PublicApiFilesInvalid : InternalApiFilesInvalid;
            if (!shippedData.RemovedApiList.IsEmpty)
            {
                errors.Add(Diagnostic.Create(descriptor, Location.None, InvalidReasonShippedCantHaveRemoved));
            }

            if (shippedData.NullableLineNumber > 0)
            {
                // '#nullable enable' must be on the first line
                errors.Add(Diagnostic.Create(descriptor, Location.None, InvalidReasonMisplacedNullableEnable));
            }

            if (unshippedData.NullableLineNumber > 0)
            {
                // '#nullable enable' must be on the first line
                errors.Add(Diagnostic.Create(descriptor, Location.None, InvalidReasonMisplacedNullableEnable));
            }

            var publicApiMap = PooledDictionary<string, ApiLine>.GetInstance(StringComparer.Ordinal);
            ValidateApiList(additionalFiles, publicApiMap, shippedData.ApiList, isPublic, errors);
            ValidateApiList(additionalFiles, publicApiMap, unshippedData.ApiList, isPublic, errors);
            publicApiMap.Free();

            return errors.Count == 0;
        }

        private static void ValidateApiList(ImmutableDictionary<AdditionalText, SourceText> additionalFiles, Dictionary<string, ApiLine> publicApiMap, ImmutableArray<ApiLine> apiList, bool isPublic, List<Diagnostic> errors)
        {
            foreach (ApiLine cur in apiList)
            {
                string textWithoutOblivious = cur.Text.TrimStart(ObliviousMarkerArray);
                if (publicApiMap.TryGetValue(textWithoutOblivious, out ApiLine existingLine))
                {
                    Location existingLocation = existingLine.GetLocation(additionalFiles);
                    Location duplicateLocation = cur.GetLocation(additionalFiles);
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
