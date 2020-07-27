// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions
{
    internal abstract class AbstractRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer
        : AbstractCodeQualityDiagnosticAnalyzer, IPragmaSuppressionsAnalyzer
    {
        private static readonly LocalizableResourceString s_localizableRemoveUnnecessarySuppression = new LocalizableResourceString(
           nameof(AnalyzersResources.Remove_unnecessary_suppression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        internal static readonly DiagnosticDescriptor s_removeUnnecessarySuppressionDescriptor = CreateDescriptor(
            IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId, s_localizableRemoveUnnecessarySuppression, s_localizableRemoveUnnecessarySuppression, isUnnecessary: true);

        private readonly Lazy<ImmutableHashSet<int>> _lazySupportedCompilerErrorCodes;

        protected AbstractRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer()
            : base(ImmutableArray.Create(s_removeUnnecessarySuppressionDescriptor), GeneratedCodeAnalysisFlags.None)
        {
            _lazySupportedCompilerErrorCodes = new Lazy<ImmutableHashSet<int>>(() => GetSupportedCompilerErrorCodes());
        }

        protected abstract string CompilerErrorCodePrefix { get; }
        protected abstract int CompilerErrorCodeDigitCount { get; }
        protected abstract ISyntaxFacts SyntaxFacts { get; }
        protected abstract ISemanticFacts SemanticFacts { get; }
        protected abstract (Assembly assembly, string typeName) GetCompilerDiagnosticAnalyzerInfo();

        private ImmutableHashSet<int> GetSupportedCompilerErrorCodes()
        {
            try
            {
                // Use reflection to fetch compiler diagnostic IDs that are supported in IDE live analysis.
                // Note that the unit test projects have IVT access to compiler layer, and hence can access this API.
                // We have unit tests that guard this reflection based logic and will fail if the API is changed
                // without updating the below code.

                var (assembly, compilerAnalyzerTypeName) = GetCompilerDiagnosticAnalyzerInfo();
                var compilerAnalyzerType = assembly.GetType(compilerAnalyzerTypeName)!;
                var methodInfo = compilerAnalyzerType.GetMethod("GetSupportedErrorCodes", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var compilerAnalyzerInstance = Activator.CreateInstance(compilerAnalyzerType);
                var supportedCodes = methodInfo.Invoke(compilerAnalyzerInstance, Array.Empty<object>()) as IEnumerable<int>;
                return supportedCodes.ToImmutableHashSet();
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
                return ImmutableHashSet<int>.Empty;
            }
        }

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected sealed override void InitializeWorker(AnalysisContext context)
        {
            // We do not register any normal analyzer actions as we need 'CompilationWithAnalyzers'
            // context to analyze unused suppressions using reported compiler and analyzer diagnostics.
            // Instead, the analyzer defines a special 'AnalyzeAsync' method that should be invoked
            // by the host with CompilationWithAnalyzers input to compute unused suppression diagnostics.
        }

        public async Task AnalyzeAsync(
            SemanticModel semanticModel,
            TextSpan? span,
            CompilationWithAnalyzers compilationWithAnalyzers,
            Func<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>> getSupportedDiagnostics,
            Func<DiagnosticAnalyzer, bool> getIsCompilationEndAnalyzer,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken)
        {
            // We need compilation with suppressed diagnostics for this feature.
            if (!compilationWithAnalyzers.Compilation.Options.ReportSuppressedDiagnostics)
            {
                return;
            }

            var tree = semanticModel.SyntaxTree;

            // Bail out if analyzer is suppressed on this file or project.
            // NOTE: Normally, we would not require this check in the analyzer as the analyzer driver has this optimization.
            // However, this is a special analyzer that is directly invoked by the analysis host (IDE), so we do this check here.
            if (tree.DiagnosticOptions.TryGetValue(IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId, out var severity) ||
                compilationWithAnalyzers.Compilation.Options.SpecificDiagnosticOptions.TryGetValue(IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId, out severity))
            {
                if (severity == ReportDiagnostic.Suppress)
                {
                    return;
                }
            }

            // Bail out if analyzer has been turned off through options.
            var option = compilationWithAnalyzers.AnalysisOptions.Options?.GetOption(
                CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions, tree, cancellationToken).Trim();
            var (userExclusions, analyzerDisabled) = ParseUserExclusions(option);
            if (analyzerDisabled)
            {
                return;
            }

            // Bail out for generated code.
            if (tree.IsGeneratedCode(compilationWithAnalyzers.AnalysisOptions.Options, SyntaxFacts, cancellationToken))
            {
                return;
            }

            var root = tree.GetRoot(cancellationToken);

            // Bail out if tree has syntax errors.
            if (root.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return;
            }

            // Process pragma directives and inline SuppressMessageAttributes in the tree.
            // The core algorithm is as follows:
            //  1. Iterate through all the active pragmas and local SuppressMessageAttributes in the source file and
            //     identify the pragmas and local SuppressMessageAttributes
            //     with diagnostics IDs for which we support unnecesary suppression analysis.
            //  2. Build the following data structures during this loop:
            //      a. A map from diagnostic ID to list of pragmas for the ID. This map tracks supported diagnostic IDs for this tree's pragmas.
            //      b. A array of tuples of candidate pragmas sorted by span, along with associated IDs and enable/disable flag.
            //         This sorted array allows mapping an unnecessary pragma to the corresponding toggling pragma pair for removal.
            //      c. A map from pragmas to a boolean indicating if the pragma was used or not.
            //      d. A map from diagnostic ID to list of SuppressMessageAttribute nodes for the ID.
            //         This map tracks supported diagnostic IDs for this tree's SuppressMessageAttribute nodes.
            //      e. A map from SuppressMessageAttribute nodes to a boolean indicating if the attribute was used or not.
            //      f. A set of supported compiler diagnostic IDs that are used in pragmas or SuppressMessageAttributes in this file.
            //  3. Map the set of candidate diagnostic IDs to the analyzers that can report diagnostics with these IDs.
            //  4. Execute these analyzers to compute the diagnostics reported by these analyzers in this file.
            //  5. Iterate through the suppressed diagnostics from this list and do the following:
            //     a. If the diagnostic was suppressed with a prama, mark the closest preceeeding disable pragma
            //        which suppresses this ID as used/necessary. Also mark the matching restore pragma as used.
            //     b. Otherwise, if the diagnostic was suppressed with SuppressMessageAttribute, mark the attribute as used. 
            //  6. Finally, report a diagostic all the pragmas and SuppressMessageAttributes which have not been marked as used.

            using var _1 = PooledDictionary<string, List<(SyntaxTrivia pragma, bool isDisable)>>.GetInstance(out var idToPragmasMap);
            using var _2 = ArrayBuilder<(SyntaxTrivia pragma, ImmutableArray<string> ids, bool isDisable)>.GetInstance(out var sortedPragmasWithIds);
            using var _3 = PooledDictionary<SyntaxTrivia, bool>.GetInstance(out var pragmasToIsUsedMap);
            using var _4 = PooledHashSet<string>.GetInstance(out var compilerDiagnosticIds);
            var hasPragmaInAnalysisSpan = ProcessPragmaDirectives(root, span, idToPragmasMap,
                pragmasToIsUsedMap, sortedPragmasWithIds, compilerDiagnosticIds, userExclusions);

            cancellationToken.ThrowIfCancellationRequested();

            using var _5 = PooledDictionary<string, List<SyntaxNode>>.GetInstance(out var idToSuppressMessageAttributesMap);
            using var _6 = PooledDictionary<SyntaxNode, bool>.GetInstance(out var suppressMessageAttributesToIsUsedMap);
            var hasAttributeInAnalysisSpan = await ProcessSuppressMessageAttributesAsync(root, semanticModel, span,
                idToSuppressMessageAttributesMap, suppressMessageAttributesToIsUsedMap, userExclusions, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Bail out if we have no pragma directives or SuppressMessageAttributes to analyze.
            if (!hasPragmaInAnalysisSpan && !hasAttributeInAnalysisSpan)
            {
                return;
            }

            using var _8 = PooledHashSet<string>.GetInstance(out var idsToAnalyzeBuilder);
            idsToAnalyzeBuilder.AddAll(idToPragmasMap.Keys);
            idsToAnalyzeBuilder.AddAll(idToSuppressMessageAttributesMap.Keys);
            var idsToAnalyze = idsToAnalyzeBuilder.ToImmutableHashSet();

            // Compute all the reported compiler and analyzer diagnostics for diagnostic IDs corresponding to pragmas in the tree.
            var (diagnostics, unhandledIds) = await GetReportedDiagnosticsForIdsAsync(
                idsToAnalyze, root, semanticModel, compilationWithAnalyzers,
                getSupportedDiagnostics, getIsCompilationEndAnalyzer, compilerDiagnosticIds, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Iterate through reported diagnostics which are suppressed in source through pragmas and mark the corresponding pragmas as used.
            await ProcessReportedDiagnosticsAsync(diagnostics, tree, compilationWithAnalyzers, idToPragmasMap,
                pragmasToIsUsedMap, idToSuppressMessageAttributesMap, suppressMessageAttributesToIsUsedMap, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Remove entries for unhandled diagnostic ids.
            foreach (var id in unhandledIds)
            {
                if (idToPragmasMap.TryGetValue(id, out var pragmas))
                {
                    foreach (var (pragma, _) in pragmas)
                    {
                        pragmasToIsUsedMap.Remove(pragma);
                    }
                }

                if (idToSuppressMessageAttributesMap.TryGetValue(id, out var attributeNodes))
                {
                    foreach (var attributeNode in attributeNodes)
                    {
                        suppressMessageAttributesToIsUsedMap.Remove(attributeNode);
                    }

                    idToSuppressMessageAttributesMap.Remove(id);
                }
            }

            // Finally, report the unnecessary suppressions.
            var effectiveSeverity = severity.ToDiagnosticSeverity() ?? s_removeUnnecessarySuppressionDescriptor.DefaultSeverity;
            ReportUnnecessarySuppressions(pragmasToIsUsedMap, sortedPragmasWithIds,
                suppressMessageAttributesToIsUsedMap, reportDiagnostic, effectiveSeverity, compilationWithAnalyzers.Compilation);
        }

        private bool ProcessPragmaDirectives(
            SyntaxNode root,
            TextSpan? span,
            PooledDictionary<string, List<(SyntaxTrivia pragma, bool isDisable)>> idToPragmasMap,
            PooledDictionary<SyntaxTrivia, bool> pragmasToIsUsedMap,
            ArrayBuilder<(SyntaxTrivia pragma, ImmutableArray<string> ids, bool isDisable)> sortedPragmasWithIds,
            PooledHashSet<string> compilerDiagnosticIds,
            ImmutableArray<string> userExclusions)
        {
            if (!root.ContainsDirectives)
            {
                return false;
            }

            using var _ = ArrayBuilder<string>.GetInstance(out var idsBuilder);
            var hasPragmaInAnalysisSpan = false;
            foreach (var trivia in root.DescendantTrivia())
            {
                // Check if this is an active pragma with at least one applicable diagnostic ID/error code.
                // Note that a pragma can have multiple error codes, such as '#pragma warning disable ID0001, ID0002'
                if (SyntaxFacts.IsPragmaDirective(trivia, out var isDisable, out var isActive, out var errorCodeNodes) &&
                    isActive &&
                    errorCodeNodes.Count > 0)
                {
                    // Iterate through each ID for this pragma and build the supported IDs.
                    idsBuilder.Clear();
                    foreach (var errorCodeNode in errorCodeNodes)
                    {
                        // Ignore unsupported IDs and those excluded through user option.
                        if (!IsSupportedId(errorCodeNode, out var id, out var isCompilerDiagnosticId) ||
                            userExclusions.Contains(id, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        idsBuilder.Add(id);
                        if (isCompilerDiagnosticId)
                        {
                            compilerDiagnosticIds.Add(id);
                        }

                        // Add entry to idToPragmasMap
                        // Insert the pragmas in reverse order for easier processing later.
                        if (!idToPragmasMap.TryGetValue(id, out var pragmasForIdInReverseOrder))
                        {
                            pragmasForIdInReverseOrder = new List<(SyntaxTrivia pragma, bool isDisable)>();
                            idToPragmasMap.Add(id, pragmasForIdInReverseOrder);
                        }

                        pragmasForIdInReverseOrder.Insert(0, (trivia, isDisable));
                    }

                    if (idsBuilder.Count == 0)
                    {
                        // No supported ID in this pragma.
                        continue;
                    }

                    hasPragmaInAnalysisSpan = hasPragmaInAnalysisSpan || !span.HasValue || span.Value.OverlapsWith(trivia.Span);

                    sortedPragmasWithIds.Add((trivia, idsBuilder.ToImmutable(), isDisable));

                    // Pragma directive is initialized as unnecessary at the start of the algorithm (value = false).
                    // We will subsequently find required/used pragmas and update the entries in this map (value = true).
                    pragmasToIsUsedMap.Add(trivia, false);
                }
            }

            return hasPragmaInAnalysisSpan;
        }

        private bool IsSupportedId(
            SyntaxNode idNode,
            [NotNullWhen(returnValue: true)] out string? id,
            out bool isCompilerDiagnosticId)
        {
            id = idNode.ToString();

            // Compiler diagnostic pragma suppressions allow specifying just the integral ID.
            // For example:
            //      "#pragma warning disable 0168" OR "#pragma warning disable 168"
            // is equivalent to
            //      "#pragma warning disable CS0168"
            // We handle all the three supported formats for compiler diagnostic pragmas.

            var idWithoutPrefix = id.StartsWith(CompilerErrorCodePrefix) && id.Length == CompilerErrorCodePrefix.Length + CompilerErrorCodeDigitCount
                ? id.Substring(CompilerErrorCodePrefix.Length)
                : id;

            // ID without prefix should parse as an integer for compiler diagnostics.
            if (int.TryParse(idWithoutPrefix, out var errorCode))
            {
                // Normalize the ID to always be in the format with prefix.
                id = CompilerErrorCodePrefix + errorCode.ToString($"D{CompilerErrorCodeDigitCount}");
                isCompilerDiagnosticId = true;
                return _lazySupportedCompilerErrorCodes.Value.Contains(errorCode);
            }

            isCompilerDiagnosticId = false;
            return IsSupportedAnalyzerDiagnosticId(id) &&
                idWithoutPrefix == id;
        }

        private static bool IsSupportedAnalyzerDiagnosticId(string id)
        {
            switch (id)
            {
                case IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId:
                    // Not supported as this would lead to recursion in computation.
                    return false;

                case "format":
                case IDEDiagnosticIds.FormattingDiagnosticId:
                    // Formatting analyzer is not supported as the analyzer does not seem to return suppressed IDE0055 diagnostics.
                    return false;

                default:
                    return true;
            }
        }

        private static (ImmutableArray<string> userExclusions, bool analyzerDisabled) ParseUserExclusions(string? userExclusions)
        {
            // Option value must be a comma separate list of diagnostic IDs to exclude from unnecessary pragma analysis.
            // We also allow a special keyword "all" to disable the analyzer completely.
            switch (userExclusions)
            {
                case "":
                case null:
                    return (userExclusions: ImmutableArray<string>.Empty, analyzerDisabled: false);

                case "all":
                    return (userExclusions: ImmutableArray<string>.Empty, analyzerDisabled: true);

                default:
                    // Default string representation for unconfigured option value should be treated as no exclusions.
                    if (userExclusions == CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions.DefaultValue)
                        return (userExclusions: ImmutableArray<string>.Empty, analyzerDisabled: false);

                    break;
            }

            using var _ = ArrayBuilder<string>.GetInstance(out var builder);
            foreach (var part in userExclusions.Split(','))
            {
                var trimmedPart = part.Trim();
                builder.Add(trimmedPart);
            }

            return (userExclusions: builder.ToImmutable(), analyzerDisabled: false);
        }

        private static async Task<(ImmutableArray<Diagnostic> reportedDiagnostics, ImmutableArray<string> unhandledIds)> GetReportedDiagnosticsForIdsAsync(
            ImmutableHashSet<string> idsToAnalyze,
            SyntaxNode root,
            SemanticModel semanticModel,
            CompilationWithAnalyzers compilationWithAnalyzers,
            Func<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>> getSupportedDiagnostics,
            Func<DiagnosticAnalyzer, bool> getIsCompilationEndAnalyzer,
            PooledHashSet<string> compilerDiagnosticIds,
            CancellationToken cancellationToken)
        {
            using var _1 = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var analyzersBuilder);
            using var _2 = ArrayBuilder<string>.GetInstance(out var unhandledIds);

            // First, we compute the relevant analyzers whose reported diagnostics need to be computed.
            var addedCompilerAnalyzer = false;
            var hasNonCompilerAnalyzers = idsToAnalyze.Count > compilerDiagnosticIds.Count;
            foreach (var analyzer in compilationWithAnalyzers.Analyzers)
            {
                if (!addedCompilerAnalyzer &&
                    analyzer.IsCompilerAnalyzer())
                {
                    addedCompilerAnalyzer = true;
                    analyzersBuilder.Add(analyzer);

                    if (!hasNonCompilerAnalyzers)
                    {
                        break;
                    }

                    continue;
                }

                if (hasNonCompilerAnalyzers)
                {
                    Debug.Assert(!analyzer.IsCompilerAnalyzer());

                    bool? lazyIsUnhandledAnalyzer = null;
                    foreach (var descriptor in getSupportedDiagnostics(analyzer))
                    {
                        if (!idsToAnalyze.Contains(descriptor.Id))
                        {
                            continue;
                        }

                        lazyIsUnhandledAnalyzer ??= getIsCompilationEndAnalyzer(analyzer) || analyzer is IPragmaSuppressionsAnalyzer;
                        if (lazyIsUnhandledAnalyzer.Value)
                        {
                            unhandledIds.Add(descriptor.Id);
                        }
                    }

                    if (lazyIsUnhandledAnalyzer.HasValue && !lazyIsUnhandledAnalyzer.Value)
                    {
                        analyzersBuilder.Add(analyzer);
                    }
                }
            }

            // Then, we execute these analyzers on the current file to fetch these diagnostics.
            // Note that if an analyzer has already executed, then this will be just a cache access
            // as computed analyzer diagnostics are cached on CompilationWithAnalyzers instance.

            using var _3 = ArrayBuilder<Diagnostic>.GetInstance(out var reportedDiagnostics);
            if (!addedCompilerAnalyzer && compilerDiagnosticIds.Count > 0)
            {
                // Special case when compiler analyzer could not be found.
                Debug.Assert(semanticModel.Compilation.Options.ReportSuppressedDiagnostics);
                reportedDiagnostics.AddRange(root.GetDiagnostics());
                reportedDiagnostics.AddRange(semanticModel.GetDiagnostics(cancellationToken: cancellationToken));
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (analyzersBuilder.Count > 0)
            {
                var analyzers = analyzersBuilder.ToImmutable();

                var syntaxDiagnostics = await compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(semanticModel.SyntaxTree, analyzers, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                reportedDiagnostics.AddRange(syntaxDiagnostics);

                var semanticDiagnostics = await compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(semanticModel, filterSpan: null, analyzers, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                reportedDiagnostics.AddRange(semanticDiagnostics);
            }

            return (reportedDiagnostics.ToImmutable(), unhandledIds.ToImmutable());
        }

        private static async Task ProcessReportedDiagnosticsAsync(
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxTree tree,
            CompilationWithAnalyzers compilationWithAnalyzers,
            PooledDictionary<string, List<(SyntaxTrivia pragma, bool isDisable)>> idToPragmasMap,
            PooledDictionary<SyntaxTrivia, bool> pragmasToIsUsedMap,
            PooledDictionary<string, List<SyntaxNode>> idToSuppressMessageAttributesMap,
            PooledDictionary<SyntaxNode, bool> suppressMessageAttributesToIsUsedMap,
            CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (!diagnostic.IsSuppressed)
                {
                    continue;
                }

                var suppressionInfo = diagnostic.GetSuppressionInfo(compilationWithAnalyzers.Compilation);
                if (suppressionInfo == null)
                {
                    continue;
                }

                if (suppressionInfo.Attribute is { } attribute)
                {
                    await ProcessAttributeSuppressionsAsync(diagnostic, attribute,
                        idToSuppressMessageAttributesMap, suppressMessageAttributesToIsUsedMap, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    ProcessPragmaSuppressions(diagnostic, tree, idToPragmasMap, pragmasToIsUsedMap);
                }
            }

            return;

            static void ProcessPragmaSuppressions(
                Diagnostic diagnostic,
                SyntaxTree tree,
                PooledDictionary<string, List<(SyntaxTrivia pragma, bool isDisable)>> idToPragmasMap,
                PooledDictionary<SyntaxTrivia, bool> pragmasToIsUsedMap)
            {
                if (!idToPragmasMap.TryGetValue(diagnostic.Id, out var pragmasForIdInReverseOrder))
                {
                    return;
                }

                Debug.Assert(diagnostic.Location.IsInSource);
                Debug.Assert(diagnostic.Location.SourceTree == tree);

                // Process the pragmas for the document bottom-up,
                // finding the first disable pragma directive before the diagnostic span.
                // Mark this pragma and the corresponding enable pragma directive as used.
                SyntaxTrivia? lastEnablePragma = null;
                foreach (var (pragma, isDisable) in pragmasForIdInReverseOrder)
                {
                    if (isDisable)
                    {
                        if (pragma.Span.End <= diagnostic.Location.SourceSpan.Start)
                        {
                            pragmasToIsUsedMap[pragma] = true;
                            if (lastEnablePragma.HasValue)
                            {
                                pragmasToIsUsedMap[lastEnablePragma.Value] = true;
                            }

                            break;
                        }
                    }
                    else
                    {
                        lastEnablePragma = pragma;
                    }
                }
            }

            static async Task ProcessAttributeSuppressionsAsync(
                Diagnostic diagnostic,
                AttributeData attribute,
                PooledDictionary<string, List<SyntaxNode>> idToSuppressMessageAttributesMap,
                PooledDictionary<SyntaxNode, bool> suppressMessageAttributesToIsUsedMap,
                CancellationToken cancellationToken)
            {
                if (attribute.ApplicationSyntaxReference == null ||
                    !idToSuppressMessageAttributesMap.TryGetValue(diagnostic.Id, out var suppressMessageAttributesForId))
                {
                    return;
                }

                var attributeNode = await attribute.ApplicationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                foreach (var node in suppressMessageAttributesForId)
                {
                    if (attributeNode == node)
                    {
                        suppressMessageAttributesToIsUsedMap[attributeNode] = true;
                        return;
                    }
                }
            }
        }

        private static void ReportUnnecessarySuppressions(
            PooledDictionary<SyntaxTrivia, bool> pragmasToIsUsedMap,
            ArrayBuilder<(SyntaxTrivia pragma, ImmutableArray<string> ids, bool isDisable)> sortedPragmasWithIds,
            PooledDictionary<SyntaxNode, bool> suppressMessageAttributesToIsUsedMap,
            Action<Diagnostic> reportDiagnostic,
            DiagnosticSeverity severity,
            Compilation compilation)
        {
            using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnosticsBuilder);
            AddUnnecessaryPragmaDiagnostics(diagnosticsBuilder, pragmasToIsUsedMap, sortedPragmasWithIds, severity);
            AddUnnecessarySuppressMessageAttributeDiagnostics(diagnosticsBuilder, suppressMessageAttributesToIsUsedMap, severity);

            // Apply the diagnostic filtering
            var effectiveDiagnostics = CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnosticsBuilder, compilation);
            foreach (var diagnostic in effectiveDiagnostics)
            {
                reportDiagnostic(diagnostic);
            }

            return;

            static void AddUnnecessaryPragmaDiagnostics(
                ArrayBuilder<Diagnostic> diagnosticsBuilder,
                PooledDictionary<SyntaxTrivia, bool> pragmasToIsUsedMap,
                ArrayBuilder<(SyntaxTrivia pragma, ImmutableArray<string> ids, bool isDisable)> sortedPragmasWithIds,
                DiagnosticSeverity severity)
            {
                foreach (var (pragma, isUsed) in pragmasToIsUsedMap)
                {
                    if (!isUsed)
                    {
                        // We found an unnecessary pragma directive.
                        // Try to find a matching disable/restore counterpart that toggles the pragma state.
                        // This enables the code fix to simultaneously remove both the disable and restore directives.
                        // If we don't find a matching pragma, report just the current pragma.
                        ImmutableArray<Location> additionalLocations;
                        if (TryGetTogglingPragmaDirective(pragma, sortedPragmasWithIds, out var togglePragma) &&
                            pragmasToIsUsedMap.TryGetValue(togglePragma, out var isToggleUsed) &&
                            !isToggleUsed)
                        {
                            additionalLocations = ImmutableArray.Create(togglePragma.GetLocation());
                        }
                        else
                        {
                            additionalLocations = ImmutableArray<Location>.Empty;
                        }

                        var diagnostic = Diagnostic.Create(s_removeUnnecessarySuppressionDescriptor, pragma.GetLocation(), severity, additionalLocations, properties: null);
                        diagnosticsBuilder.Add(diagnostic);
                    }
                }
            }

            static void AddUnnecessarySuppressMessageAttributeDiagnostics(
                ArrayBuilder<Diagnostic> diagnosticsBuilder,
                PooledDictionary<SyntaxNode, bool> suppressMessageAttributesToIsUsedMap,
                DiagnosticSeverity severity)
            {
                foreach (var (attribute, isUsed) in suppressMessageAttributesToIsUsedMap)
                {
                    if (!isUsed)
                    {
                        var diagnostic = Diagnostic.Create(s_removeUnnecessarySuppressionDescriptor, attribute.GetLocation(), severity, additionalLocations: null, properties: null);
                        diagnosticsBuilder.Add(diagnostic);
                    }
                }
            }
        }

        private static bool TryGetTogglingPragmaDirective(
            SyntaxTrivia pragma,
            ArrayBuilder<(SyntaxTrivia pragma, ImmutableArray<string> ids, bool isDisable)> sortedPragmasWithIds,
            out SyntaxTrivia togglePragma)
        {
            var indexOfPragma = sortedPragmasWithIds.FindIndex(p => p.pragma == pragma);
            var idsForPragma = sortedPragmasWithIds[indexOfPragma].ids;
            var isDisable = sortedPragmasWithIds[indexOfPragma].isDisable;
            var incrementOrDecrement = isDisable ? 1 : -1;
            var matchingPragmaStackCount = 0;
            for (var i = indexOfPragma + incrementOrDecrement; i >= 0 && i < sortedPragmasWithIds.Count; i += incrementOrDecrement)
            {
                var (nextPragma, nextPragmaIds, nextPragmaIsDisable) = sortedPragmasWithIds[i];
                var intersect = nextPragmaIds.Intersect(idsForPragma).ToImmutableArray();
                if (intersect.IsEmpty)
                {
                    // Unrelated pragma
                    continue;
                }

                if (intersect.Length != idsForPragma.Length)
                {
                    // Partial intersection of IDs - bail out.
                    togglePragma = default;
                    return false;
                }

                // Found a pragma with same IDs.
                // Check if this is a pragma of same kind (disable/restore) or not.
                if (isDisable == nextPragmaIsDisable)
                {
                    // Same pragma kind, increment the stack count
                    matchingPragmaStackCount++;
                }
                else
                {
                    // Found a pragma of opposite kind.
                    if (matchingPragmaStackCount > 0)
                    {
                        // Not matching one for the input pragma, decrement stack count
                        matchingPragmaStackCount--;
                    }
                    else
                    {
                        // Found the match.
                        togglePragma = nextPragma;
                        return true;
                    }
                }
            }

            togglePragma = default;
            return false;
        }

        private async Task<bool> ProcessSuppressMessageAttributesAsync(
            SyntaxNode root,
            SemanticModel semanticModel,
            TextSpan? span,
            PooledDictionary<string, List<SyntaxNode>> idToSuppressMessageAttributesMap,
            PooledDictionary<SyntaxNode, bool> suppressMessageAttributesToIsUsedMap,
            ImmutableArray<string> userExclusions,
            CancellationToken cancellationToken)
        {
            var suppressMessageAttributeType = semanticModel.Compilation.SuppressMessageAttributeType();
            if (suppressMessageAttributeType == null)
            {
                return false;
            }

            var declarationNodes = SyntaxFacts.GetTopLevelAndMethodLevelMembers(root);
            using var _ = PooledHashSet<ISymbol>.GetInstance(out var processedPartialSymbols);
            if (declarationNodes.Count > 0)
            {
                foreach (var node in declarationNodes)
                {
                    if (span.HasValue && !node.FullSpan.Contains(span.Value))
                    {
                        continue;
                    }

                    var symbols = SemanticFacts.GetDeclaredSymbols(semanticModel, node, cancellationToken);
                    foreach (var symbol in symbols)
                    {
                        switch (symbol?.Kind)
                        {
                            // Local SuppressMessageAttributes are only applicable for types and members.
                            case SymbolKind.NamedType:
                            case SymbolKind.Method:
                            case SymbolKind.Field:
                            case SymbolKind.Property:
                            case SymbolKind.Event:
                                break;

                            default:
                                continue;
                        }

                        // Skip already processed symbols from partial declarations
                        var isPartial = symbol.Locations.Length > 1;
                        if (isPartial && !processedPartialSymbols.Add(symbol))
                        {
                            continue;
                        }

                        foreach (var attribute in symbol.GetAttributes())
                        {
                            if (attribute.ApplicationSyntaxReference != null &&
                                TryGetSuppressedDiagnosticId(attribute, suppressMessageAttributeType, out var id))
                            {
                                // Ignore unsupported IDs and those excluded through user option.
                                if (!IsSupportedAnalyzerDiagnosticId(id) ||
                                    userExclusions.Contains(id, StringComparer.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                if (!idToSuppressMessageAttributesMap.TryGetValue(id, out var nodesForId))
                                {
                                    nodesForId = new List<SyntaxNode>();
                                    idToSuppressMessageAttributesMap.Add(id, nodesForId);
                                }

                                var attributeNode = await attribute.ApplicationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                                nodesForId.Add(attributeNode);

                                // Initialize the attribute node as unnecessary at the start of the algorithm.
                                // Later processing will identify attributes which are indeed responsible for suppressing diagnostics
                                // and mark them as used.
                                // NOTE: For attributes on partial symbols with multiple declarations, we conservatively
                                // consider them as used and avoid unnecessary attribute analysis because that would potentially
                                // require analysis across multiple files, which can be expensive from a performance standpoint.
                                suppressMessageAttributesToIsUsedMap.Add(attributeNode, isPartial);
                            }
                        }
                    }
                }
            }

            return idToSuppressMessageAttributesMap.Count > 0;
        }

        private static bool TryGetSuppressedDiagnosticId(
            AttributeData attribute,
            INamedTypeSymbol suppressMessageAttributeType,
            [NotNullWhen(returnValue: true)] out string? id)
        {
            if (suppressMessageAttributeType.Equals(attribute.AttributeClass) &&
                attribute.AttributeConstructor?.Parameters.Length >= 2 &&
                attribute.AttributeConstructor.Parameters[1].Name == "checkId" &&
                attribute.AttributeConstructor.Parameters[1].Type.SpecialType == SpecialType.System_String &&
                attribute.ConstructorArguments.Length >= 2 &&
                attribute.ConstructorArguments[1] is { } typedConstant &&
                typedConstant.Kind == TypedConstantKind.Primitive &&
                typedConstant.Value is string checkId)
            {
                // CheckId represents diagnostic ID, followed by an option ':' and name.
                // For example, "CA1801:ReviewUnusedParameters"
                var index = checkId.IndexOf(':');
                id = index > 0 ? checkId.Substring(0, index) : checkId;
                return id.Length > 0;
            }

            id = null;
            return false;
        }
    }
}
