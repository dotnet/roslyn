// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    using Editor.Shared.Utilities;
    using DiagnosticId = String;
    using LanguageKind = String;

    [Export(typeof(ICodeFixService)), Shared]
    internal partial class CodeFixService : ForegroundThreadAffinitizedObject, ICodeFixService
    {
        private readonly IDiagnosticAnalyzerService _diagnosticService;

        private readonly ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>> _workspaceFixersMap;
        private readonly ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, ImmutableDictionary<DiagnosticId, List<CodeFixProvider>>> _projectFixersMap;

        // Shared by project fixers and workspace fixers.
        private ImmutableDictionary<CodeFixProvider, ImmutableArray<DiagnosticId>> _fixerToFixableIdsMap = ImmutableDictionary<CodeFixProvider, ImmutableArray<DiagnosticId>>.Empty;

        private readonly ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<CodeFixProvider, int>>> _fixerPriorityMap;

        private readonly ConditionalWeakTable<AnalyzerReference, ProjectCodeFixProvider> _analyzerReferenceToFixersMap;
        private readonly ConditionalWeakTable<AnalyzerReference, ProjectCodeFixProvider>.CreateValueCallback _createProjectCodeFixProvider;

        private readonly ImmutableDictionary<LanguageKind, Lazy<ISuppressionFixProvider>> _suppressionProvidersMap;
        private readonly IEnumerable<Lazy<IErrorLoggerService>> _errorLoggers;

        private ImmutableDictionary<object, FixAllProviderInfo> _fixAllProviderMap;

        [ImportingConstructor]
        public CodeFixService(
            IDiagnosticAnalyzerService service,
            [ImportMany]IEnumerable<Lazy<IErrorLoggerService>> loggers,
            [ImportMany]IEnumerable<Lazy<CodeFixProvider, CodeChangeProviderMetadata>> fixers,
            [ImportMany]IEnumerable<Lazy<ISuppressionFixProvider, CodeChangeProviderMetadata>> suppressionProviders)
            : base(assertIsForeground: false)
        {
            _errorLoggers = loggers;
            _diagnosticService = service;
            var fixersPerLanguageMap = fixers.ToPerLanguageMapWithMultipleLanguages();
            var suppressionProvidersPerLanguageMap = suppressionProviders.ToPerLanguageMapWithMultipleLanguages();

            _workspaceFixersMap = GetFixerPerLanguageMap(fixersPerLanguageMap, null);
            _suppressionProvidersMap = GetSuppressionProvidersPerLanguageMap(suppressionProvidersPerLanguageMap);

            // REVIEW: currently, fixer's priority is statically defined by the fixer itself. might considering making it more dynamic or configurable.
            _fixerPriorityMap = GetFixerPriorityPerLanguageMap(fixersPerLanguageMap);

            // Per-project fixers
            _projectFixersMap = new ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, ImmutableDictionary<string, List<CodeFixProvider>>>();
            _analyzerReferenceToFixersMap = new ConditionalWeakTable<AnalyzerReference, ProjectCodeFixProvider>();
            _createProjectCodeFixProvider = new ConditionalWeakTable<AnalyzerReference, ProjectCodeFixProvider>.CreateValueCallback(r => new ProjectCodeFixProvider(r));
            _fixAllProviderMap = ImmutableDictionary<object, FixAllProviderInfo>.Empty;
        }

        public async Task<FirstDiagnosticResult> GetFirstDiagnosticWithFixAsync(
            Document document, TextSpan range, CancellationToken cancellationToken)
        {
            if (document == null || !document.IsOpen())
            {
                return default;
            }

            using (var diagnostics = SharedPools.Default<List<DiagnosticData>>().GetPooledObject())
            {
                var fullResult = await _diagnosticService.TryAppendDiagnosticsForSpanAsync(document, range, diagnostics.Object, cancellationToken: cancellationToken).ConfigureAwait(false);
                foreach (var diagnostic in diagnostics.Object)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!range.IntersectsWith(diagnostic.TextSpan))
                    {
                        continue;
                    }

                    // REVIEW: 2 possible designs. 
                    // 1. find the first fix and then return right away. if the lightbulb is actually expanded, find all fixes for the line synchronously. or
                    // 2. kick off a task that finds all fixes for the given range here but return once we find the first one. 
                    // at the same time, let the task to run to finish. if the lightbulb is expanded, we just simply use the task to get all fixes.
                    //
                    // first approach is simpler, so I will implement that first. if the first approach turns out to be not good enough, then
                    // I will try the second approach which will be more complex but quicker
                    var hasFix = await ContainsAnyFix(document, diagnostic, cancellationToken).ConfigureAwait(false);
                    if (hasFix)
                    {
                        return new FirstDiagnosticResult(!fullResult, hasFix, diagnostic);
                    }
                }

                return new FirstDiagnosticResult(!fullResult, false, default);
            }
        }

        public async Task<ImmutableArray<CodeFixCollection>> GetFixesAsync(Document document, TextSpan range, bool includeSuppressionFixes, CancellationToken cancellationToken)
        {
            // REVIEW: this is the first and simplest design. basically, when ctrl+. is pressed, it asks diagnostic service to give back
            // current diagnostics for the given span, and it will use that to get fixes. internally diagnostic service will either return cached information 
            // (if it is up-to-date) or synchronously do the work at the spot.
            //
            // this design's weakness is that each side don't have enough information to narrow down works to do. it will most likely always do more works than needed.
            // sometimes way more than it is needed. (compilation)
            Dictionary<TextSpan, List<DiagnosticData>> aggregatedDiagnostics = null;
            foreach (var diagnostic in await _diagnosticService.GetDiagnosticsForSpanAsync(document, range, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (diagnostic.IsSuppressed)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                aggregatedDiagnostics = aggregatedDiagnostics ?? new Dictionary<TextSpan, List<DiagnosticData>>();
                aggregatedDiagnostics.GetOrAdd(diagnostic.TextSpan, _ => new List<DiagnosticData>()).Add(diagnostic);
            }

            if (aggregatedDiagnostics == null)
            {
                return ImmutableArray<CodeFixCollection>.Empty;
            }

            var result = ArrayBuilder<CodeFixCollection>.GetInstance();
            foreach (var spanAndDiagnostic in aggregatedDiagnostics)
            {
                await AppendFixesAsync(
                    document, spanAndDiagnostic.Key, spanAndDiagnostic.Value, 
                    result, cancellationToken).ConfigureAwait(false);
            }

            if (result.Count > 0)
            {
                // sort the result to the order defined by the fixers
                var priorityMap = _fixerPriorityMap[document.Project.Language].Value;
                result.Sort((d1, d2) =>
                {
                    if (priorityMap.TryGetValue((CodeFixProvider)d1.Provider, out int priority1))
                    {
                        if (priorityMap.TryGetValue((CodeFixProvider)d2.Provider, out int priority2))
                        {
                            return priority1 - priority2;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else
                    {
                        return 1;
                    }
                });
            }

            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            if (document.Project.Solution.Workspace.Kind != WorkspaceKind.Interactive && includeSuppressionFixes)
            {
                foreach (var spanAndDiagnostic in aggregatedDiagnostics)
                {
                    await AppendSuppressionsAsync(
                        document, spanAndDiagnostic.Key, spanAndDiagnostic.Value, 
                        result, cancellationToken).ConfigureAwait(false);
                }
            }

            return result.ToImmutableAndFree();
        }

        private async Task AppendFixesAsync(
            Document document,
            TextSpan span,
            IEnumerable<DiagnosticData> diagnostics,
            ArrayBuilder<CodeFixCollection> result,
            CancellationToken cancellationToken)
        {
            bool hasAnySharedFixer = _workspaceFixersMap.TryGetValue(document.Project.Language, out var fixerMap);

            var projectFixersMap = GetProjectFixers(document.Project);
            var hasAnyProjectFixer = projectFixersMap.Any();

            if (!hasAnySharedFixer && !hasAnyProjectFixer)
            {
                return;
            }

            var allFixers = new List<CodeFixProvider>();

            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            bool isInteractive = document.Project.Solution.Workspace.Kind == WorkspaceKind.Interactive;

            foreach (var diagnosticId in diagnostics.Select(d => d.Id).Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();

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

                if (hasAnyProjectFixer && projectFixersMap.TryGetValue(diagnosticId, out var projectFixers))
                {
                    Debug.Assert(!isInteractive);
                    allFixers.AddRange(projectFixers);
                }
            }

            var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>();

            foreach (var fixer in allFixers.Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();

                await AppendFixesOrSuppressionsAsync(
                    document, span, diagnostics, result, fixer,
                    hasFix: d => this.GetFixableDiagnosticIds(fixer, extensionManager).Contains(d.Id),
                    getFixes: dxs => GetCodeFixesAsync(document, span, fixer, dxs, cancellationToken),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<ImmutableArray<CodeFix>> GetCodeFixesAsync(
            Document document, TextSpan span, CodeFixProvider fixer,
            ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var fixes = ArrayBuilder<CodeFix>.GetInstance();
            var context = new CodeFixContext(document, span, diagnostics,
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

            var task = fixer.RegisterCodeFixesAsync(context) ?? SpecializedTasks.EmptyTask;
            await task.ConfigureAwait(false);
            return fixes.ToImmutableAndFree();
        }

        private async Task AppendSuppressionsAsync(
            Document document, TextSpan span, IEnumerable<DiagnosticData> diagnostics, 
            ArrayBuilder<CodeFixCollection> result, CancellationToken cancellationToken)
        {
            if (!_suppressionProvidersMap.TryGetValue(document.Project.Language, out var lazySuppressionProvider) || lazySuppressionProvider.Value == null)
            {
                return;
            }

            await AppendFixesOrSuppressionsAsync(
                document, span, diagnostics, result, lazySuppressionProvider.Value, 
                hasFix: d => lazySuppressionProvider.Value.CanBeSuppressedOrUnsuppressed(d),
                getFixes: dxs => lazySuppressionProvider.Value.GetSuppressionsAsync(
                    document, span, dxs, cancellationToken), 
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task AppendFixesOrSuppressionsAsync(
            Document document,
            TextSpan span,
            IEnumerable<DiagnosticData> diagnosticsWithSameSpan,
            ArrayBuilder<CodeFixCollection> result,
            object fixer,
            Func<Diagnostic, bool> hasFix,
            Func<ImmutableArray<Diagnostic>, Task<ImmutableArray<CodeFix>>> getFixes,
            CancellationToken cancellationToken)
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

            var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>();
            var fixes = await extensionManager.PerformFunctionAsync(fixer,
                () => getFixes(diagnostics),
                defaultValue: ImmutableArray<CodeFix>.Empty).ConfigureAwait(false);

            if (fixes.IsDefaultOrEmpty)
            {
                return;
            }

            // If the fix provider supports fix all occurrences, then get the corresponding FixAllProviderInfo and fix all context.
            var fixAllProviderInfo = extensionManager.PerformFunction(fixer, () => ImmutableInterlocked.GetOrAdd(ref _fixAllProviderMap, fixer, FixAllProviderInfo.Create), defaultValue: null);

            FixAllState fixAllState = null;
            var supportedScopes = ImmutableArray<FixAllScope>.Empty;
            if (fixAllProviderInfo != null)
            {
                var codeFixProvider = (fixer as CodeFixProvider) ?? new WrapperCodeFixProvider((ISuppressionFixProvider)fixer, diagnostics.Select(d => d.Id));
                fixAllState = CreateFixAllState(
                    fixAllProviderInfo.FixAllProvider,
                    document, fixAllProviderInfo, codeFixProvider, diagnostics,
                    this.GetDocumentDiagnosticsAsync, this.GetProjectDiagnosticsAsync);
                supportedScopes = fixAllProviderInfo.SupportedScopes;
            }

            var codeFix = new CodeFixCollection(
                fixer, span, fixes, fixAllState,
                supportedScopes, diagnostics.First());
            result.Add(codeFix);
        }

        internal FixAllState CreateFixAllState(
            FixAllProvider fixAllProvider,
            Document document,
            FixAllProviderInfo fixAllProviderInfo,
            CodeFixProvider originalFixProvider,
            IEnumerable<Diagnostic> originalFixDiagnostics,
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync)
        {
            var diagnosticIds = originalFixDiagnostics.Where(fixAllProviderInfo.CanBeFixed)
                                                      .Select(d => d.Id)
                                                      .ToImmutableHashSet();
            var diagnosticProvider = new FixAllDiagnosticProvider(this, diagnosticIds);
            return new FixAllState(
                fixAllProvider: fixAllProvider,
                document: document,
                codeFixProvider: originalFixProvider,
                scope: FixAllScope.Document,
                codeActionEquivalenceKey: null,
                diagnosticIds: diagnosticIds,
                fixAllDiagnosticProvider: diagnosticProvider);
        }

        public CodeFixProvider GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds)
        {
            if (!_suppressionProvidersMap.TryGetValue(language, out var lazySuppressionProvider) || lazySuppressionProvider.Value == null)
            {
                return null;
            }

            return new WrapperCodeFixProvider(lazySuppressionProvider.Value, diagnosticIds);
        }

        private async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ImmutableHashSet<string> diagnosticIds, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(document);
            var solution = document.Project.Solution;
            var diagnostics = await _diagnosticService.GetDiagnosticsForIdsAsync(solution, null, document.Id, diagnosticIds, cancellationToken: cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfFalse(diagnostics.All(d => d.DocumentId != null));
            return await diagnostics.ToDiagnosticsAsync(document.Project, cancellationToken).ConfigureAwait(false);
        }

        private async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, bool includeAllDocumentDiagnostics, ImmutableHashSet<string> diagnosticIds, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(project);

            if (includeAllDocumentDiagnostics)
            {
                // Get all diagnostics for the entire project, including document diagnostics.
                var diagnostics = await _diagnosticService.GetDiagnosticsForIdsAsync(project.Solution, project.Id, diagnosticIds: diagnosticIds, cancellationToken: cancellationToken).ConfigureAwait(false);
                return await diagnostics.ToDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Get all no-location diagnostics for the project, doesn't include document diagnostics.
                var diagnostics = await _diagnosticService.GetProjectDiagnosticsForIdsAsync(project.Solution, project.Id, diagnosticIds, cancellationToken: cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfFalse(diagnostics.All(d => d.DocumentId == null));
                return await diagnostics.ToDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<bool> ContainsAnyFix(
            Document document, DiagnosticData diagnostic, CancellationToken cancellationToken)
        {
            var workspaceFixers = ImmutableArray<CodeFixProvider>.Empty;
            var hasAnySharedFixer = _workspaceFixersMap.TryGetValue(document.Project.Language, out var fixerMap) && fixerMap.Value.TryGetValue(diagnostic.Id, out workspaceFixers);
            var hasAnyProjectFixer = GetProjectFixers(document.Project).TryGetValue(diagnostic.Id, out var projectFixers);

            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            if (hasAnySharedFixer && document.Project.Solution.Workspace.Kind == WorkspaceKind.Interactive)
            {
                workspaceFixers = workspaceFixers.WhereAsArray(IsInteractiveCodeFixProvider);
                hasAnySharedFixer = workspaceFixers.Any();
            }

            Lazy<ISuppressionFixProvider> lazySuppressionProvider = null;
            var hasSuppressionFixer = 
                _suppressionProvidersMap.TryGetValue(document.Project.Language, out lazySuppressionProvider) &&
                lazySuppressionProvider.Value != null;

            if (!hasAnySharedFixer && !hasAnyProjectFixer && !hasSuppressionFixer)
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

            if (hasSuppressionFixer && lazySuppressionProvider.Value.CanBeSuppressedOrUnsuppressed(dx))
            {
                return true;
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

            var extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>();

            // we do have fixer. now let's see whether it actually can fix it
            foreach (var fixer in allFixers)
            {
                await extensionManager.PerformActionAsync(fixer, () => fixer.RegisterCodeFixesAsync(context) ?? SpecializedTasks.EmptyTask).ConfigureAwait(false);
                foreach (var fix in fixes)
                {
                    if (!fix.Action.PerformFinalApplicabilityCheck)
                    {
                        return true;
                    }

                    // Have to see if this fix is still applicable.  Jump to the foreground thread
                    // to make that check.
                    var applicable = await Task.Factory.StartNew(() =>
                        {
                            this.AssertIsForeground();
                            return fix.Action.IsApplicable(document.Project.Solution.Workspace);
                        },
                        cancellationToken, TaskCreationOptions.None, ForegroundTaskScheduler).ConfigureAwait(false);
                    this.AssertIsBackground();

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

        private ImmutableArray<DiagnosticId> GetFixableDiagnosticIds(CodeFixProvider fixer, IExtensionManager extensionManager)
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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
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
            IExtensionManager extensionManager)
        {
            var fixerMap = ImmutableDictionary.Create<LanguageKind, Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>>();
            foreach (var languageKindAndFixers in fixersPerLanguage)
            {
                var lazyMap = new Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>(() =>
                {
                    var mutableMap = new Dictionary<DiagnosticId, List<CodeFixProvider>>();

                    foreach (var fixer in languageKindAndFixers.Value)
                    {
                        foreach (var id in this.GetFixableDiagnosticIds(fixer.Value, extensionManager))
                        {
                            if (string.IsNullOrWhiteSpace(id))
                            {
                                continue;
                            }

                            var list = mutableMap.GetOrAdd(id, s_createList);
                            list.Add(fixer.Value);
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

        private static ImmutableDictionary<LanguageKind, Lazy<ISuppressionFixProvider>> GetSuppressionProvidersPerLanguageMap(
            Dictionary<LanguageKind, List<Lazy<ISuppressionFixProvider, CodeChangeProviderMetadata>>> suppressionProvidersPerLanguage)
        {
            var suppressionFixerMap = ImmutableDictionary.Create<LanguageKind, Lazy<ISuppressionFixProvider>>();
            foreach (var languageKindAndFixers in suppressionProvidersPerLanguage)
            {
                var suppressionFixerLazyMap = new Lazy<ISuppressionFixProvider>(() => languageKindAndFixers.Value.SingleOrDefault().Value);
                suppressionFixerMap = suppressionFixerMap.Add(languageKindAndFixers.Key, suppressionFixerLazyMap);
            }

            return suppressionFixerMap;
        }

        private static ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<CodeFixProvider, int>>> GetFixerPriorityPerLanguageMap(
            Dictionary<LanguageKind, List<Lazy<CodeFixProvider, CodeChangeProviderMetadata>>> fixersPerLanguage)
        {
            var languageMap = ImmutableDictionary.CreateBuilder<LanguageKind, Lazy<ImmutableDictionary<CodeFixProvider, int>>>();
            foreach (var languageAndFixers in fixersPerLanguage)
            {
                var lazyMap = new Lazy<ImmutableDictionary<CodeFixProvider, int>>(() =>
                {
                    var priorityMap = ImmutableDictionary.CreateBuilder<CodeFixProvider, int>();

                    var fixers = ExtensionOrderer.Order(languageAndFixers.Value);
                    for (var i = 0; i < fixers.Count; i++)
                    {
                        priorityMap.Add(fixers[i].Value, i);
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
            ImmutableDictionary<DiagnosticId, List<CodeFixProvider>>.Builder builder = null;
            foreach (var reference in project.AnalyzerReferences)
            {
                var projectCodeFixerProvider = _analyzerReferenceToFixersMap.GetValue(reference, _createProjectCodeFixProvider);
                foreach (var fixer in projectCodeFixerProvider.GetFixers(project.Language))
                {
                    var fixableIds = this.GetFixableDiagnosticIds(fixer, extensionManager);
                    foreach (var id in fixableIds)
                    {
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            continue;
                        }

                        builder = builder ?? ImmutableDictionary.CreateBuilder<DiagnosticId, List<CodeFixProvider>>();
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
