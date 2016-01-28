// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        private readonly int _correlationId;
        private readonly MemberRangeMap _memberRangeMap;
        private readonly AnalyzerExecutor _executor;
        private readonly StateManager _stateManager;

        /// <summary>
        /// PERF: Always run analyzers sequentially for background analysis.
        /// </summary>
        private const bool ConcurrentAnalysis = false;

        /// <summary>
        /// Always compute suppressed diagnostics - diagnostic clients may or may not request for suppressed diagnostics.
        /// </summary>
        private const bool ReportSuppressedDiagnostics = true;

        public DiagnosticIncrementalAnalyzer(
            DiagnosticAnalyzerService owner,
            int correlationId,
            Workspace workspace,
            HostAnalyzerManager analyzerManager,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
            : base(owner, workspace, analyzerManager, hostDiagnosticUpdateSource)
        {
            _correlationId = correlationId;
            _memberRangeMap = new MemberRangeMap();
            _executor = new AnalyzerExecutor(this);

            _stateManager = new StateManager(analyzerManager);
            _stateManager.ProjectAnalyzerReferenceChanged += OnProjectAnalyzerReferenceChanged;
        }

        private void OnProjectAnalyzerReferenceChanged(object sender, ProjectAnalyzerReferenceChangedEventArgs e)
        {
            if (e.Removed.Length == 0)
            {
                // nothing to refresh
                return;
            }

            // events will be automatically serialized.
            ClearProjectStatesAsync(e.Project, e.Removed, CancellationToken.None);
        }

        public override Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentOpen, GetOpenLogMessage, document, cancellationToken))
            {
                return ClearOnlyDocumentStates(document, raiseEvent: true, cancellationToken: cancellationToken);
            }
        }

        public override Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentClose, GetResetLogMessage, document, cancellationToken))
            {
                // we don't need the info for closed file
                _memberRangeMap.Remove(document.Id);

                return ClearOnlyDocumentStates(document, raiseEvent: true, cancellationToken: cancellationToken);
            }
        }

        public override Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentReset, GetResetLogMessage, document, cancellationToken))
            {
                // clear states for re-analysis and raise events about it. otherwise, some states might not updated on re-analysis
                // due to our build-live de-duplication logic where we put all state in Documents state.
                return ClearOnlyDocumentStates(document, raiseEvent: true, cancellationToken: cancellationToken);
            }
        }

        private Task ClearOnlyDocumentStates(Document document, bool raiseEvent, CancellationToken cancellationToken)
        {
            // we remove whatever information we used to have on document open/close and re-calculate diagnostics
            // we had to do this since some diagnostic analyzer changes its behavior based on whether the document is opened or not.
            // so we can't use cached information.
            ClearDocumentStates(document, _stateManager.GetStateSets(document.Project), raiseEvent, includeProjectState: false, cancellationToken: cancellationToken);

            return SpecializedTasks.EmptyTask;
        }

        private bool CheckOption(Workspace workspace, string language, bool forceAnalysis)
        {
            if (workspace.Options.GetOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, language) &&
                workspace.Options.GetOption(RuntimeOptions.FullSolutionAnalysis))
            {
                return true;
            }

            if (forceAnalysis)
            {
                return true;
            }

            return false;
        }

        internal CompilationWithAnalyzers GetCompilationWithAnalyzers(Project project, Compilation compilation, bool concurrentAnalysis, bool reportSuppressedDiagnostics)
        {
            Contract.ThrowIfFalse(project.SupportsCompilation);
            Contract.ThrowIfNull(compilation);

            Func<Exception, bool> analyzerExceptionFilter = ex =>
            {
                if (project.Solution.Workspace.Options.GetOption(InternalDiagnosticsOptions.CrashOnAnalyzerException))
                {
                    // if option is on, crash the host to get crash dump.
                    FatalError.ReportUnlessCanceled(ex);
                }

                return true;
            };

            var analysisOptions = new CompilationWithAnalyzersOptions(
                new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution.Workspace),
                GetOnAnalyzerException(project.Id),
                analyzerExceptionFilter,
                concurrentAnalysis,
                logAnalyzerExecutionTime: true,
                reportSuppressedDiagnostics: reportSuppressedDiagnostics);

            var analyzers = _stateManager.GetAnalyzers(project);
            var filteredAnalyzers = analyzers
                .Where(a => !CompilationWithAnalyzers.IsDiagnosticAnalyzerSuppressed(a, compilation.Options, analysisOptions.OnAnalyzerException))
                .Distinct()
                .ToImmutableArray();
            if (filteredAnalyzers.IsEmpty)
            {
                return null;
            }

            return new CompilationWithAnalyzers(compilation, filteredAnalyzers, analysisOptions);
        }

        public override async Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            await AnalyzeSyntaxAsync(document, diagnosticIds: null, skipClosedFileChecks: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task AnalyzeSyntaxAsync(Document document, ImmutableHashSet<string> diagnosticIds, bool skipClosedFileChecks, CancellationToken cancellationToken)
        {
            try
            {
                if (!skipClosedFileChecks && !CheckOption(document.Project.Solution.Workspace, document.Project.Language, document.IsOpen()))
                {
                    return;
                }

                var textVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                var dataVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
                var versions = new VersionArgument(textVersion, dataVersion);

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var fullSpan = root == null ? null : (TextSpan?)root.FullSpan;

                var userDiagnosticDriver = new DiagnosticAnalyzerDriver(document, fullSpan, root, this, ConcurrentAnalysis, ReportSuppressedDiagnostics, cancellationToken);
                var openedDocument = document.IsOpen();

                foreach (var stateSet in _stateManager.GetOrUpdateStateSets(document.Project))
                {
                    if (SkipRunningAnalyzer(document.Project.CompilationOptions, userDiagnosticDriver, openedDocument, skipClosedFileChecks, stateSet))
                    {
                        await ClearExistingDiagnostics(document, stateSet, StateType.Syntax, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (ShouldRunAnalyzerForStateType(stateSet.Analyzer, StateType.Syntax, diagnosticIds))
                    {
                        var data = await _executor.GetSyntaxAnalysisDataAsync(userDiagnosticDriver, stateSet, versions).ConfigureAwait(false);
                        if (data.FromCache)
                        {
                            RaiseDiagnosticsCreatedFromCacheIfNeeded(StateType.Syntax, document, stateSet, data.Items);
                            continue;
                        }

                        var state = stateSet.GetState(StateType.Syntax);
                        await state.PersistAsync(document, data.ToPersistData(), cancellationToken).ConfigureAwait(false);

                        RaiseDocumentDiagnosticsUpdatedIfNeeded(StateType.Syntax, document, stateSet, data.OldItems, data.Items);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public override async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
        {
            await AnalyzeDocumentAsync(document, bodyOpt, diagnosticIds: null, skipClosedFileChecks: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, ImmutableHashSet<string> diagnosticIds, bool skipClosedFileChecks, CancellationToken cancellationToken)
        {
            try
            {
                if (!skipClosedFileChecks && !CheckOption(document.Project.Solution.Workspace, document.Project.Language, document.IsOpen()))
                {
                    return;
                }

                var textVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                var projectVersion = await document.Project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
                var dataVersion = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                var versions = new VersionArgument(textVersion, dataVersion, projectVersion);
                if (bodyOpt == null)
                {
                    await AnalyzeDocumentAsync(document, versions, diagnosticIds, skipClosedFileChecks, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // only open file can go this route
                    await AnalyzeBodyDocumentAsync(document, bodyOpt, versions, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task AnalyzeBodyDocumentAsync(Document document, SyntaxNode member, VersionArgument versions, CancellationToken cancellationToken)
        {
            try
            {
                // syntax facts service must exist, otherwise, this method won't have called.
                var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var memberId = syntaxFacts.GetMethodLevelMemberId(root, member);

                var spanBasedDriver = new DiagnosticAnalyzerDriver(document, member.FullSpan, root, this, ConcurrentAnalysis, ReportSuppressedDiagnostics, cancellationToken);
                var documentBasedDriver = new DiagnosticAnalyzerDriver(document, root.FullSpan, root, this, ConcurrentAnalysis, ReportSuppressedDiagnostics, cancellationToken);

                foreach (var stateSet in _stateManager.GetOrUpdateStateSets(document.Project))
                {
                    if (Owner.IsAnalyzerSuppressed(stateSet.Analyzer, document.Project))
                    {
                        await ClearExistingDiagnostics(document, stateSet, StateType.Document, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (ShouldRunAnalyzerForStateType(stateSet.Analyzer, StateType.Document))
                    {
                        var supportsSemanticInSpan = stateSet.Analyzer.SupportsSpanBasedSemanticDiagnosticAnalysis();
                        var userDiagnosticDriver = supportsSemanticInSpan ? spanBasedDriver : documentBasedDriver;

                        var ranges = _memberRangeMap.GetSavedMemberRange(stateSet.Analyzer, document);
                        var data = await _executor.GetDocumentBodyAnalysisDataAsync(
                            stateSet, versions, userDiagnosticDriver, root, member, memberId, supportsSemanticInSpan, ranges).ConfigureAwait(false);

                        _memberRangeMap.UpdateMemberRange(stateSet.Analyzer, document, versions.TextVersion, memberId, member.FullSpan, ranges);

                        var state = stateSet.GetState(StateType.Document);
                        await state.PersistAsync(document, data.ToPersistData(), cancellationToken).ConfigureAwait(false);

                        if (data.FromCache)
                        {
                            RaiseDiagnosticsCreatedFromCacheIfNeeded(StateType.Document, document, stateSet, data.Items);
                            continue;
                        }

                        RaiseDocumentDiagnosticsUpdatedIfNeeded(StateType.Document, document, stateSet, data.OldItems, data.Items);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task AnalyzeDocumentAsync(Document document, VersionArgument versions, ImmutableHashSet<string> diagnosticIds, bool skipClosedFileChecks, CancellationToken cancellationToken)
        {
            try
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var fullSpan = root == null ? null : (TextSpan?)root.FullSpan;

                var userDiagnosticDriver = new DiagnosticAnalyzerDriver(document, fullSpan, root, this, ConcurrentAnalysis, ReportSuppressedDiagnostics, cancellationToken);
                bool openedDocument = document.IsOpen();

                foreach (var stateSet in _stateManager.GetOrUpdateStateSets(document.Project))
                {
                    if (SkipRunningAnalyzer(document.Project.CompilationOptions, userDiagnosticDriver, openedDocument, skipClosedFileChecks, stateSet))
                    {
                        await ClearExistingDiagnostics(document, stateSet, StateType.Document, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (ShouldRunAnalyzerForStateType(stateSet.Analyzer, StateType.Document, diagnosticIds))
                    {
                        var data = await _executor.GetDocumentAnalysisDataAsync(userDiagnosticDriver, stateSet, versions).ConfigureAwait(false);
                        if (data.FromCache)
                        {
                            RaiseDiagnosticsCreatedFromCacheIfNeeded(StateType.Document, document, stateSet, data.Items);
                            continue;
                        }

                        if (openedDocument)
                        {
                            _memberRangeMap.Touch(stateSet.Analyzer, document, versions.TextVersion);
                        }

                        var state = stateSet.GetState(StateType.Document);
                        await state.PersistAsync(document, data.ToPersistData(), cancellationToken).ConfigureAwait(false);

                        RaiseDocumentDiagnosticsUpdatedIfNeeded(StateType.Document, document, stateSet, data.OldItems, data.Items);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
        {
            await AnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
        }

        private async Task AnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
        {
            try
            {
                // Compilation actions can report diagnostics on open files, so "documentOpened = true"
                if (!CheckOption(project.Solution.Workspace, project.Language, forceAnalysis: false))
                {
                    return;
                }

                var projectTextVersion = await project.GetLatestDocumentVersionAsync(cancellationToken).ConfigureAwait(false);
                var semanticVersion = await project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
                var projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
                var analyzerDriver = new DiagnosticAnalyzerDriver(project, this, ConcurrentAnalysis, ReportSuppressedDiagnostics, cancellationToken);

                var versions = new VersionArgument(projectTextVersion, semanticVersion, projectVersion);
                foreach (var stateSet in _stateManager.GetOrUpdateStateSets(project))
                {
                    // Compilation actions can report diagnostics on open files, so we skipClosedFileChecks.
                    if (SkipRunningAnalyzer(project.CompilationOptions, analyzerDriver, openedDocument: true, skipClosedFileChecks: true, stateSet: stateSet))
                    {
                        await ClearExistingDiagnostics(project, stateSet, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (ShouldRunAnalyzerForStateType(stateSet.Analyzer, StateType.Project, diagnosticIds: null))
                    {
                        var data = await _executor.GetProjectAnalysisDataAsync(analyzerDriver, stateSet, versions).ConfigureAwait(false);
                        if (data.FromCache)
                        {
                            RaiseProjectDiagnosticsUpdatedIfNeeded(project, stateSet, ImmutableArray<DiagnosticData>.Empty, data.Items);
                            continue;
                        }

                        var state = stateSet.GetState(StateType.Project);
                        await PersistProjectData(project, state, data).ConfigureAwait(false);

                        RaiseProjectDiagnosticsUpdatedIfNeeded(project, stateSet, data.OldItems, data.Items);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private bool SkipRunningAnalyzer(
            CompilationOptions compilationOptions,
            DiagnosticAnalyzerDriver userDiagnosticDriver,
            bool openedDocument,
            bool skipClosedFileChecks,
            StateSet stateSet)
        {
            if (Owner.IsAnalyzerSuppressed(stateSet.Analyzer, userDiagnosticDriver.Project))
            {
                return true;
            }

            if (skipClosedFileChecks)
            {
                return false;
            }

            if (ShouldRunAnalyzerForClosedFile(compilationOptions, openedDocument, stateSet.Analyzer))
            {
                return false;
            }

            return true;
        }

        private static async Task PersistProjectData(Project project, DiagnosticState state, AnalysisData data)
        {
            // TODO: Cancellation is not allowed here to prevent data inconsistency. But there is still a possibility of data inconsistency due to
            //       things like exception. For now, I am letting it go and let v2 engine take care of it properly. If v2 doesn't come online soon enough
            //       more refactoring is required on project state.

            // clear all existing data
            state.Remove(project.Id);
            foreach (var document in project.Documents)
            {
                state.Remove(document.Id);
            }

            // quick bail out
            if (data.Items.Length == 0)
            {
                return;
            }

            // save new data
            var group = data.Items.GroupBy(d => d.DocumentId);
            foreach (var kv in group)
            {
                if (kv.Key == null)
                {
                    // save project scope diagnostics
                    await state.PersistAsync(project, new AnalysisData(data.TextVersion, data.DataVersion, kv.ToImmutableArrayOrEmpty()), CancellationToken.None).ConfigureAwait(false);
                    continue;
                }

                // save document scope diagnostics
                var document = project.GetDocument(kv.Key);
                if (document == null)
                {
                    continue;
                }

                await state.PersistAsync(document, new AnalysisData(data.TextVersion, data.DataVersion, kv.ToImmutableArrayOrEmpty()), CancellationToken.None).ConfigureAwait(false);
            }
        }

        public override void RemoveDocument(DocumentId documentId)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_RemoveDocument, GetRemoveLogMessage, documentId, CancellationToken.None))
            {
                _memberRangeMap.Remove(documentId);

                foreach (var stateSet in _stateManager.GetStateSets(documentId.ProjectId))
                {
                    stateSet.Remove(documentId);

                    var solutionArgs = new SolutionArgument(null, documentId.ProjectId, documentId);
                    for (var stateType = 0; stateType < s_stateTypeCount; stateType++)
                    {
                        RaiseDiagnosticsRemoved((StateType)stateType, documentId, stateSet, solutionArgs);
                    }
                }
            }
        }

        public override void RemoveProject(ProjectId projectId)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_RemoveProject, GetRemoveLogMessage, projectId, CancellationToken.None))
            {
                foreach (var stateSet in _stateManager.GetStateSets(projectId))
                {
                    stateSet.Remove(projectId);

                    var solutionArgs = new SolutionArgument(null, projectId, null);
                    RaiseDiagnosticsRemoved(StateType.Project, projectId, stateSet, solutionArgs);
                }
            }

            _stateManager.RemoveStateSet(projectId);
        }

        public override async Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> diagnostics, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var getter = new LatestDiagnosticsForSpanGetter(this, document, root, range, blockForData: false, diagnostics: diagnostics, includeSuppressedDiagnostics: includeSuppressedDiagnostics, cancellationToken: cancellationToken);
            return await getter.TryGetAsync().ConfigureAwait(false);
        }

        public override async Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var getter = new LatestDiagnosticsForSpanGetter(this, document, root, range, blockForData: true, includeSuppressedDiagnostics: includeSuppressedDiagnostics, cancellationToken: cancellationToken);

            var result = await getter.TryGetAsync().ConfigureAwait(false);
            Contract.Requires(result);

            return getter.Diagnostics;
        }

        private bool ShouldRunAnalyzerForClosedFile(CompilationOptions options, bool openedDocument, DiagnosticAnalyzer analyzer)
        {
            // we have opened document, doesn't matter
            if (openedDocument || analyzer.IsCompilerAnalyzer())
            {
                return true;
            }

            // PERF: Don't query descriptors for compiler analyzer, always execute it.
            if (analyzer.IsCompilerAnalyzer())
            {
                return true;
            }

            return Owner.GetDiagnosticDescriptors(analyzer).Any(d => GetEffectiveSeverity(d, options) != ReportDiagnostic.Hidden);
        }

        private static ReportDiagnostic GetEffectiveSeverity(DiagnosticDescriptor descriptor, CompilationOptions options)
        {
            return options == null
                ? MapSeverityToReport(descriptor.DefaultSeverity)
                : descriptor.GetEffectiveSeverity(options);
        }

        private static ReportDiagnostic MapSeverityToReport(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Hidden:
                    return ReportDiagnostic.Hidden;
                case DiagnosticSeverity.Info:
                    return ReportDiagnostic.Info;
                case DiagnosticSeverity.Warning:
                    return ReportDiagnostic.Warn;
                case DiagnosticSeverity.Error:
                    return ReportDiagnostic.Error;
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private bool ShouldRunAnalyzerForStateType(DiagnosticAnalyzer analyzer, StateType stateTypeId, ImmutableHashSet<string> diagnosticIds)
        {
            return ShouldRunAnalyzerForStateType(analyzer, stateTypeId, diagnosticIds, Owner.GetDiagnosticDescriptors);
        }

        private static bool ShouldRunAnalyzerForStateType(DiagnosticAnalyzer analyzer, StateType stateTypeId,
            ImmutableHashSet<string> diagnosticIds = null, Func<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>> getDescriptors = null)
        {
            // PERF: Don't query descriptors for compiler analyzer, always execute it for all state types.
            if (analyzer.IsCompilerAnalyzer())
            {
                return true;
            }

            if (diagnosticIds != null && getDescriptors(analyzer).All(d => !diagnosticIds.Contains(d.Id)))
            {
                return false;
            }

            switch (stateTypeId)
            {
                case StateType.Syntax:
                    return analyzer.SupportsSyntaxDiagnosticAnalysis();

                case StateType.Document:
                    return analyzer.SupportsSemanticDiagnosticAnalysis();

                case StateType.Project:
                    return analyzer.SupportsProjectDiagnosticAnalysis();

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public override void LogAnalyzerCountSummary()
        {
            DiagnosticAnalyzerLogger.LogAnalyzerCrashCountSummary(_correlationId, DiagnosticLogAggregator);
            DiagnosticAnalyzerLogger.LogAnalyzerTypeCountSummary(_correlationId, DiagnosticLogAggregator);

            // reset the log aggregator
            ResetDiagnosticLogAggregator();
        }

        private static bool CheckSyntaxVersions(Document document, AnalysisData existingData, VersionArgument versions)
        {
            if (existingData == null)
            {
                return false;
            }

            return document.CanReusePersistedTextVersion(versions.TextVersion, existingData.TextVersion) &&
                   document.CanReusePersistedSyntaxTreeVersion(versions.DataVersion, existingData.DataVersion);
        }

        private static bool CheckSemanticVersions(Document document, AnalysisData existingData, VersionArgument versions)
        {
            if (existingData == null)
            {
                return false;
            }

            return document.CanReusePersistedTextVersion(versions.TextVersion, existingData.TextVersion) &&
                   document.Project.CanReusePersistedDependentSemanticVersion(versions.ProjectVersion, versions.DataVersion, existingData.DataVersion);
        }

        private static bool CheckSemanticVersions(Project project, AnalysisData existingData, VersionArgument versions)
        {
            if (existingData == null)
            {
                return false;
            }

            return VersionStamp.CanReusePersistedVersion(versions.TextVersion, existingData.TextVersion) &&
                   project.CanReusePersistedDependentSemanticVersion(versions.ProjectVersion, versions.DataVersion, existingData.DataVersion);
        }

        private void RaiseDiagnosticsCreatedFromCacheIfNeeded(StateType type, Document document, StateSet stateSet, ImmutableArray<DiagnosticData> items)
        {
            RaiseDocumentDiagnosticsUpdatedIfNeeded(type, document, stateSet, ImmutableArray<DiagnosticData>.Empty, items);
        }

        private void RaiseDocumentDiagnosticsUpdatedIfNeeded(
            StateType type, Document document, StateSet stateSet, ImmutableArray<DiagnosticData> existingItems, ImmutableArray<DiagnosticData> newItems)
        {
            var noItems = existingItems.Length == 0 && newItems.Length == 0;
            if (noItems)
            {
                return;
            }

            RaiseDiagnosticsCreated(type, document.Id, stateSet, new SolutionArgument(document), newItems);
        }

        private void RaiseProjectDiagnosticsUpdatedIfNeeded(
            Project project, StateSet stateSet, ImmutableArray<DiagnosticData> existingItems, ImmutableArray<DiagnosticData> newItems)
        {
            var noItems = existingItems.Length == 0 && newItems.Length == 0;
            if (noItems)
            {
                return;
            }

            RaiseProjectDiagnosticsRemovedIfNeeded(project, stateSet, existingItems, newItems);
            RaiseProjectDiagnosticsUpdated(project, stateSet, newItems);
        }

        private void RaiseProjectDiagnosticsRemovedIfNeeded(
            Project project, StateSet stateSet, ImmutableArray<DiagnosticData> existingItems, ImmutableArray<DiagnosticData> newItems)
        {
            if (existingItems.Length == 0)
            {
                return;
            }

            var removedItems = existingItems.GroupBy(d => d.DocumentId).Select(g => g.Key).Except(newItems.GroupBy(d => d.DocumentId).Select(g => g.Key));
            foreach (var documentId in removedItems)
            {
                if (documentId == null)
                {
                    RaiseDiagnosticsRemoved(StateType.Project, project.Id, stateSet, new SolutionArgument(project));
                    continue;
                }

                var document = project.GetDocument(documentId);
                var argument = documentId == null ? new SolutionArgument(null, documentId.ProjectId, documentId) : new SolutionArgument(document);
                RaiseDiagnosticsRemoved(StateType.Project, documentId, stateSet, argument);
            }
        }

        private void RaiseProjectDiagnosticsUpdated(Project project, StateSet stateSet, ImmutableArray<DiagnosticData> diagnostics)
        {
            var group = diagnostics.GroupBy(d => d.DocumentId);
            foreach (var kv in group)
            {
                if (kv.Key == null)
                {
                    RaiseDiagnosticsCreated(StateType.Project, project.Id, stateSet, new SolutionArgument(project), kv.ToImmutableArrayOrEmpty());
                    continue;
                }

                RaiseDiagnosticsCreated(StateType.Project, kv.Key, stateSet, new SolutionArgument(project.GetDocument(kv.Key)), kv.ToImmutableArrayOrEmpty());
            }
        }

        private static ImmutableArray<DiagnosticData> GetDiagnosticData(ILookup<DocumentId, DiagnosticData> lookup, DocumentId documentId)
        {
            return lookup.Contains(documentId) ? lookup[documentId].ToImmutableArrayOrEmpty() : ImmutableArray<DiagnosticData>.Empty;
        }

        private void RaiseDiagnosticsCreated(
            StateType type, object key, StateSet stateSet, SolutionArgument solution, ImmutableArray<DiagnosticData> diagnostics)
        {
            if (Owner == null)
            {
                return;
            }

            // get right arg id for the given analyzer
            var id = CreateArgumentKey(type, key, stateSet);

            Owner.RaiseDiagnosticsUpdated(this,
                DiagnosticsUpdatedArgs.DiagnosticsCreated(id, Workspace, solution.Solution, solution.ProjectId, solution.DocumentId, diagnostics));
        }

        private static ArgumentKey CreateArgumentKey(StateType type, object key, StateSet stateSet)
        {
            return stateSet.ErrorSourceName != null
                ? new HostAnalyzerKey(stateSet.Analyzer, type, key, stateSet.ErrorSourceName)
                : new ArgumentKey(stateSet.Analyzer, type, key);
        }

        private void RaiseDiagnosticsRemoved(
            StateType type, object key, StateSet stateSet, SolutionArgument solution)
        {
            if (Owner == null)
            {
                return;
            }

            // get right arg id for the given analyzer
            var id = CreateArgumentKey(type, key, stateSet);

            Owner.RaiseDiagnosticsUpdated(this,
                DiagnosticsUpdatedArgs.DiagnosticsRemoved(id, Workspace, solution.Solution, solution.ProjectId, solution.DocumentId));
        }

        private ImmutableArray<DiagnosticData> UpdateDocumentDiagnostics(
            AnalysisData existingData, ImmutableArray<TextSpan> range, ImmutableArray<DiagnosticData> memberDiagnostics,
            SyntaxTree tree, SyntaxNode member, int memberId)
        {
            // get old span
            var oldSpan = range[memberId];

            // get old diagnostics
            var diagnostics = existingData.Items;

            // check quick exit cases
            if (diagnostics.Length == 0 && memberDiagnostics.Length == 0)
            {
                return diagnostics;
            }

            // simple case
            if (diagnostics.Length == 0 && memberDiagnostics.Length > 0)
            {
                return memberDiagnostics;
            }

            // regular case
            var result = new List<DiagnosticData>();

            // update member location
            Contract.Requires(member.FullSpan.Start == oldSpan.Start);
            var delta = member.FullSpan.End - oldSpan.End;

            var replaced = false;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.TextSpan.Start < oldSpan.Start)
                {
                    result.Add(diagnostic);
                    continue;
                }

                if (!replaced)
                {
                    result.AddRange(memberDiagnostics);
                    replaced = true;
                }

                if (oldSpan.End <= diagnostic.TextSpan.Start)
                {
                    result.Add(UpdatePosition(diagnostic, tree, delta));
                    continue;
                }
            }

            // if it haven't replaced, replace it now
            if (!replaced)
            {
                result.AddRange(memberDiagnostics);
                replaced = true;
            }

            return result.ToImmutableArray();
        }

        private DiagnosticData UpdatePosition(DiagnosticData diagnostic, SyntaxTree tree, int delta)
        {
            var start = Math.Min(Math.Max(diagnostic.TextSpan.Start + delta, 0), tree.Length);
            var newSpan = new TextSpan(start, start >= tree.Length ? 0 : diagnostic.TextSpan.Length);

            var mappedLineInfo = tree.GetMappedLineSpan(newSpan);
            var originalLineInfo = tree.GetLineSpan(newSpan);

            return new DiagnosticData(
                diagnostic.Id,
                diagnostic.Category,
                diagnostic.Message,
                diagnostic.ENUMessageForBingSearch,
                diagnostic.Severity,
                diagnostic.DefaultSeverity,
                diagnostic.IsEnabledByDefault,
                diagnostic.WarningLevel,
                diagnostic.CustomTags,
                diagnostic.Properties,
                diagnostic.Workspace,
                diagnostic.ProjectId,
                new DiagnosticDataLocation(diagnostic.DocumentId, newSpan,
                    originalFilePath: originalLineInfo.Path,
                    originalStartLine: originalLineInfo.StartLinePosition.Line,
                    originalStartColumn: originalLineInfo.StartLinePosition.Character,
                    originalEndLine: originalLineInfo.EndLinePosition.Line,
                    originalEndColumn: originalLineInfo.EndLinePosition.Character,
                    mappedFilePath: mappedLineInfo.GetMappedFilePathIfExist(),
                    mappedStartLine: mappedLineInfo.StartLinePosition.Line,
                    mappedStartColumn: mappedLineInfo.StartLinePosition.Character,
                    mappedEndLine: mappedLineInfo.EndLinePosition.Line,
                    mappedEndColumn: mappedLineInfo.EndLinePosition.Character),
                description: diagnostic.Description,
                helpLink: diagnostic.HelpLink,
                isSuppressed: diagnostic.IsSuppressed);
        }

        private static IEnumerable<DiagnosticData> GetDiagnosticData(Document document, SyntaxTree tree, TextSpan? span, IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics != null ? diagnostics.Where(dx => ShouldIncludeDiagnostic(dx, tree, span)).Select(d => DiagnosticData.Create(document, d)) : null;
        }

        private static bool ShouldIncludeDiagnostic(Diagnostic diagnostic, SyntaxTree tree, TextSpan? span)
        {
            if (diagnostic == null)
            {
                return false;
            }

            if (diagnostic.Location == null || diagnostic.Location == Location.None)
            {
                return false;
            }

            if (diagnostic.Location.SourceTree != tree)
            {
                return false;
            }

            if (span == null)
            {
                return true;
            }

            return span.Value.Contains(diagnostic.Location.SourceSpan);
        }

        private static IEnumerable<DiagnosticData> GetDiagnosticData(Project project, IEnumerable<Diagnostic> diagnostics)
        {
            if (diagnostics == null)
            {
                yield break;
            }

            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Location == null || diagnostic.Location == Location.None)
                {
                    yield return DiagnosticData.Create(project, diagnostic);
                    continue;
                }

                var document = project.GetDocument(diagnostic.Location.SourceTree);
                if (document == null)
                {
                    continue;
                }

                yield return DiagnosticData.Create(document, diagnostic);
            }
        }

        private static async Task<IEnumerable<DiagnosticData>> GetSyntaxDiagnosticsAsync(DiagnosticAnalyzerDriver userDiagnosticDriver, DiagnosticAnalyzer analyzer)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_SyntaxDiagnostic, GetSyntaxLogMessage, userDiagnosticDriver.Document, userDiagnosticDriver.Span, analyzer, userDiagnosticDriver.CancellationToken))
            {
                try
                {
                    Contract.ThrowIfNull(analyzer);

                    var tree = await userDiagnosticDriver.Document.GetSyntaxTreeAsync(userDiagnosticDriver.CancellationToken).ConfigureAwait(false);
                    var diagnostics = await userDiagnosticDriver.GetSyntaxDiagnosticsAsync(analyzer).ConfigureAwait(false);
                    return GetDiagnosticData(userDiagnosticDriver.Document, tree, userDiagnosticDriver.Span, diagnostics);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        private static async Task<IEnumerable<DiagnosticData>> GetSemanticDiagnosticsAsync(DiagnosticAnalyzerDriver userDiagnosticDriver, DiagnosticAnalyzer analyzer)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_SemanticDiagnostic, GetSemanticLogMessage, userDiagnosticDriver.Document, userDiagnosticDriver.Span, analyzer, userDiagnosticDriver.CancellationToken))
            {
                try
                {
                    Contract.ThrowIfNull(analyzer);

                    var tree = await userDiagnosticDriver.Document.GetSyntaxTreeAsync(userDiagnosticDriver.CancellationToken).ConfigureAwait(false);
                    var diagnostics = await userDiagnosticDriver.GetSemanticDiagnosticsAsync(analyzer).ConfigureAwait(false);
                    return GetDiagnosticData(userDiagnosticDriver.Document, tree, userDiagnosticDriver.Span, diagnostics);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        private static async Task<IEnumerable<DiagnosticData>> GetProjectDiagnosticsAsync(DiagnosticAnalyzerDriver userDiagnosticDriver, DiagnosticAnalyzer analyzer)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_ProjectDiagnostic, GetProjectLogMessage, userDiagnosticDriver.Project, analyzer, userDiagnosticDriver.CancellationToken))
            {
                try
                {
                    Contract.ThrowIfNull(analyzer);

                    var diagnostics = await userDiagnosticDriver.GetProjectDiagnosticsAsync(analyzer).ConfigureAwait(false);
                    return GetDiagnosticData(userDiagnosticDriver.Project, diagnostics);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        private void ClearDocumentStates(
            Document document, IEnumerable<StateSet> states,
            bool raiseEvent, bool includeProjectState,
            CancellationToken cancellationToken)
        {
            // Compiler + User diagnostics
            foreach (var state in states)
            {
                for (var stateType = 0; stateType < s_stateTypeCount; stateType++)
                {
                    if (!includeProjectState && stateType == (int)StateType.Project)
                    {
                        // don't re-set project state type
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    ClearDocumentState(document, state, (StateType)stateType, raiseEvent);
                }
            }
        }

        private void ClearDocumentState(Document document, StateSet stateSet, StateType type, bool raiseEvent)
        {
            var state = stateSet.GetState(type);

            // remove saved info
            state.Remove(document.Id);

            if (raiseEvent)
            {
                // raise diagnostic updated event
                var documentId = document.Id;
                var solutionArgs = new SolutionArgument(document);

                RaiseDiagnosticsRemoved(type, document.Id, stateSet, solutionArgs);
            }
        }

        private void ClearProjectStatesAsync(Project project, IEnumerable<StateSet> states, CancellationToken cancellationToken)
        {
            foreach (var document in project.Documents)
            {
                ClearDocumentStates(document, states, raiseEvent: true, includeProjectState: true, cancellationToken: cancellationToken);
            }

            foreach (var stateSet in states)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ClearProjectState(project, stateSet);
            }
        }

        private void ClearProjectState(Project project, StateSet stateSet)
        {
            var state = stateSet.GetState(StateType.Project);

            // remove saved cache
            state.Remove(project.Id);

            // raise diagnostic updated event
            var solutionArgs = new SolutionArgument(project);
            RaiseDiagnosticsRemoved(StateType.Project, project.Id, stateSet, solutionArgs);
        }

        private async Task ClearExistingDiagnostics(Document document, StateSet stateSet, StateType type, CancellationToken cancellationToken)
        {
            var state = stateSet.GetState(type);
            var existingData = await state.TryGetExistingDataAsync(document, cancellationToken).ConfigureAwait(false);
            if (existingData?.Items.Length > 0)
            {
                ClearDocumentState(document, stateSet, type, raiseEvent: true);
            }
        }

        private async Task ClearExistingDiagnostics(Project project, StateSet stateSet, CancellationToken cancellationToken)
        {
            var state = stateSet.GetState(StateType.Project);
            var existingData = await state.TryGetExistingDataAsync(project, cancellationToken).ConfigureAwait(false);
            if (existingData?.Items.Length > 0)
            {
                ClearProjectState(project, stateSet);
            }
        }

        private static string GetSyntaxLogMessage(Document document, TextSpan? span, DiagnosticAnalyzer analyzer)
        {
            return string.Format("syntax: {0}, {1}, {2}", document.FilePath ?? document.Name, span.HasValue ? span.Value.ToString() : "Full", analyzer.ToString());
        }

        private static string GetSemanticLogMessage(Document document, TextSpan? span, DiagnosticAnalyzer analyzer)
        {
            return string.Format("semantic: {0}, {1}, {2}", document.FilePath ?? document.Name, span.HasValue ? span.Value.ToString() : "Full", analyzer.ToString());
        }

        private static string GetProjectLogMessage(Project project, DiagnosticAnalyzer analyzer)
        {
            return string.Format("project: {0}, {1}", project.FilePath ?? project.Name, analyzer.ToString());
        }

        private static string GetResetLogMessage(Document document)
        {
            return string.Format("document reset: {0}", document.FilePath ?? document.Name);
        }

        private static string GetOpenLogMessage(Document document)
        {
            return string.Format("document open: {0}", document.FilePath ?? document.Name);
        }

        private static string GetRemoveLogMessage(DocumentId id)
        {
            return string.Format("document remove: {0}", id.ToString());
        }

        private static string GetRemoveLogMessage(ProjectId id)
        {
            return string.Format("project remove: {0}", id.ToString());
        }

        public override Task NewSolutionSnapshotAsync(Solution newSolution, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }
    }
}
