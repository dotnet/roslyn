// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    using Editor.Shared.Utilities;
    using DiagnosticId = String;
    using LanguageKind = String;

    [Export(typeof(ICodeFixService)), Shared]
    internal partial class CodeFixService : ForegroundThreadAffinitizedObject, ICodeFixService
    {
        private static readonly Comparison<DiagnosticData> s_diagnosticDataComparisonById =
            new Comparison<DiagnosticData>((d1, d2) => DiagnosticId.CompareOrdinal(d1.Id, d2.Id));

        private readonly IDiagnosticAnalyzerService _diagnosticService;

        private readonly Func<Workspace, ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>>> _getWorkspaceFixersMap;
        private ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>>? _lazyWorkspaceFixersMap;
        private readonly ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, ImmutableDictionary<DiagnosticId, List<CodeFixProvider>>> _projectFixersMap;

        // Shared by project fixers and workspace fixers.
        private ImmutableDictionary<CodeFixProvider, ImmutableArray<DiagnosticId>> _fixerToFixableIdsMap = ImmutableDictionary<CodeFixProvider, ImmutableArray<DiagnosticId>>.Empty;

        private readonly Func<Workspace, ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<CodeFixProvider, int>>>> _getFixerPriorityMap;
        private ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<CodeFixProvider, int>>>? _lazyFixerPriorityMap;

        private readonly ConditionalWeakTable<AnalyzerReference, ProjectCodeFixProvider> _analyzerReferenceToFixersMap;
        private readonly ConditionalWeakTable<AnalyzerReference, ProjectCodeFixProvider>.CreateValueCallback _createProjectCodeFixProvider;

        private readonly ImmutableDictionary<LanguageKind, Lazy<ImmutableArray<IConfigurationFixProvider>>> _configurationProvidersMap;
        private readonly IEnumerable<Lazy<IErrorLoggerService>> _errorLoggers;

        private ImmutableDictionary<object, FixAllProviderInfo> _fixAllProviderMap;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CodeFixService(
            IThreadingContext threadingContext,
            IDiagnosticAnalyzerService diagnosticAnalyzerService,
            [ImportMany] IEnumerable<Lazy<IErrorLoggerService>> loggers,
            [ImportMany] IEnumerable<Lazy<CodeFixProvider, CodeChangeProviderMetadata>> fixers,
            [ImportMany] IEnumerable<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>> configurationProviders)
            : base(threadingContext, assertIsForeground: false)
        {
            _errorLoggers = loggers;
            _diagnosticService = diagnosticAnalyzerService;
            var fixersPerLanguageMap = fixers.ToPerLanguageMapWithMultipleLanguages();
            var configurationProvidersPerLanguageMap = configurationProviders.ToPerLanguageMapWithMultipleLanguages();

            _getWorkspaceFixersMap = workspace => GetFixerPerLanguageMap(fixersPerLanguageMap, workspace);
            _configurationProvidersMap = GetConfigurationProvidersPerLanguageMap(configurationProvidersPerLanguageMap);

            // REVIEW: currently, fixer's priority is statically defined by the fixer itself. might considering making it more dynamic or configurable.
            _getFixerPriorityMap = workspace => GetFixerPriorityPerLanguageMap(fixersPerLanguageMap, workspace);

            // Per-project fixers
            _projectFixersMap = new ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, ImmutableDictionary<string, List<CodeFixProvider>>>();
            _analyzerReferenceToFixersMap = new ConditionalWeakTable<AnalyzerReference, ProjectCodeFixProvider>();
            _createProjectCodeFixProvider = new ConditionalWeakTable<AnalyzerReference, ProjectCodeFixProvider>.CreateValueCallback(r => new ProjectCodeFixProvider(r));
            _fixAllProviderMap = ImmutableDictionary<object, FixAllProviderInfo>.Empty;
        }

        public async Task<FirstDiagnosticResult> GetMostSevereFixableDiagnosticAsync(
            Document document, TextSpan range, CancellationToken cancellationToken)
        {
            if (document == null || !document.IsOpen())
            {
                return default;
            }

            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var diagnostics);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var linkedToken = linkedTokenSource.Token;

            // This flag is used by SuggestedActionsSource to track what solution is was
            // last able to get "full results" for.
            var isFullResult = await _diagnosticService.TryAppendDiagnosticsForSpanAsync(
                document, range, diagnostics, cancellationToken: linkedToken).ConfigureAwait(false);

            var errorDiagnostics = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
            var otherDiagnostics = diagnostics.Where(d => d.Severity != DiagnosticSeverity.Error);

            // Kick off a task that will determine there's an Error Diagnostic with a fixer
            var errorDiagnosticsTask = Task.Run(
                () => GetFirstDiagnosticWithFixAsync(document, errorDiagnostics, range, linkedToken),
                linkedToken);

            // Kick off a task that will determine if any non-Error Diagnostic has a fixer
            var otherDiagnosticsTask = Task.Run(
                () => GetFirstDiagnosticWithFixAsync(document, otherDiagnostics, range, linkedToken),
                linkedToken);

            // If the error diagnostics task happens to complete with a non-null result before
            // the other diagnostics task, we can cancel the other task.
            var diagnostic = await errorDiagnosticsTask.ConfigureAwait(false)
                ?? await otherDiagnosticsTask.ConfigureAwait(false);
            linkedTokenSource.Cancel();

            return new FirstDiagnosticResult(partialResult: !isFullResult,
                                   hasFix: diagnostic != null,
                                   diagnostic: diagnostic);
        }

        private async Task<DiagnosticData?> GetFirstDiagnosticWithFixAsync(
            Document document,
            IEnumerable<DiagnosticData> severityGroup,
            TextSpan range,
            CancellationToken cancellationToken)
        {
            foreach (var diagnostic in severityGroup)
            {
                if (!range.IntersectsWith(diagnostic.GetTextSpan()))
                {
                    continue;
                }

                if (await ContainsAnyFixAsync(document, diagnostic, cancellationToken).ConfigureAwait(false))
                {
                    return diagnostic;
                }
            }

            return null;
        }

        public Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(Document document, TextSpan range, bool includeConfigurationFixes, CancellationToken cancellationToken)
            => GetFixesAsync(document, range, includeConfigurationFixes, isBlocking: false, cancellationToken);

        public Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(Document document, TextSpan range, bool includeConfigurationFixes, bool isBlocking, CancellationToken cancellationToken)
            => GetFixesAsync(document, range, includeConfigurationFixes, isBlocking, addOperationScope: _ => null, cancellationToken);

        public async Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(Document document, TextSpan range, bool includeConfigurationFixes, bool isBlocking, Func<string, IDisposable?> addOperationScope, CancellationToken cancellationToken)
        {
            // REVIEW: this is the first and simplest design. basically, when ctrl+. is pressed, it asks diagnostic service to give back
            // current diagnostics for the given span, and it will use that to get fixes. internally diagnostic service will either return cached information
            // (if it is up-to-date) or synchronously do the work at the spot.
            //
            // this design's weakness is that each side don't have enough information to narrow down works to do. it will most likely always do more works than needed.
            // sometimes way more than it is needed. (compilation)

            // group diagnostics by their diagnostics span
            // invariant: later code gathers & runs CodeFixProviders for diagnostics with one identical diagnostics span (that gets set later as CodeFixCollection's TextSpan)
            // order diagnostics by span.
            SortedDictionary<TextSpan, List<DiagnosticData>>? aggregatedDiagnostics = null;
            foreach (var diagnostic in await _diagnosticService.GetDiagnosticsForSpanAsync(document, range, diagnosticIdOpt: null, includeConfigurationFixes, addOperationScope, cancellationToken).ConfigureAwait(false))
            {
                if (diagnostic.IsSuppressed)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                aggregatedDiagnostics ??= new SortedDictionary<TextSpan, List<DiagnosticData>>();
                aggregatedDiagnostics.GetOrAdd(diagnostic.GetTextSpan(), _ => new List<DiagnosticData>()).Add(diagnostic);
            }

            if (aggregatedDiagnostics == null)
            {
                return ImmutableArray<CodeFixCollection>.Empty;
            }

            // Order diagnostics by DiagnosticId so the fixes are in a deterministic order.
            foreach (var diagnosticsWithSpan in aggregatedDiagnostics.Values)
            {
                diagnosticsWithSpan.Sort(s_diagnosticDataComparisonById);
            }

            // append fixes for all diagnostics with the same diagnostics span
            using var resultDisposer = ArrayBuilder<CodeFixCollection>.GetInstance(out var result);
            foreach (var spanAndDiagnostic in aggregatedDiagnostics)
            {
                await AppendFixesAsync(
                    document, spanAndDiagnostic.Key, spanAndDiagnostic.Value, fixAllForInSpan: false, isBlocking,
                    result, addOperationScope, cancellationToken).ConfigureAwait(false);
            }

            if (result.Count > 0 && TryGetWorkspaceFixersPriorityMap(document, out var fixersForLanguage))
            {
                // sort the result to the order defined by the fixers
#pragma warning disable IDE0007 // Use implicit type - Explicit type is need to suppress an incorrect nullable warning on dereferencing the map.
                ImmutableDictionary<CodeFixProvider, int> priorityMap = fixersForLanguage.Value;
#pragma warning restore IDE0007 // Use implicit type
                result.Sort((d1, d2) => GetValue(d1).CompareTo(GetValue(d2)));

                int GetValue(CodeFixCollection c)
                    => priorityMap.TryGetValue((CodeFixProvider)c.Provider, out var value) ? value : int.MaxValue;
            }

            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            if (document.Project.Solution.Workspace.Kind != WorkspaceKind.Interactive && includeConfigurationFixes)
            {
                // Ensure that we do not register duplicate configuration fixes.
                using var _ = PooledHashSet<string>.GetInstance(out var registeredConfigurationFixTitles);
                foreach (var spanAndDiagnostic in aggregatedDiagnostics)
                {
                    await AppendConfigurationsAsync(
                        document, spanAndDiagnostic.Key, spanAndDiagnostic.Value,
                        result, registeredConfigurationFixTitles, cancellationToken).ConfigureAwait(false);
                }
            }

            return result.ToImmutable();
        }

        public async Task<CodeFixCollection?> GetDocumentFixAllForIdInSpanAsync(Document document, TextSpan range, string diagnosticId, CancellationToken cancellationToken)
        {
            var diagnostics = (await _diagnosticService.GetDiagnosticsForSpanAsync(document, range, diagnosticId, includeSuppressedDiagnostics: false, cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();
            if (diagnostics.Count == 0)
            {
                return null;
            }

            using var resultDisposer = ArrayBuilder<CodeFixCollection>.GetInstance(out var result);
            await AppendFixesAsync(document, range, diagnostics, fixAllForInSpan: true, isBlocking: false, result, addOperationScope: _ => null, cancellationToken).ConfigureAwait(false);

            // TODO: Just get the first fix for now until we have a way to config user's preferred fix
            // https://github.com/dotnet/roslyn/issues/27066
            return result.ToImmutable().FirstOrDefault();
        }

        public async Task<Document> ApplyCodeFixesForSpecificDiagnosticIdAsync(Document document, string diagnosticId, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = new TextSpan(0, tree.Length);

            var fixCollection = await GetDocumentFixAllForIdInSpanAsync(
                document, textSpan, diagnosticId, cancellationToken).ConfigureAwait(false);
            if (fixCollection == null)
            {
                return document;
            }

            var fixAllService = document.Project.Solution.Workspace.Services.GetRequiredService<IFixAllGetFixesService>();

            var solution = await fixAllService.GetFixAllChangedSolutionAsync(
                new FixAllContext(fixCollection.FixAllState, progressTracker, cancellationToken)).ConfigureAwait(false);

            return solution.GetDocument(document.Id) ?? throw new NotSupportedException(EditorFeaturesResources.Removal_of_document_not_supported);
        }

        private bool TryGetWorkspaceFixersMap(Document document, out Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>> fixerMap)
        {
            if (_lazyWorkspaceFixersMap == null)
            {
                var workspaceFixersMap = _getWorkspaceFixersMap(document.Project.Solution.Workspace);
                Interlocked.CompareExchange(ref _lazyWorkspaceFixersMap, workspaceFixersMap, null);
            }

            return _lazyWorkspaceFixersMap.TryGetValue(document.Project.Language, out fixerMap);
        }

        private bool TryGetWorkspaceFixersPriorityMap(Document document, out Lazy<ImmutableDictionary<CodeFixProvider, int>> fixersPriorityMap)
        {
            if (_lazyFixerPriorityMap == null)
            {
                var fixersPriorityByLanguageMap = _getFixerPriorityMap(document.Project.Solution.Workspace);
                Interlocked.CompareExchange(ref _lazyFixerPriorityMap, fixersPriorityByLanguageMap, null);
            }

            return _lazyFixerPriorityMap.TryGetValue(document.Project.Language, out fixersPriorityMap);
        }

        private bool TryGetWorkspaceFixer(
            Lazy<CodeFixProvider, CodeChangeProviderMetadata> lazyFixer,
            Workspace workspace,
            bool logExceptionWithInfoBar,
            [NotNullWhen(returnValue: true)] out CodeFixProvider? fixer)
        {
            try
            {
                fixer = lazyFixer.Value;
                return true;
            }
            catch (Exception ex)
            {
                // Gracefully handle exceptions in creating fixer instance.
                // Log exception and show info bar, if needed.
                if (logExceptionWithInfoBar)
                {
                    var errorReportingService = workspace.Services.GetRequiredService<IErrorReportingService>();
                    var message = lazyFixer.Metadata.Name != null
                        ? string.Format(EditorFeaturesResources.Error_creating_instance_of_CodeFixProvider_0, lazyFixer.Metadata.Name)
                        : EditorFeaturesResources.Error_creating_instance_of_CodeFixProvider;

                    errorReportingService.ShowErrorInfoInActiveView(
                        message,
                        new InfoBarUI(
                            WorkspacesResources.Show_Stack_Trace,
                            InfoBarUI.UIKind.HyperLink,
                            () => errorReportingService.ShowDetailedErrorInfo(ex), closeAfterAction: true));

                    foreach (var errorLogger in _errorLoggers)
                    {
                        errorLogger.Value.LogException(this, ex);
                    }
                }

                fixer = null;
                return false;
            }
        }

        private async Task AppendFixesAsync(
            Document document,
            TextSpan span,
            IEnumerable<DiagnosticData> diagnostics,
            bool fixAllForInSpan,
            bool isBlocking,
            ArrayBuilder<CodeFixCollection> result,
            Func<string, IDisposable?> addOperationScope,
            CancellationToken cancellationToken)
        {
            var hasAnySharedFixer = TryGetWorkspaceFixersMap(document, out var fixerMap);

            var projectFixersMap = GetProjectFixers(document.Project);
            var hasAnyProjectFixer = projectFixersMap.Any();

            if (!hasAnySharedFixer && !hasAnyProjectFixer)
            {
                return;
            }

            var allFixers = new List<CodeFixProvider>();

            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            var isInteractive = document.Project.Solution.Workspace.Kind == WorkspaceKind.Interactive;

            // gather CodeFixProviders for all distinct diagnostics found for current span
            foreach (var diagnosticId in diagnostics.Select(d => d.Id).Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Prioritize NuGet based project code fixers over VSIX based workspace code fixers.
                if (hasAnyProjectFixer && projectFixersMap.TryGetValue(diagnosticId, out var projectFixers))
                {
                    Debug.Assert(!isInteractive);
                    allFixers.AddRange(projectFixers);
                }

                if (hasAnySharedFixer && fixerMap.Value.TryGetValue(diagnosticId, out var workspaceFixers))
                {
                    if (isInteractive)
                    {
                        allFixers.AddRange(workspaceFixers.Where(IsInteractiveCodeFixProvider));
                    }
                    else
                    {
                        allFixers.AddRange(workspaceFixers);
                    }
                }
            }

            var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>();

            // Run each CodeFixProvider to gather individual CodeFixes for reported diagnostics.
            // Ensure that no diagnostic has registered code actions from different code fix providers with same equivalance key.
            // This prevents duplicate registered code actions from NuGet and VSIX code fix providers.
            // See https://github.com/dotnet/roslyn/issues/18818 for details.
            var uniqueDiagosticToEquivalenceKeysMap = new Dictionary<Diagnostic, PooledHashSet<string?>>();

            // NOTE: For backward compatibility, we allow multiple registered code actions from the same code fix provider
            // to have the same equivalence key. See https://github.com/dotnet/roslyn/issues/44553 for details.
            // To ensure this, we track the fixer that first registered a code action to fix a diagnostic with a specific equivalence key.
            var diagnosticAndEquivalenceKeyToFixersMap = new Dictionary<(Diagnostic diagnostic, string? equivalenceKey), CodeFixProvider>();

            try
            {
                foreach (var fixer in allFixers.Distinct())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await AppendFixesOrConfigurationsAsync(
                        document, span, diagnostics, fixAllForInSpan, result, fixer,
                        hasFix: d => this.GetFixableDiagnosticIds(fixer, extensionManager).Contains(d.Id),
                        getFixes: dxs =>
                        {
                            var fixerName = fixer.GetType().Name;
                            using (addOperationScope(fixerName))
                            using (RoslynEventSource.LogInformationalBlock(FunctionId.CodeFixes_GetCodeFixesAsync, fixerName, cancellationToken))
                            {
                                if (fixAllForInSpan)
                                {
                                    var primaryDiagnostic = dxs.First();
                                    return GetCodeFixesAsync(document, primaryDiagnostic.Location.SourceSpan, fixer, isBlocking,
                                        ImmutableArray.Create(primaryDiagnostic), uniqueDiagosticToEquivalenceKeysMap,
                                        diagnosticAndEquivalenceKeyToFixersMap, cancellationToken);
                                }
                                else
                                {
                                    return GetCodeFixesAsync(document, span, fixer, isBlocking, dxs,
                                        uniqueDiagosticToEquivalenceKeysMap, diagnosticAndEquivalenceKeyToFixersMap, cancellationToken);
                                }
                            }
                        },
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    // Just need the first result if we are doing fix all in span
                    if (fixAllForInSpan && result.Any())
                        return;
                }
            }
            finally
            {
                foreach (var pooledSet in uniqueDiagosticToEquivalenceKeysMap.Values)
                {
                    pooledSet.Free();
                }
            }
        }

        private static async Task<ImmutableArray<CodeFix>> GetCodeFixesAsync(
            Document document, TextSpan span, CodeFixProvider fixer, bool isBlocking,
            ImmutableArray<Diagnostic> diagnostics,
            Dictionary<Diagnostic, PooledHashSet<string?>> uniqueDiagosticToEquivalenceKeysMap,
            Dictionary<(Diagnostic diagnostic, string? equivalenceKey), CodeFixProvider> diagnosticAndEquivalenceKeyToFixersMap,
            CancellationToken cancellationToken)
        {
            using var fixesDisposer = ArrayBuilder<CodeFix>.GetInstance(out var fixes);
            var context = new CodeFixContext(document, span, diagnostics,
                // TODO: Can we share code between similar lambdas that we pass to this API in BatchFixAllProvider.cs, CodeFixService.cs and CodeRefactoringService.cs?
                (action, applicableDiagnostics) =>
                {
                    // Serialize access for thread safety - we don't know what thread the fix provider will call this delegate from.
                    lock (fixes)
                    {
                        // Filter out applicable diagnostics which already have a registered code action with same equivalence key.
                        applicableDiagnostics = FilterApplicableDiagnostics(applicableDiagnostics, action.EquivalenceKey,
                            fixer, uniqueDiagosticToEquivalenceKeysMap, diagnosticAndEquivalenceKeyToFixersMap);

                        if (!applicableDiagnostics.IsEmpty)
                        {
                            fixes.Add(new CodeFix(document.Project, action, applicableDiagnostics));
                        }
                    }
                },
                verifyArguments: false,
                isBlocking,
                cancellationToken: cancellationToken);

            var task = fixer.RegisterCodeFixesAsync(context) ?? Task.CompletedTask;
            await task.ConfigureAwait(false);
            return fixes.ToImmutable();

            static ImmutableArray<Diagnostic> FilterApplicableDiagnostics(
                ImmutableArray<Diagnostic> applicableDiagnostics,
                string? equivalenceKey,
                CodeFixProvider fixer,
                Dictionary<Diagnostic, PooledHashSet<string?>> uniqueDiagosticToEquivalenceKeysMap,
                Dictionary<(Diagnostic diagnostic, string? equivalenceKey), CodeFixProvider> diagnosticAndEquivalenceKeyToFixersMap)
            {
                using var disposer = ArrayBuilder<Diagnostic>.GetInstance(out var newApplicableDiagnostics);
                foreach (var diagnostic in applicableDiagnostics)
                {
                    if (!uniqueDiagosticToEquivalenceKeysMap.TryGetValue(diagnostic, out var equivalenceKeys))
                    {
                        // First code action registered to fix this diagnostic with any equivalenceKey.
                        // Record the equivalence key and the fixer that registered this action.
                        equivalenceKeys = PooledHashSet<string?>.GetInstance();
                        equivalenceKeys.Add(equivalenceKey);
                        uniqueDiagosticToEquivalenceKeysMap[diagnostic] = equivalenceKeys;
                        diagnosticAndEquivalenceKeyToFixersMap.Add((diagnostic, equivalenceKey), fixer);
                    }
                    else if (equivalenceKeys.Add(equivalenceKey))
                    {
                        // First code action registered to fix this diagnostic with the given equivalenceKey.
                        // Record the the fixer that registered this action.
                        diagnosticAndEquivalenceKeyToFixersMap.Add((diagnostic, equivalenceKey), fixer);
                    }
                    else if (diagnosticAndEquivalenceKeyToFixersMap[(diagnostic, equivalenceKey)] != fixer)
                    {
                        // Diagnostic already has a registered code action with same equivalence key from a different fixer.
                        // Note that we allow same fixer to register multiple such code actions with the same equivalence key
                        // for backward compatibility. See https://github.com/dotnet/roslyn/issues/44553 for details.
                        continue;
                    }

                    newApplicableDiagnostics.Add(diagnostic);
                }

                return newApplicableDiagnostics.Count == applicableDiagnostics.Length
                    ? applicableDiagnostics
                    : newApplicableDiagnostics.ToImmutable();
            }
        }

        private async Task AppendConfigurationsAsync(
            Document document,
            TextSpan diagnosticsSpan,
            IEnumerable<DiagnosticData> diagnostics,
            ArrayBuilder<CodeFixCollection> result,
            PooledHashSet<string> registeredConfigurationFixTitles,
            CancellationToken cancellationToken)
        {
            if (!_configurationProvidersMap.TryGetValue(document.Project.Language, out var lazyConfigurationProviders) || lazyConfigurationProviders.Value == null)
            {
                return;
            }

            // append CodeFixCollection for each CodeFixProvider
            foreach (var provider in lazyConfigurationProviders.Value)
            {
                using (RoslynEventSource.LogInformationalBlock(FunctionId.CodeFixes_GetCodeFixesAsync, provider, cancellationToken))
                {
                    await AppendFixesOrConfigurationsAsync(
                        document, diagnosticsSpan, diagnostics, fixAllForInSpan: false, result, provider,
                        hasFix: d => provider.IsFixableDiagnostic(d),
                        getFixes: async dxs =>
                        {
                            var fixes = await provider.GetFixesAsync(document, diagnosticsSpan, dxs, cancellationToken).ConfigureAwait(false);
                            return fixes.WhereAsArray(f => registeredConfigurationFixTitles.Add(f.Action.Title));
                        },
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task AppendFixesOrConfigurationsAsync<TCodeFixProvider>(
            Document document,
            TextSpan fixesSpan,
            IEnumerable<DiagnosticData> diagnosticsWithSameSpan,
            bool fixAllForInSpan,
            ArrayBuilder<CodeFixCollection> result,
            TCodeFixProvider fixer,
            Func<Diagnostic, bool> hasFix,
            Func<ImmutableArray<Diagnostic>, Task<ImmutableArray<CodeFix>>> getFixes,
            CancellationToken cancellationToken)
            where TCodeFixProvider : notnull
        {
            var allDiagnostics =
                await diagnosticsWithSameSpan.OrderByDescending(d => d.Severity)
                                             .ToDiagnosticsAsync(document.Project, cancellationToken).ConfigureAwait(false);
            var diagnostics = allDiagnostics.WhereAsArray(hasFix);
            if (diagnostics.Length <= 0)
            {
                // this can happen for suppression case where all diagnostics can't be suppressed
                return;
            }

            var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();
            var fixes = await extensionManager.PerformFunctionAsync(fixer,
                 () => getFixes(diagnostics),
                defaultValue: ImmutableArray<CodeFix>.Empty).ConfigureAwait(false);

            if (fixes.IsDefaultOrEmpty)
            {
                return;
            }

            // If the fix provider supports fix all occurrences, then get the corresponding FixAllProviderInfo and fix all context.
            var fixAllProviderInfo = extensionManager.PerformFunction<FixAllProviderInfo?>(fixer, () => ImmutableInterlocked.GetOrAdd(ref _fixAllProviderMap, fixer, FixAllProviderInfo.Create), defaultValue: null);

            FixAllState? fixAllState = null;
            var supportedScopes = ImmutableArray<FixAllScope>.Empty;
            if (fixAllProviderInfo != null)
            {
                var codeFixProvider = (fixer as CodeFixProvider) ?? new WrapperCodeFixProvider((IConfigurationFixProvider)fixer, diagnostics.Select(d => d.Id));

                var diagnosticIds = diagnostics.Where(fixAllProviderInfo.CanBeFixed)
                                          .Select(d => d.Id)
                                          .ToImmutableHashSet();

                // When computing FixAll for unnecessary pragma suppression diagnostic,
                // we need to include suppressed diagnostics, as well as reported compiler and analyzer diagnostics.
                // A null value for 'diagnosticIdsForDiagnosticProvider' passed to 'FixAllDiagnosticProvider'
                // ensures the latter.
                ImmutableHashSet<string>? diagnosticIdsForDiagnosticProvider;
                bool includeSuppressedDiagnostics;
                if (diagnosticIds.Contains(IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId))
                {
                    diagnosticIdsForDiagnosticProvider = null;
                    includeSuppressedDiagnostics = true;
                }
                else
                {
                    diagnosticIdsForDiagnosticProvider = diagnosticIds;
                    includeSuppressedDiagnostics = false;
                }

                var diagnosticProvider = fixAllForInSpan
                    ? new FixAllPredefinedDiagnosticProvider(allDiagnostics)
                    : (FixAllContext.DiagnosticProvider)new FixAllDiagnosticProvider(this, diagnosticIdsForDiagnosticProvider, includeSuppressedDiagnostics);

                fixAllState = new FixAllState(
                    fixAllProvider: fixAllProviderInfo.FixAllProvider,
                    document: document,
                    codeFixProvider: codeFixProvider,
                    scope: FixAllScope.Document,
                    codeActionEquivalenceKey: null,
                    diagnosticIds: diagnosticIds,
                    fixAllDiagnosticProvider: diagnosticProvider);

                supportedScopes = fixAllProviderInfo.SupportedScopes;
            }

            var codeFix = new CodeFixCollection(
                fixer, fixesSpan, fixes, fixAllState,
                supportedScopes, diagnostics.First());
            result.Add(codeFix);
        }

        /// <summary> Looks explicitly for an <see cref="AbstractSuppressionCodeFixProvider"/>.</summary>
        public CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds)
        {
            if (!_configurationProvidersMap.TryGetValue(language, out var lazyConfigurationProviders) ||
                lazyConfigurationProviders.Value.IsDefault)
            {
                return null;
            }

            // Explicitly looks for an AbstractSuppressionCodeFixProvider
            var fixer = lazyConfigurationProviders.Value.OfType<AbstractSuppressionCodeFixProvider>().FirstOrDefault();
            if (fixer == null)
            {
                return null;
            }

            return new WrapperCodeFixProvider(fixer, diagnosticIds);
        }

        private async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ImmutableHashSet<string>? diagnosticIds, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(document);
            var solution = document.Project.Solution;
            var diagnostics = await _diagnosticService.GetDiagnosticsForIdsAsync(solution, null, document.Id, diagnosticIds, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfFalse(diagnostics.All(d => d.DocumentId != null));
            return await diagnostics.ToDiagnosticsAsync(document.Project, cancellationToken).ConfigureAwait(false);
        }

        private async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, bool includeAllDocumentDiagnostics, ImmutableHashSet<string>? diagnosticIds, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(project);

            if (includeAllDocumentDiagnostics)
            {
                // Get all diagnostics for the entire project, including document diagnostics.
                var diagnostics = await _diagnosticService.GetDiagnosticsForIdsAsync(project.Solution, project.Id, documentId: null, diagnosticIds, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
                return await diagnostics.ToDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Get all no-location diagnostics for the project, doesn't include document diagnostics.
                var diagnostics = await _diagnosticService.GetProjectDiagnosticsForIdsAsync(project.Solution, project.Id, diagnosticIds, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfFalse(diagnostics.All(d => d.DocumentId == null));
                return await diagnostics.ToDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<bool> ContainsAnyFixAsync(
            Document document, DiagnosticData diagnostic, CancellationToken cancellationToken)
        {
            var workspaceFixers = ImmutableArray<CodeFixProvider>.Empty;
            var hasAnySharedFixer = TryGetWorkspaceFixersMap(document, out var fixerMap) && fixerMap.Value.TryGetValue(diagnostic.Id, out workspaceFixers);
            var hasAnyProjectFixer = GetProjectFixers(document.Project).TryGetValue(diagnostic.Id, out var projectFixers);

            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            if (hasAnySharedFixer && document.Project.Solution.Workspace.Kind == WorkspaceKind.Interactive)
            {
                workspaceFixers = workspaceFixers.WhereAsArray(IsInteractiveCodeFixProvider);
                hasAnySharedFixer = workspaceFixers.Any();
            }

            var hasConfigurationFixer =
                _configurationProvidersMap.TryGetValue(document.Project.Language, out var lazyConfigurationProviders) &&
                !lazyConfigurationProviders.Value.IsDefaultOrEmpty;

            if (!hasAnySharedFixer && !hasAnyProjectFixer && !hasConfigurationFixer)
            {
                return false;
            }

            var allFixers = ImmutableArray<CodeFixProvider>.Empty;
            if (hasAnySharedFixer)
            {
                allFixers = workspaceFixers;
            }

            if (hasAnyProjectFixer)
            {
                allFixers = allFixers.AddRange(projectFixers);
            }

            var dx = await diagnostic.ToDiagnosticAsync(document.Project, cancellationToken).ConfigureAwait(false);

            if (hasConfigurationFixer)
            {
                foreach (var lazyConfigurationProvider in lazyConfigurationProviders.Value)
                {
                    if (lazyConfigurationProvider.IsFixableDiagnostic(dx))
                    {
                        return true;
                    }
                }
            }

            var fixes = new List<CodeFix>();
            var context = new CodeFixContext(document, dx,

                // TODO: Can we share code between similar lambdas that we pass to this API in BatchFixAllProvider.cs, CodeFixService.cs and CodeRefactoringService.cs?
                (action, applicableDiagnostics) =>
                {
                    // Serialize access for thread safety - we don't know what thread the fix provider will call this delegate from.
                    lock (fixes)
                    {
                        fixes.Add(new CodeFix(document.Project, action, applicableDiagnostics));
                    }
                },
                verifyArguments: false,
                cancellationToken: cancellationToken);

            var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();

            // we do have fixer. now let's see whether it actually can fix it
            foreach (var fixer in allFixers)
            {
                await extensionManager.PerformActionAsync(fixer, () => fixer.RegisterCodeFixesAsync(context) ?? Task.CompletedTask).ConfigureAwait(false);
                foreach (var fix in fixes)
                {
                    if (!fix.Action.PerformFinalApplicabilityCheck)
                    {
                        return true;
                    }

                    // Have to see if this fix is still applicable.  Jump to the foreground thread
                    // to make that check.
                    await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);

                    var applicable = fix.Action.IsApplicable(document.Project.Solution.Workspace);

                    await TaskScheduler.Default;

                    if (applicable)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsInteractiveCodeFixProvider(CodeFixProvider provider)
        {
            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            return provider is FullyQualify.AbstractFullyQualifyCodeFixProvider ||
                   provider is AddImport.AbstractAddImportCodeFixProvider;
        }

        private static readonly Func<DiagnosticId, List<CodeFixProvider>> s_createList = _ => new List<CodeFixProvider>();

        private ImmutableArray<DiagnosticId> GetFixableDiagnosticIds(CodeFixProvider fixer, IExtensionManager? extensionManager)
        {
            // If we are passed a null extension manager it means we do not have access to a document so there is nothing to
            // show the user.  In this case we will log any exceptions that occur, but the user will not see them.
            if (extensionManager != null)
            {
                return extensionManager.PerformFunction(
                    fixer,
                    () => ImmutableInterlocked.GetOrAdd(ref _fixerToFixableIdsMap, fixer, f => GetAndTestFixableDiagnosticIds(f)),
                    defaultValue: ImmutableArray<DiagnosticId>.Empty);
            }

            try
            {
                return ImmutableInterlocked.GetOrAdd(ref _fixerToFixableIdsMap, fixer, f => GetAndTestFixableDiagnosticIds(f));
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                foreach (var logger in _errorLoggers)
                {
                    logger.Value.LogException(fixer, e);
                }
                return ImmutableArray<DiagnosticId>.Empty;
            }
        }

        private static ImmutableArray<string> GetAndTestFixableDiagnosticIds(CodeFixProvider codeFixProvider)
        {
            var ids = codeFixProvider.FixableDiagnosticIds;
            if (ids.IsDefault)
            {
                throw new InvalidOperationException(
                    string.Format(
                        WorkspacesResources._0_returned_an_uninitialized_ImmutableArray,
                        codeFixProvider.GetType().Name + "." + nameof(CodeFixProvider.FixableDiagnosticIds)));
            }

            return ids;
        }

        private ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>> GetFixerPerLanguageMap(
            Dictionary<LanguageKind, List<Lazy<CodeFixProvider, CodeChangeProviderMetadata>>> fixersPerLanguage,
            Workspace workspace)
        {
            var fixerMap = ImmutableDictionary.Create<LanguageKind, Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>>();
            var extensionManager = workspace.Services.GetService<IExtensionManager>();
            foreach (var languageKindAndFixers in fixersPerLanguage)
            {
                var lazyMap = new Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>(() =>
                {
                    var mutableMap = new Dictionary<DiagnosticId, List<CodeFixProvider>>();

                    foreach (var lazyFixer in languageKindAndFixers.Value)
                    {
                        if (!TryGetWorkspaceFixer(lazyFixer, workspace, logExceptionWithInfoBar: true, out var fixer))
                        {
                            continue;
                        }

                        foreach (var id in this.GetFixableDiagnosticIds(fixer, extensionManager))
                        {
                            if (string.IsNullOrWhiteSpace(id))
                            {
                                continue;
                            }

                            var list = mutableMap.GetOrAdd(id, s_createList);
                            list.Add(fixer);
                        }
                    }

                    var immutableMap = ImmutableDictionary.CreateBuilder<DiagnosticId, ImmutableArray<CodeFixProvider>>();
                    foreach (var diagnosticIdAndFixers in mutableMap)
                    {
                        immutableMap.Add(diagnosticIdAndFixers.Key, diagnosticIdAndFixers.Value.AsImmutableOrEmpty());
                    }

                    return immutableMap.ToImmutable();
                }, isThreadSafe: true);

                fixerMap = fixerMap.Add(languageKindAndFixers.Key, lazyMap);
            }

            return fixerMap;
        }

        private static ImmutableDictionary<LanguageKind, Lazy<ImmutableArray<IConfigurationFixProvider>>> GetConfigurationProvidersPerLanguageMap(
            Dictionary<LanguageKind, List<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>>> configurationProvidersPerLanguage)
        {
            var configurationFixerMap = ImmutableDictionary.Create<LanguageKind, Lazy<ImmutableArray<IConfigurationFixProvider>>>();
            foreach (var languageKindAndFixers in configurationProvidersPerLanguage)
            {
                var lazyConfigurationFixers = new Lazy<ImmutableArray<IConfigurationFixProvider>>(() => GetConfigurationFixProviders(languageKindAndFixers.Value));
                configurationFixerMap = configurationFixerMap.Add(languageKindAndFixers.Key, lazyConfigurationFixers);
            }

            return configurationFixerMap;

            static ImmutableArray<IConfigurationFixProvider> GetConfigurationFixProviders(List<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>> languageKindAndFixers)
            {
                using var builderDisposer = ArrayBuilder<IConfigurationFixProvider>.GetInstance(out var builder);
                var orderedLanguageKindAndFixers = ExtensionOrderer.Order(languageKindAndFixers);
                foreach (var languageKindAndFixersValue in orderedLanguageKindAndFixers)
                {
                    builder.Add(languageKindAndFixersValue.Value);
                }

                return builder.ToImmutable();
            }
        }

        private ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<CodeFixProvider, int>>> GetFixerPriorityPerLanguageMap(
            Dictionary<LanguageKind, List<Lazy<CodeFixProvider, CodeChangeProviderMetadata>>> fixersPerLanguage,
            Workspace workspace)
        {
            var languageMap = ImmutableDictionary.CreateBuilder<LanguageKind, Lazy<ImmutableDictionary<CodeFixProvider, int>>>();
            foreach (var languageAndFixers in fixersPerLanguage)
            {
                var lazyMap = new Lazy<ImmutableDictionary<CodeFixProvider, int>>(() =>
                {
                    var priorityMap = ImmutableDictionary.CreateBuilder<CodeFixProvider, int>();

                    var lazyFixers = ExtensionOrderer.Order(languageAndFixers.Value);
                    for (var i = 0; i < lazyFixers.Count; i++)
                    {
                        if (!TryGetWorkspaceFixer(lazyFixers[i], workspace, logExceptionWithInfoBar: false, out var fixer))
                        {
                            continue;
                        }

                        priorityMap.Add(fixer, i);
                    }

                    return priorityMap.ToImmutable();
                }, isThreadSafe: true);

                languageMap.Add(languageAndFixers.Key, lazyMap);
            }

            return languageMap.ToImmutable();
        }

        private ImmutableDictionary<DiagnosticId, List<CodeFixProvider>> GetProjectFixers(Project project)
        {
            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            return project.Solution.Workspace.Kind == WorkspaceKind.Interactive
                ? ImmutableDictionary<DiagnosticId, List<CodeFixProvider>>.Empty
                : _projectFixersMap.GetValue(project.AnalyzerReferences, pId => ComputeProjectFixers(project));
        }

        private ImmutableDictionary<DiagnosticId, List<CodeFixProvider>> ComputeProjectFixers(Project project)
        {
            var extensionManager = project.Solution.Workspace.Services.GetService<IExtensionManager>();
            ImmutableDictionary<DiagnosticId, List<CodeFixProvider>>.Builder? builder = null;
            foreach (var reference in project.AnalyzerReferences)
            {
                var projectCodeFixerProvider = _analyzerReferenceToFixersMap.GetValue(reference, _createProjectCodeFixProvider);
                foreach (var fixer in projectCodeFixerProvider.GetExtensions(project.Language))
                {
                    var fixableIds = this.GetFixableDiagnosticIds(fixer, extensionManager);
                    foreach (var id in fixableIds)
                    {
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            continue;
                        }

                        builder ??= ImmutableDictionary.CreateBuilder<DiagnosticId, List<CodeFixProvider>>();
                        var list = builder.GetOrAdd(id, s_createList);
                        list.Add(fixer);
                    }
                }
            }

            if (builder == null)
            {
                return ImmutableDictionary<DiagnosticId, List<CodeFixProvider>>.Empty;
            }

            return builder.ToImmutable();
        }
    }
}
