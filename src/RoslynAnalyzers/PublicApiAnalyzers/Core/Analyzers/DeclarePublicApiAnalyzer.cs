// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
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

        /// <summary>
        /// Comma/semicolon-separated list of InternalsVisibleTo (IVT) target assembly names (supporting a
        /// <c>*</c> wildcard anywhere in the pattern, e.g. <c>*.Tests</c>) that should be ignored when deciding
        /// whether internal API tracking should run. See <see cref="ShouldTrackInternalApiBasedOnIvt"/>.
        /// </summary>
        private const string InternalApiSkipIvtEditorConfigOptionName = $"{BaseEditorConfigPath}.internal_api_skip_ivt";

        /// <summary>
        /// Well-known assembly name used by Castle DynamicProxy (which backs mocking frameworks such as Moq) for its
        /// generated proxy assembly. IVT grants to this assembly never represent a real compatibility consumer, so it
        /// is always ignored when deciding whether internal API tracking should run.
        /// </summary>
        private const string DynamicProxyGenAssemblyName = "DynamicProxyGenAssembly2";

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
                // Internal API tracking (RS0051-RS0058) exists to catch binary-breaking changes across
                // InternalsVisibleTo (IVT) boundaries. IVT grants to test projects and mocking frameworks
                // (Moq/Castle proxies use the well-known assembly name 'DynamicProxyGenAssembly2') are not real
                // compatibility consumers. When the only IVT grants target such assemblies, internal API tracking
                // adds no value and only creates noise, so we skip registering the internal analyzer entirely.
                // This only affects the internal analyzer instance; the public instance is never affected.
                if (!isPublic && !ShouldTrackInternalApiBasedOnIvt(context.Compilation, context.Options))
                {
                    return;
                }

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

        /// <summary>
        /// Determines whether internal API tracking should run for the given compilation, based on its
        /// InternalsVisibleTo (IVT) grants. IVT is assembly-wide (not per-API), so the decision must be made at the
        /// assembly level: we read the compilation assembly's IVT grants, drop the ones matching the ignore patterns
        /// (always including the built-in <see cref="DynamicProxyGenAssemblyName"/> default plus anything configured
        /// via <see cref="InternalApiSkipIvtEditorConfigOptionName"/>), and if no real (non-ignored) IVT target
        /// remains we disable internal API tracking.
        /// <para>
        /// To stay conservative we only skip when there was at least one IVT grant and every grant is ignored. A
        /// project with internal API files but zero IVT grants has still explicitly opted in (via the presence of the
        /// InternalAPI.*.txt files), so we keep tracking in that case.
        /// </para>
        /// </summary>
        private static bool ShouldTrackInternalApiBasedOnIvt(Compilation compilation, AnalyzerOptions analyzerOptions)
        {
            var internalsVisibleToAttribute = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesInternalsVisibleToAttribute);
            if (internalsVisibleToAttribute is null)
            {
                // Can't resolve the IVT attribute type; preserve existing behavior and keep tracking.
                return true;
            }

            var ignorePatterns = GetInternalApiSkipIvtPatterns(analyzerOptions, compilation);

            var sawAnyIvtGrant = false;
            foreach (var attribute in compilation.Assembly.GetAttributes(internalsVisibleToAttribute))
            {
                if (attribute.ConstructorArguments.Length == 0 ||
                    attribute.ConstructorArguments[0].Value is not string assemblyNameArgument ||
                    string.IsNullOrWhiteSpace(assemblyNameArgument))
                {
                    continue;
                }

                sawAnyIvtGrant = true;

                // The IVT argument has the form "AssemblyName, PublicKey=0024...". Strip everything from the first
                // comma onward to obtain the simple assembly name before matching.
                var simpleName = assemblyNameArgument;
                var commaIndex = simpleName.IndexOf(',');
                if (commaIndex >= 0)
                {
                    simpleName = simpleName[..commaIndex];
                }

                simpleName = simpleName.Trim();

                if (!IsIgnoredIvtTarget(simpleName, ignorePatterns))
                {
                    // At least one real (non-ignored) IVT consumer exists; keep tracking.
                    return true;
                }
            }

            // Only skip when there was at least one IVT grant and all grants were ignored.
            return !sawAnyIvtGrant;
        }

        private static ImmutableArray<string> GetInternalApiSkipIvtPatterns(AnalyzerOptions analyzerOptions, Compilation compilation)
        {
            var builder = ImmutableArray.CreateBuilder<string>();

            // Always ignore the Castle DynamicProxy / Moq proxy assembly.
            builder.Add(DynamicProxyGenAssemblyName);

            // IVT is assembly-level, so read the (compilation-level) editorconfig option via the first syntax tree,
            // mirroring TryGetEditorConfigOptionForMissingFiles.
            if (compilation.SyntaxTrees.FirstOrDefault() is { } tree &&
                TryGetEditorConfigOption(analyzerOptions, tree, InternalApiSkipIvtEditorConfigOptionName, out var patternsString) &&
                !string.IsNullOrWhiteSpace(patternsString))
            {
                foreach (var pattern in patternsString.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = pattern.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        builder.Add(trimmed);
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static bool IsIgnoredIvtTarget(string assemblySimpleName, ImmutableArray<string> ignorePatterns)
        {
            foreach (var pattern in ignorePatterns)
            {
                if (IsAssemblyNameWildcardMatch(assemblySimpleName, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAssemblyNameWildcardMatch(string assemblySimpleName, string pattern)
        {
            if (!pattern.Contains('*'))
            {
                return string.Equals(assemblySimpleName, pattern, StringComparison.OrdinalIgnoreCase);
            }

            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(assemblySimpleName, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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
