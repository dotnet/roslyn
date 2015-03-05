// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        private static readonly ImmutableArray<StateType> s_documentScopeStateTypes = ImmutableArray.Create<StateType>(StateType.Syntax, StateType.Document);

        private readonly int _correlationId;
        private readonly DiagnosticAnalyzerService _owner;
        private readonly MemberRangeMap _memberRangeMap;
        private readonly AnalyzerExecutor _executor;
        private readonly StateManager _stateManger;
        private readonly SimpleTaskQueue _eventQueue;

        private DiagnosticLogAggregator _diagnosticLogAggregator;

        public DiagnosticIncrementalAnalyzer(
            DiagnosticAnalyzerService owner, 
            int correlationId, 
            Workspace workspace, 
            HostAnalyzerManager analyzerManager,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
            : base(workspace, hostDiagnosticUpdateSource)
        {
            _owner = owner;
            _correlationId = correlationId;
            _memberRangeMap = new MemberRangeMap();
            _executor = new AnalyzerExecutor(this);
            _eventQueue = new SimpleTaskQueue(TaskScheduler.Default);
            
            _stateManger = new StateManager(analyzerManager);
            _stateManger.ProjectAnalyzerReferenceChanged += OnProjectAnalyzerReferenceChanged;

            _diagnosticLogAggregator = new DiagnosticLogAggregator(_owner);
        }

        private void OnProjectAnalyzerReferenceChanged(object sender, ProjectAnalyzerReferenceChangedEventArgs e)
        {
            if (e.Removed.Length == 0)
            {
                // nothing to refresh
                return;
            }

            // guarantee order of the events.
            var asyncToken = _owner.Listener.BeginAsyncOperation(nameof(OnProjectAnalyzerReferenceChanged));
            _eventQueue.ScheduleTask(() => ClearProjectStatesAsync(e.Project, e.Removed, CancellationToken.None), CancellationToken.None).CompletesAsyncOperation(asyncToken);
        }

        public override Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentOpen, GetOpenLogMessage, document, cancellationToken))
            {
                // we remove whatever information we used to have on document open/close and re-calcuate diagnostics
                // we had to do this since some diagnostic analyzer change its behavior based on whether the document is opend or not.
                // so we can't use cached information.
                return ClearDocumentStatesAsync(document, _stateManger.GetStateSets(document.Project), cancellationToken);
            }
        }

        public override Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentReset, GetResetLogMessage, document, cancellationToken))
            {
                // we don't need the info for closed file
                _memberRangeMap.Remove(document.Id);

                // we remove whatever information we used to have on document open/close and re-calcuate diagnostics
                // we had to do this since some diagnostic analyzer change its behavior based on whether the document is opend or not.
                // so we can't use cached information.
                return ClearDocumentStatesAsync(document, _stateManger.GetStateSets(document.Project), cancellationToken);
            }
        }

        private bool CheckOption(Workspace workspace, string language, bool documentOpened)
        {
            var optionService = workspace.Services.GetService<IOptionService>();
            if (optionService == null || optionService.GetOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, language))
            {
                return true;
            }

            if (documentOpened)
            {
                return true;
            }

            return false;
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

                var userDiagnosticDriver = new DiagnosticAnalyzerDriver(document, fullSpan, root, _diagnosticLogAggregator, HostDiagnosticUpdateSource, cancellationToken);
                var openedDocument = document.IsOpen();

                foreach (var stateSet in _stateManger.GetOrUpdateStateSets(document.Project))
                {
                    if (userDiagnosticDriver.IsAnalyzerSuppressed(stateSet.Analyzer))
                    {
                        await HandleSuppressedAnalyzerAsync(document, stateSet, StateType.Syntax, cancellationToken).ConfigureAwait(false);
                    }
                    else if (ShouldRunAnalyzerForStateType(userDiagnosticDriver, stateSet.Analyzer, StateType.Syntax, diagnosticIds) &&
                        (skipClosedFileChecks || ShouldRunAnalyzerForClosedFile(openedDocument, stateSet.Analyzer)))
                    {
                        var data = await _executor.GetSyntaxAnalysisDataAsync(userDiagnosticDriver, stateSet, versions).ConfigureAwait(false);
                        if (data.FromCache)
                        {
                            RaiseDiagnosticsUpdated(StateType.Syntax, document.Id, stateSet.Analyzer, new SolutionArgument(document), data.Items);
                            continue;
                        }

                        var state = stateSet.GetState(StateType.Syntax);
                        await state.PersistAsync(document, data.ToPersistData(), cancellationToken).ConfigureAwait(false);

                        RaiseDiagnosticsUpdatedIfNeeded(StateType.Syntax, document, stateSet.Analyzer, data.OldItems, data.Items);
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

                var spanBasedDriver = new DiagnosticAnalyzerDriver(document, member.FullSpan, root, _diagnosticLogAggregator, HostDiagnosticUpdateSource, cancellationToken);
                var documentBasedDriver = new DiagnosticAnalyzerDriver(document, root.FullSpan, root, _diagnosticLogAggregator, HostDiagnosticUpdateSource, cancellationToken);
                
                foreach (var stateSet in _stateManger.GetOrUpdateStateSets(document.Project))
                {
                    bool supportsSemanticInSpan;
                    if (spanBasedDriver.IsAnalyzerSuppressed(stateSet.Analyzer))
                    {
                        await HandleSuppressedAnalyzerAsync(document, stateSet, StateType.Document, cancellationToken).ConfigureAwait(false);
                    }
                    else if (ShouldRunAnalyzerForStateType(spanBasedDriver, stateSet.Analyzer, StateType.Document, out supportsSemanticInSpan))
                    {
                        var userDiagnosticDriver = supportsSemanticInSpan ? spanBasedDriver : documentBasedDriver;

                        var ranges = _memberRangeMap.GetSavedMemberRange(stateSet.Analyzer, document);
                        var data = await _executor.GetDocumentBodyAnalysisDataAsync(
                            stateSet, versions, userDiagnosticDriver, root, member, memberId, supportsSemanticInSpan, ranges).ConfigureAwait(false);

                        _memberRangeMap.UpdateMemberRange(stateSet.Analyzer, document, versions.TextVersion, memberId, member.FullSpan, ranges);

                        var state = stateSet.GetState(StateType.Document);
                        await state.PersistAsync(document, data.ToPersistData(), cancellationToken).ConfigureAwait(false);

                        if (data.FromCache)
                        {
                            RaiseDiagnosticsUpdated(StateType.Document, document.Id, stateSet.Analyzer, new SolutionArgument(document), data.Items);
                            continue;
                        }

                        RaiseDiagnosticsUpdatedIfNeeded(StateType.Document, document, stateSet.Analyzer, data.OldItems, data.Items);
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

                var userDiagnosticDriver = new DiagnosticAnalyzerDriver(document, fullSpan, root, _diagnosticLogAggregator, HostDiagnosticUpdateSource, cancellationToken);
                bool openedDocument = document.IsOpen();
                
                foreach (var stateSet in _stateManger.GetOrUpdateStateSets(document.Project))
                {
                    if (userDiagnosticDriver.IsAnalyzerSuppressed(stateSet.Analyzer))
                    {
                        await HandleSuppressedAnalyzerAsync(document, stateSet, StateType.Document, cancellationToken).ConfigureAwait(false);
                    }
                    else if (ShouldRunAnalyzerForStateType(userDiagnosticDriver, stateSet.Analyzer, StateType.Document, diagnosticIds) &&
                        (skipClosedFileChecks || ShouldRunAnalyzerForClosedFile(openedDocument, stateSet.Analyzer)))
                    {
                        var data = await _executor.GetDocumentAnalysisDataAsync(userDiagnosticDriver, stateSet, versions).ConfigureAwait(false);
                        if (data.FromCache)
                        {
                            RaiseDiagnosticsUpdated(StateType.Document, document.Id, stateSet.Analyzer, new SolutionArgument(document), data.Items);
                            continue;
                        }

                        if (openedDocument)
                        {
                            _memberRangeMap.Touch(stateSet.Analyzer, document, versions.TextVersion);
                        }

                        var state = stateSet.GetState(StateType.Document);
                        await state.PersistAsync(document, data.ToPersistData(), cancellationToken).ConfigureAwait(false);

                        RaiseDiagnosticsUpdatedIfNeeded(StateType.Document, document, stateSet.Analyzer, data.OldItems, data.Items);
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
            await AnalyzeProjectAsync(project, diagnosticIds: null, skipClosedFileChecks: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task AnalyzeProjectAsync(Project project, ImmutableHashSet<string> diagnosticIds, bool skipClosedFileChecks, CancellationToken cancellationToken)
        {
            try
            {
                if (!skipClosedFileChecks && !CheckOption(project.Solution.Workspace, project.Language, documentOpened: project.Documents.Any(d => d.IsOpen())))
                {
                    return;
                }

                var projectVersion = await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
                var semanticVersion = await project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
                var userDiagnosticDriver = new DiagnosticAnalyzerDriver(project, _diagnosticLogAggregator, HostDiagnosticUpdateSource, cancellationToken);
                
                var versions = new VersionArgument(VersionStamp.Default, semanticVersion, projectVersion);
                foreach (var stateSet in _stateManger.GetOrUpdateStateSets(project))
                {
                    if (userDiagnosticDriver.IsAnalyzerSuppressed(stateSet.Analyzer))
                    {
                        await HandleSuppressedAnalyzerAsync(project, stateSet, cancellationToken).ConfigureAwait(false);
                    }
                    else if (ShouldRunAnalyzerForStateType(userDiagnosticDriver, stateSet.Analyzer, StateType.Project, diagnosticIds) &&
                        (skipClosedFileChecks || ShouldRunAnalyzerForClosedFile(openedDocument: false, analyzer: stateSet.Analyzer)))
                    {
                        var data = await _executor.GetProjectAnalysisDataAsync(userDiagnosticDriver, stateSet, versions).ConfigureAwait(false);
                        if (data.FromCache)
                        {
                            RaiseDiagnosticsUpdated(StateType.Project, project.Id, stateSet.Analyzer, new SolutionArgument(project), data.Items);
                            continue;
                        }

                        var state = stateSet.GetState(StateType.Project);
                        await state.PersistAsync(project, data.ToPersistData(), cancellationToken).ConfigureAwait(false);

                        RaiseDiagnosticsUpdatedIfNeeded(project, stateSet.Analyzer, data.OldItems, data.Items);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public override void RemoveDocument(DocumentId documentId)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_RemoveDocument, GetRemoveLogMessage, documentId, CancellationToken.None))
            {
                _memberRangeMap.Remove(documentId);

                foreach (var stateSet in _stateManger.GetStateSets(documentId.ProjectId))
                {
                    stateSet.Remove(documentId);

                    var solutionArgs = new SolutionArgument(null, documentId.ProjectId, documentId);
                    foreach (var stateType in s_documentScopeStateTypes)
                    {
                        RaiseDiagnosticsUpdated(stateType, documentId, stateSet.Analyzer, solutionArgs, ImmutableArray<DiagnosticData>.Empty);
                    }
                }
            }
        }

        public override void RemoveProject(ProjectId projectId)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_RemoveProject, GetRemoveLogMessage, projectId, CancellationToken.None))
            {
                foreach (var stateSet in _stateManger.GetStateSets(projectId))
                {
                    stateSet.Remove(projectId);

                    var solutionArgs = new SolutionArgument(null, projectId, null);
                    RaiseDiagnosticsUpdated(StateType.Project, projectId, stateSet.Analyzer, solutionArgs, ImmutableArray<DiagnosticData>.Empty);
                }
            }

            _stateManger.RemoveStateSet(projectId);
            }

        public override async Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> diagnostics, CancellationToken cancellationToken)
        {
            try
            {
                var textVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
                var semanticVersion = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var result = true;
                result &= await TryGetLatestDiagnosticsAsync(
                    StateType.Syntax, document, range, root, diagnostics, false,
                    (t, d) => t.Equals(textVersion) && d.Equals(syntaxVersion),
                    GetSyntaxDiagnosticsAsync, cancellationToken).ConfigureAwait(false);

                result &= await TryGetLatestDiagnosticsAsync(
                    StateType.Document, document, range, root, diagnostics, false,
                    (t, d) => t.Equals(textVersion) && d.Equals(semanticVersion),
                    GetSemanticDiagnosticsAsync, cancellationToken).ConfigureAwait(false);

                return result;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public override async Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, CancellationToken cancellationToken)
        {
            try
            {
                var textVersion = await document.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
                var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
                var semanticVersion = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var result = true;
                using (var diagnostics = SharedPools.Default<List<DiagnosticData>>().GetPooledObject())
                {
                    result &= await TryGetLatestDiagnosticsAsync(
                            StateType.Syntax, document, range, root, diagnostics.Object, true,
                        (t, d) => t.Equals(textVersion) && d.Equals(syntaxVersion),
                        GetSyntaxDiagnosticsAsync, cancellationToken).ConfigureAwait(false);

                    result &= await TryGetLatestDiagnosticsAsync(
                            StateType.Document, document, range, root, diagnostics.Object, true,
                        (t, d) => t.Equals(textVersion) && d.Equals(semanticVersion),
                        GetSemanticDiagnosticsAsync, cancellationToken).ConfigureAwait(false);

                    // must be always up-to-date
                    Debug.Assert(result);
                    if (diagnostics.Object.Count > 0)
                    {
                        return diagnostics.Object.ToImmutableArray();
                    }

                    return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task<bool> TryGetLatestDiagnosticsAsync(
            StateType stateType, Document document, TextSpan range, SyntaxNode root,
            List<DiagnosticData> diagnostics, bool requireUpToDateDocumentDiagnostic,
            Func<VersionStamp, VersionStamp, bool> versionCheck,
            Func<DiagnosticAnalyzerDriver, DiagnosticAnalyzer, Task<IEnumerable<DiagnosticData>>> getDiagnostics,
            CancellationToken cancellationToken)
        {
            try
            {
                bool result = true;
                var fullSpan = root == null ? null : (TextSpan?)root.FullSpan;

                // Share the diagnostic analyzer driver across all analyzers.
                var spanBasedDriver = new DiagnosticAnalyzerDriver(document, range, root, _diagnosticLogAggregator, HostDiagnosticUpdateSource, cancellationToken);
                var documentBasedDriver = new DiagnosticAnalyzerDriver(document, fullSpan, root, _diagnosticLogAggregator, HostDiagnosticUpdateSource, cancellationToken);
                
                foreach (var stateSet in _stateManger.GetOrCreateStateSets(document.Project))
                {
                    bool supportsSemanticInSpan;
                    if (!spanBasedDriver.IsAnalyzerSuppressed(stateSet.Analyzer) &&
                        ShouldRunAnalyzerForStateType(spanBasedDriver, stateSet.Analyzer, stateType, out supportsSemanticInSpan))
                    {
                        var userDiagnosticDriver = supportsSemanticInSpan ? spanBasedDriver : documentBasedDriver;

                        result &= await TryGetLatestDiagnosticsAsync(
                            stateSet, stateType, document, range, root, diagnostics, requireUpToDateDocumentDiagnostic,
                            versionCheck, getDiagnostics, supportsSemanticInSpan, userDiagnosticDriver, cancellationToken).ConfigureAwait(false);
                    }
                }

                return result;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task<bool> TryGetLatestDiagnosticsAsync(
            StateSet stateSet, StateType stateType, Document document, TextSpan range, SyntaxNode root,
            List<DiagnosticData> diagnostics, bool requireUpToDateDocumentDiagnostic,
            Func<VersionStamp, VersionStamp, bool> versionCheck,
            Func<DiagnosticAnalyzerDriver, DiagnosticAnalyzer, Task<IEnumerable<DiagnosticData>>> getDiagnostics,
            bool supportsSemanticInSpan,
            DiagnosticAnalyzerDriver userDiagnosticDriver,
            CancellationToken cancellationToken)
        {
            try
            {
                var shouldInclude = (Func<DiagnosticData, bool>)(d => range.IntersectsWith(d.TextSpan));

                // make sure we get state even when none of our analyzer has ran yet. 
                // but this shouldn't create analyzer that doesnt belong to this project (language)
                var state = stateSet.GetState(stateType);

                    // see whether we can use existing info
                    var existingData = await state.TryGetExistingDataAsync(document, cancellationToken).ConfigureAwait(false);
                    if (existingData != null && versionCheck(existingData.TextVersion, existingData.DataVersion))
                    {
                        if (existingData.Items == null)
                        {
                            return true;
                        }

                        diagnostics.AddRange(existingData.Items.Where(shouldInclude));
                        return true;
                    }

                // check whether we want up-to-date document wide diagnostics
                if (stateType == StateType.Document && !supportsSemanticInSpan && !requireUpToDateDocumentDiagnostic)
                {
                    return false;
                }

                var dx = await getDiagnostics(userDiagnosticDriver, stateSet.Analyzer).ConfigureAwait(false);
                if (dx != null)
                {
                    // no state yet
                    diagnostics.AddRange(dx.Where(shouldInclude));
                }

                return true;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private bool ShouldRunAnalyzerForClosedFile(bool openedDocument, DiagnosticAnalyzer analyzer)
        {
            // we have opened document, doesnt matter
            if (openedDocument)
            {
                return true;
            }

            return _owner.GetDiagnosticDescriptors(analyzer).Any(d => d.DefaultSeverity != DiagnosticSeverity.Hidden);
        }

        private bool ShouldRunAnalyzerForStateType(DiagnosticAnalyzerDriver driver, DiagnosticAnalyzer analyzer,
            StateType stateTypeId, ImmutableHashSet<string> diagnosticIds)
        {
            bool discarded;
            return ShouldRunAnalyzerForStateType(driver, analyzer, stateTypeId, out discarded, diagnosticIds, _owner.GetDiagnosticDescriptors);
        }

        private static bool ShouldRunAnalyzerForStateType(DiagnosticAnalyzerDriver driver, DiagnosticAnalyzer analyzer, StateType stateTypeId,
            out bool supportsSemanticInSpan, ImmutableHashSet<string> diagnosticIds = null, Func<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>> getDescriptor = null)
        {
            Debug.Assert(!driver.IsAnalyzerSuppressed(analyzer));

            supportsSemanticInSpan = false;
            if (diagnosticIds != null && getDescriptor(analyzer).All(d => !diagnosticIds.Contains(d.Id)))
            {
                return false;
            }

            switch (stateTypeId)
            {
                case StateType.Syntax:
                    return analyzer.SupportsSyntaxDiagnosticAnalysis(driver);

                case StateType.Document:
                    return analyzer.SupportsSemanticDiagnosticAnalysis(driver, out supportsSemanticInSpan);

                case StateType.Project:
                    return analyzer.SupportsProjectDiagnosticAnalysis(driver);

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        // internal for testing purposes only.
        internal void ForceAnalyzeAllDocuments(Project project, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            var diagnosticIds = _owner.GetDiagnosticDescriptors(analyzer).Select(d => d.Id).ToImmutableHashSet();
            ReanalyzeAllDocumentsAsync(project, diagnosticIds, cancellationToken).Wait(cancellationToken);
        }

        public override void LogAnalyzerCountSummary()
        {
            DiagnosticAnalyzerLogger.LogAnalyzerCrashCountSummary(_correlationId, _diagnosticLogAggregator);
            DiagnosticAnalyzerLogger.LogAnalyzerTypeCountSummary(_correlationId, _diagnosticLogAggregator);

            // reset the log aggregator
            _diagnosticLogAggregator = new DiagnosticLogAggregator(_owner);
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

            return project.CanReusePersistedDependentSemanticVersion(versions.ProjectVersion, versions.DataVersion, existingData.DataVersion);
        }

        private void RaiseDiagnosticsUpdatedIfNeeded(
            StateType type, Document document, DiagnosticAnalyzer analyzer, ImmutableArray<DiagnosticData> existingItems, ImmutableArray<DiagnosticData> newItems)
        {
            var noItems = existingItems.Length == 0 && newItems.Length == 0;
            if (!noItems)
            {
                RaiseDiagnosticsUpdated(type, document.Id, analyzer, new SolutionArgument(document), newItems);
            }
        }

        private void RaiseDiagnosticsUpdatedIfNeeded(
            Project project, DiagnosticAnalyzer analyzer, ImmutableArray<DiagnosticData> existingItems, ImmutableArray<DiagnosticData> newItems)
        {
            var noItems = existingItems.Length == 0 && newItems.Length == 0;
            if (!noItems)
            {
                RaiseDiagnosticsUpdated(StateType.Project, project.Id, analyzer, new SolutionArgument(project), newItems);
            }
        }

        private void RaiseDiagnosticsUpdated(
            StateType type, object key, DiagnosticAnalyzer analyzer, SolutionArgument solution, ImmutableArray<DiagnosticData> diagnostics)
        {
            if (_owner != null)
            {
                var id = new ArgumentKey(analyzer, type, key);
                _owner.RaiseDiagnosticsUpdated(this,
                    new DiagnosticsUpdatedArgs(id, Workspace, solution.Solution, solution.ProjectId, solution.DocumentId, diagnostics));
            }
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
                diagnostic.MessageFormat,
                diagnostic.Severity,
                diagnostic.DefaultSeverity,
                diagnostic.IsEnabledByDefault,
                diagnostic.WarningLevel,
                diagnostic.CustomTags,
                diagnostic.Properties,
                diagnostic.Workspace,
                diagnostic.ProjectId,
                diagnostic.DocumentId,
                newSpan,
                mappedFilePath: mappedLineInfo.HasMappedPath ? mappedLineInfo.Path : null,
                mappedStartLine: mappedLineInfo.StartLinePosition.Line,
                mappedStartColumn: mappedLineInfo.StartLinePosition.Character,
                mappedEndLine: mappedLineInfo.EndLinePosition.Line,
                mappedEndColumn: mappedLineInfo.EndLinePosition.Character,
                originalFilePath: originalLineInfo.Path,
                originalStartLine: originalLineInfo.StartLinePosition.Line,
                originalStartColumn: originalLineInfo.StartLinePosition.Character,
                originalEndLine: originalLineInfo.EndLinePosition.Line,
                originalEndColumn: originalLineInfo.EndLinePosition.Character,
                description: diagnostic.Description,
                helpLink: diagnostic.HelpLink);
        }

        private static IEnumerable<DiagnosticData> GetDiagnosticData(Document document, TextSpan? span, IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics != null ? diagnostics.Where(dx => ShouldIncludeDiagnostic(dx, span)).Select(d => DiagnosticData.Create(document, d)) : null;
        }

        private static bool ShouldIncludeDiagnostic(Diagnostic diagnostic, TextSpan? span)
        {
            if (diagnostic == null)
            {
                return false;
            }

            if (span == null)
            {
                return true;
            }

            if (diagnostic.Location == null)
            {
                return false;
            }

            return span.Value.Contains(diagnostic.Location.SourceSpan);
        }

        private static IEnumerable<DiagnosticData> GetDiagnosticData(Project project, IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics != null ? diagnostics.Select(d => DiagnosticData.Create(project, d)) : null;
        }

        private static async Task<IEnumerable<DiagnosticData>> GetSyntaxDiagnosticsAsync(DiagnosticAnalyzerDriver userDiagnosticDriver, DiagnosticAnalyzer analyzer)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_SyntaxDiagnostic, GetSyntaxLogMessage, userDiagnosticDriver.Document, userDiagnosticDriver.Span, analyzer, userDiagnosticDriver.CancellationToken))
            {
                try
                {
                    Contract.ThrowIfNull(analyzer);

                    var diagnostics = await userDiagnosticDriver.GetSyntaxDiagnosticsAsync(analyzer).ConfigureAwait(false);
                    return GetDiagnosticData(userDiagnosticDriver.Document, userDiagnosticDriver.Span, diagnostics);
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

                    var diagnostics = await userDiagnosticDriver.GetSemanticDiagnosticsAsync(analyzer).ConfigureAwait(false);
                    return GetDiagnosticData(userDiagnosticDriver.Document, userDiagnosticDriver.Span, diagnostics);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        private static async Task<IEnumerable<DiagnosticData>> GetProjectDiagnosticsAsync(DiagnosticAnalyzerDriver userDiagnosticDriver, DiagnosticAnalyzer analyzer, Action<Project, DiagnosticAnalyzer, CancellationToken> forceAnalyzeAllDocuments)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_ProjectDiagnostic, GetProjectLogMessage, userDiagnosticDriver.Project, analyzer, userDiagnosticDriver.CancellationToken))
            {
                try
                {
                    Contract.ThrowIfNull(analyzer);

                    var diagnostics = await userDiagnosticDriver.GetProjectDiagnosticsAsync(analyzer, forceAnalyzeAllDocuments).ConfigureAwait(false);
                    return GetDiagnosticData(userDiagnosticDriver.Project, diagnostics);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        private async Task ClearDocumentStatesAsync(Document document, IEnumerable<StateSet> states, CancellationToken cancellationToken)
        {
            try
            {
                // Compiler + User diagnostics
                foreach (var state in states)
                {
                    foreach (var stateType in s_documentScopeStateTypes)
                    {
                        await ClearDocumentStateAsync(document, state.Analyzer, stateType, state.GetState(stateType), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task ClearDocumentStateAsync(Document document, DiagnosticAnalyzer analyzer, StateType type, DiagnosticState state, CancellationToken cancellationToken)
        {
            try
            {
                // remove memory cache
                state.Remove(document.Id);

                // remove persistent cache
                await state.PersistAsync(document, AnalysisData.Empty, cancellationToken).ConfigureAwait(false);

                // raise diagnostic updated event
                var documentId = type == StateType.Project ? null : document.Id;
                var projectId = document.Project.Id;
                var key = documentId ?? (object)projectId;
                var solutionArgs = new SolutionArgument(document.Project.Solution, projectId, documentId);

                RaiseDiagnosticsUpdated(type, key, analyzer, solutionArgs, ImmutableArray<DiagnosticData>.Empty);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task ClearProjectStatesAsync(Project project, IEnumerable<StateSet> states, CancellationToken cancellationToken)
        {
            foreach (var document in project.Documents)
            {
                await ClearDocumentStatesAsync(document, states, cancellationToken).ConfigureAwait(false);
            }

            foreach (var state in states)
            {
                await ClearProjectStateAsync(project, state.Analyzer, state.GetState(StateType.Project), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ClearProjectStateAsync(Project project, DiagnosticAnalyzer analyzer, DiagnosticState state, CancellationToken cancellationToken)
        {
            try
            {
                // remove memory cache
                state.Remove(project.Id);

                // remove persistent cache
                await state.PersistAsync(project, AnalysisData.Empty, cancellationToken).ConfigureAwait(false);

                // raise diagnostic updated event
                var solutionArgs = new SolutionArgument(project);
                RaiseDiagnosticsUpdated(StateType.Project, project.Id, analyzer, solutionArgs, ImmutableArray<DiagnosticData>.Empty);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task HandleSuppressedAnalyzerAsync(Document document, StateSet stateSet, StateType type, CancellationToken cancellationToken)
        {
            var state = stateSet.GetState(type);
            var existingData = await state.TryGetExistingDataAsync(document, cancellationToken).ConfigureAwait(false);
            if (existingData != null && existingData.Items.Length > 0)
            {
                await ClearDocumentStateAsync(document, stateSet.Analyzer, type, state, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task HandleSuppressedAnalyzerAsync(Project project, StateSet stateSet, CancellationToken cancellationToken)
        {
            var state = stateSet.GetState(StateType.Project);
            var existingData = await state.TryGetExistingDataAsync(project, cancellationToken).ConfigureAwait(false);
            if (existingData != null && existingData.Items.Length > 0)
            {
                await ClearProjectStateAsync(project, stateSet.Analyzer, state, cancellationToken).ConfigureAwait(false);
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

        #region unused 
        public override Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }
        #endregion
    }
}
