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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    using ProviderId = Int32;

    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        private static readonly int s_stateTypeCount = Enum.GetNames(typeof(StateType)).Count();
        private static readonly ImmutableArray<StateType> s_documentScopeStateTypes = ImmutableArray.Create<StateType>(StateType.Syntax, StateType.Document);

        private readonly int _correlationId;
        private readonly DiagnosticAnalyzerService _owner;
        private readonly MemberRangeMap _memberRangeMap;
        private readonly DiagnosticAnalyzersAndStates _analyzersAndState;
        private readonly AnalyzerExecutor _executor;

        private DiagnosticLogAggregator _diagnosticLogAggregator;

        public DiagnosticIncrementalAnalyzer(DiagnosticAnalyzerService owner, int correlationId, Workspace workspace, AnalyzerManager analyzerManager)
        {
            _owner = owner;
            _correlationId = correlationId;
            _memberRangeMap = new MemberRangeMap();
            _analyzersAndState = new DiagnosticAnalyzersAndStates(this, workspace, analyzerManager);
            _executor = new AnalyzerExecutor(this);

            _diagnosticLogAggregator = new DiagnosticLogAggregator(_owner);
        }

        public override Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentOpen, GetOpenLogMessage, document, cancellationToken))
            {
                // we remove whatever information we used to have on document open/close and re-calcuate diagnostics
                // we had to do this since some diagnostic provider change its behavior based on whether the document is opend or not.
                // so we can't use cached information.
                return RemoveAllCacheDataAsync(document, cancellationToken);
            }
        }

        public override Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentReset, GetResetLogMessage, document, cancellationToken))
            {
                // we don't need the info for closed file
                _memberRangeMap.Remove(document.Id);

                // we remove whatever information we used to have on document open/close and re-calcuate diagnostics
                // we had to do this since some diagnostic provider change its behavior based on whether the document is opend or not.
                // so we can't use cached information.
                return RemoveAllCacheDataAsync(document, cancellationToken);
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

                var userDiagnosticDriver = new DiagnosticAnalyzerDriver(document, fullSpan, root, _diagnosticLogAggregator, cancellationToken);
                var options = document.Project.CompilationOptions;
                var openedDocument = document.IsOpen();

                foreach (var providerAndId in await _analyzersAndState.GetAllProviderAndIdsAsync(document.Project, cancellationToken).ConfigureAwait(false))
                {
                    var provider = providerAndId.Key;
                    var providerId = providerAndId.Value;

                    if (IsAnalyzerSuppressed(provider, options, userDiagnosticDriver))
                    {
                        await HandleSuppressedAnalyzerAsync(document, StateType.Syntax, providerId, provider, cancellationToken).ConfigureAwait(false);
                    }
                    else if (ShouldRunProviderForStateType(StateType.Syntax, provider, userDiagnosticDriver, diagnosticIds) &&
                        (skipClosedFileChecks || ShouldRunProviderForClosedFile(openedDocument, provider)))
                    {
                        var data = await _executor.GetSyntaxAnalysisDataAsync(provider, providerId, versions, userDiagnosticDriver).ConfigureAwait(false);
                        if (data.FromCache)
                        {
                            RaiseDiagnosticsUpdated(StateType.Syntax, document.Id, providerId, new SolutionArgument(document), data.Items);
                            continue;
                        }

                        var state = _analyzersAndState.GetOrCreateDiagnosticState(StateType.Syntax, providerId, provider, document.Project.Id, document.Project.Language);
                        await state.PersistAsync(document, data.ToPersistData(), cancellationToken).ConfigureAwait(false);

                        RaiseDiagnosticsUpdatedIfNeeded(StateType.Syntax, document, providerId, data.OldItems, data.Items);
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

                var spanBasedDriver = new DiagnosticAnalyzerDriver(document, member.FullSpan, root, _diagnosticLogAggregator, cancellationToken);
                var documentBasedDriver = new DiagnosticAnalyzerDriver(document, root.FullSpan, root, _diagnosticLogAggregator, cancellationToken);
                var options = document.Project.CompilationOptions;

                foreach (var providerAndId in await _analyzersAndState.GetAllProviderAndIdsAsync(document.Project, cancellationToken).ConfigureAwait(false))
                {
                    var provider = providerAndId.Key;
                    var providerId = providerAndId.Value;

                    bool supportsSemanticInSpan;
                    if (IsAnalyzerSuppressed(provider, options, spanBasedDriver))
                    {
                        await HandleSuppressedAnalyzerAsync(document, StateType.Document, providerId, provider, cancellationToken).ConfigureAwait(false);
                    }
                    else if (ShouldRunProviderForStateType(StateType.Document, provider, spanBasedDriver, out supportsSemanticInSpan))
                    {
                        var userDiagnosticDriver = supportsSemanticInSpan ? spanBasedDriver : documentBasedDriver;

                        var ranges = _memberRangeMap.GetSavedMemberRange(providerId, document);
                        var data = await _executor.GetDocumentBodyAnalysisDataAsync(
                            provider, providerId, versions, userDiagnosticDriver, root, member, memberId, supportsSemanticInSpan, ranges).ConfigureAwait(false);

                        _memberRangeMap.UpdateMemberRange(providerId, document, versions.TextVersion, memberId, member.FullSpan, ranges);

                        var state = _analyzersAndState.GetOrCreateDiagnosticState(StateType.Document, providerId, provider, document.Project.Id, document.Project.Language);
                        await state.PersistAsync(document, data.ToPersistData(), cancellationToken).ConfigureAwait(false);

                        if (data.FromCache)
                        {
                            RaiseDiagnosticsUpdated(StateType.Document, document.Id, providerId, new SolutionArgument(document), data.Items);
                            continue;
                        }

                        RaiseDiagnosticsUpdatedIfNeeded(StateType.Document, document, providerId, data.OldItems, data.Items);
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

                var userDiagnosticDriver = new DiagnosticAnalyzerDriver(document, fullSpan, root, _diagnosticLogAggregator, cancellationToken);
                bool openedDocument = document.IsOpen();
                var options = document.Project.CompilationOptions;

                foreach (var providerAndId in await _analyzersAndState.GetAllProviderAndIdsAsync(document.Project, cancellationToken).ConfigureAwait(false))
                {
                    var provider = providerAndId.Key;
                    var providerId = providerAndId.Value;

                    if (IsAnalyzerSuppressed(provider, options, userDiagnosticDriver))
                    {
                        await HandleSuppressedAnalyzerAsync(document, StateType.Document, providerId, provider, cancellationToken).ConfigureAwait(false);
                    }
                    else if (ShouldRunProviderForStateType(StateType.Document, provider, userDiagnosticDriver, diagnosticIds) &&
                        (skipClosedFileChecks || ShouldRunProviderForClosedFile(openedDocument, provider)))
                    {
                        var data = await _executor.GetDocumentAnalysisDataAsync(provider, providerId, versions, userDiagnosticDriver).ConfigureAwait(false);
                        if (data.FromCache)
                        {
                            RaiseDiagnosticsUpdated(StateType.Document, document.Id, providerId, new SolutionArgument(document), data.Items);
                            continue;
                        }

                        if (openedDocument)
                        {
                            _memberRangeMap.Touch(providerId, document, versions.TextVersion);
                        }

                        var state = _analyzersAndState.GetOrCreateDiagnosticState(StateType.Document, providerId, provider, document.Project.Id, document.Project.Language);
                        await state.PersistAsync(document, data.ToPersistData(), cancellationToken).ConfigureAwait(false);

                        RaiseDiagnosticsUpdatedIfNeeded(StateType.Document, document, providerId, data.OldItems, data.Items);
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
                var userDiagnosticDriver = new DiagnosticAnalyzerDriver(project, _diagnosticLogAggregator, cancellationToken);
                var options = project.CompilationOptions;

                var versions = new VersionArgument(VersionStamp.Default, semanticVersion, projectVersion);
                foreach (var providerAndId in await _analyzersAndState.GetAllProviderAndIdsAsync(project, cancellationToken).ConfigureAwait(false))
                {
                    var provider = providerAndId.Key;
                    var providerId = providerAndId.Value;

                    if (IsAnalyzerSuppressed(provider, options, userDiagnosticDriver))
                    {
                        await HandleSuppressedAnalyzerAsync(project, providerId, provider, cancellationToken).ConfigureAwait(false);
                    }
                    else if (ShouldRunProviderForStateType(StateType.Project, provider, userDiagnosticDriver, diagnosticIds) &&
                        (skipClosedFileChecks || ShouldRunProviderForClosedFile(openedDocument: false, provider: provider)))
                    {
                        var data = await _executor.GetProjectAnalysisDataAsync(provider, providerId, versions, userDiagnosticDriver).ConfigureAwait(false);
                        if (data.FromCache)
                        {
                            RaiseDiagnosticsUpdated(StateType.Project, project.Id, providerId, new SolutionArgument(project), data.Items);
                            continue;
                        }

                        var state = _analyzersAndState.GetOrCreateDiagnosticState(StateType.Project, providerId, provider, project.Id, project.Language);
                        await state.PersistAsync(project, data.ToPersistData(), cancellationToken).ConfigureAwait(false);

                        RaiseDiagnosticsUpdatedIfNeeded(project, providerId, data.OldItems, data.Items);
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

                foreach (var stateProviderIdAndType in _analyzersAndState.GetAllExistingDiagnosticStates(documentId.ProjectId))
                {
                    var state = stateProviderIdAndType.Item1;
                    var providerId = stateProviderIdAndType.Item2;
                    var type = stateProviderIdAndType.Item3;

                    if (state != null)
                    {
                        state.Remove(documentId);
                    }

                    var solutionArgs = new SolutionArgument(null, documentId.ProjectId, documentId);

                    RaiseDiagnosticsUpdated(type, documentId, providerId, solutionArgs, ImmutableArray<DiagnosticData>.Empty);
                }
            }
        }

        public override void RemoveProject(ProjectId projectId)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_RemoveProject, GetRemoveLogMessage, projectId, CancellationToken.None))
            {
                foreach (var stateProviderIdAndType in _analyzersAndState.GetAllExistingDiagnosticStates(projectId, StateType.Project))
                {
                    var state = stateProviderIdAndType.Item1;
                    var providerId = stateProviderIdAndType.Item2;

                    if (state != null)
                    {
                        state.Remove(projectId);
                    }

                    var solutionArgs = new SolutionArgument(null, projectId, null);

                    RaiseDiagnosticsUpdated(StateType.Project, projectId, providerId, solutionArgs, ImmutableArray<DiagnosticData>.Empty);
                }

                _analyzersAndState.RemoveProjectAnalyzersAndStates(projectId);
            }
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
            StateType stateType,
            Document document, TextSpan range, SyntaxNode root,
            List<DiagnosticData> diagnostics, bool requireUpToDateDocumentDiagnostic,
            Func<VersionStamp, VersionStamp, bool> versionCheck,
            Func<ProviderId, DiagnosticAnalyzer, DiagnosticAnalyzerDriver, Task<IEnumerable<DiagnosticData>>> getDiagnostics,
            CancellationToken cancellationToken)
        {
            try
            {
                bool result = true;
                var fullSpan = root == null ? null : (TextSpan?)root.FullSpan;

                // Share the diagnostic analyzer driver across all analyzers.
                var spanBasedDriver = new DiagnosticAnalyzerDriver(document, range, root, _diagnosticLogAggregator, cancellationToken);
                var documentBasedDriver = new DiagnosticAnalyzerDriver(document, fullSpan, root, _diagnosticLogAggregator, cancellationToken);
                var options = document.Project.CompilationOptions;

                foreach (var providerAndId in await _analyzersAndState.GetAllProviderAndIdsAsync(document.Project, cancellationToken).ConfigureAwait(false))
                {
                    var provider = providerAndId.Key;
                    var providerId = providerAndId.Value;

                    bool supportsSemanticInSpan;
                    if (!IsAnalyzerSuppressed(provider, options, spanBasedDriver) &&
                        ShouldRunProviderForStateType(stateType, provider, spanBasedDriver, out supportsSemanticInSpan))
                    {
                        var userDiagnosticDriver = supportsSemanticInSpan ? spanBasedDriver : documentBasedDriver;

                        result &= await TryGetLatestDiagnosticsAsync(
                            provider, providerId, stateType,
                            document, range, root, diagnostics, requireUpToDateDocumentDiagnostic,
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
            DiagnosticAnalyzer provider, ProviderId providerId,
            StateType stateType, Document document, TextSpan range, SyntaxNode root,
            List<DiagnosticData> diagnostics, bool requireUpToDateDocumentDiagnostic,
            Func<VersionStamp, VersionStamp, bool> versionCheck,
            Func<ProviderId, DiagnosticAnalyzer, DiagnosticAnalyzerDriver, Task<IEnumerable<DiagnosticData>>> getDiagnostics,
            bool supportsSemanticInSpan,
            DiagnosticAnalyzerDriver userDiagnosticDriver,
            CancellationToken cancellationToken)
        {
            try
            {
                var shouldInclude = (Func<DiagnosticData, bool>)(d => range.IntersectsWith(d.TextSpan));

                // make sure we get state even when none of our analyzer has ran yet. 
                // but this shouldn't create analyzer that doesnt belong to this project (language)
                var state = _analyzersAndState.GetOrCreateDiagnosticState(stateType, providerId, provider, document.Project.Id, document.Project.Language);
                if (state == null)
                {
                    if (!requireUpToDateDocumentDiagnostic)
                    {
                        // the provider never ran yet.
                        return true;
                    }
                }
                else
                {
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
                }

                // check whether we want up-to-date document wide diagnostics
                if (stateType == StateType.Document && !supportsSemanticInSpan && !requireUpToDateDocumentDiagnostic)
                {
                    return false;
                }

                var dx = await getDiagnostics(providerId, provider, userDiagnosticDriver).ConfigureAwait(false);
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

        private bool ShouldRunProviderForClosedFile(bool openedDocument, DiagnosticAnalyzer provider)
        {
            // we have opened document, doesnt matter
            if (openedDocument)
            {
                return true;
            }

            return _owner.GetDiagnosticDescriptors(provider).Any(d => d.DefaultSeverity != DiagnosticSeverity.Hidden);
        }

        private bool ShouldRunProviderForStateType(StateType stateTypeId, DiagnosticAnalyzer provider,
            DiagnosticAnalyzerDriver driver, ImmutableHashSet<string> diagnosticIds)
        {
            return ShouldRunProviderForStateType(stateTypeId, provider, driver, diagnosticIds, _owner.GetDiagnosticDescriptors);
        }

        private static bool ShouldRunProviderForStateType(StateType stateTypeId, DiagnosticAnalyzer provider, DiagnosticAnalyzerDriver driver,
            ImmutableHashSet<string> diagnosticIds, Func<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>> getDescriptor)
        {
            bool discarded;
            return ShouldRunProviderForStateType(stateTypeId, provider, driver, out discarded, diagnosticIds, getDescriptor);
        }

        private static bool ShouldRunProviderForStateType(StateType stateTypeId, DiagnosticAnalyzer provider, DiagnosticAnalyzerDriver driver,
            out bool supportsSemanticInSpan, ImmutableHashSet<string> diagnosticIds = null, Func<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>> getDescriptor = null)
        {
            Debug.Assert(!IsAnalyzerSuppressed(provider, driver.Project.CompilationOptions, driver));

            supportsSemanticInSpan = false;
            if (diagnosticIds != null && getDescriptor(provider).All(d => !diagnosticIds.Contains(d.Id)))
            {
                return false;
            }

            switch (stateTypeId)
            {
                case StateType.Syntax:
                    return provider.SupportsSyntaxDiagnosticAnalysis(driver);

                case StateType.Document:
                    return provider.SupportsSemanticDiagnosticAnalysis(driver, out supportsSemanticInSpan);

                case StateType.Project:
                    return provider.SupportsProjectDiagnosticAnalysis(driver);

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private static bool IsAnalyzerSuppressed(DiagnosticAnalyzer provider, CompilationOptions options, DiagnosticAnalyzerDriver driver)
        {
            if (options != null && CompilationWithAnalyzers.IsDiagnosticAnalyzerSuppressed(provider, options, driver.CatchAnalyzerExceptionHandler))
            {
                // All diagnostics that are generated by this DiagnosticAnalyzer will be suppressed, so we need not run the analyzer.
                return true;
            }

            return false;
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
            StateType type, Document document, ProviderId providerId, ImmutableArray<DiagnosticData> existingItems, ImmutableArray<DiagnosticData> newItems)
        {
            var noItems = existingItems.Length == 0 && newItems.Length == 0;
            if (!noItems)
            {
                RaiseDiagnosticsUpdated(type, document.Id, providerId, new SolutionArgument(document), newItems);
            }
        }

        private void RaiseDiagnosticsUpdatedIfNeeded(Project project, ProviderId providerId, ImmutableArray<DiagnosticData> existingItems, ImmutableArray<DiagnosticData> newItems)
        {
            var noItems = existingItems.Length == 0 && newItems.Length == 0;
            if (!noItems)
            {
                RaiseDiagnosticsUpdated(StateType.Project, project.Id, providerId, new SolutionArgument(project), newItems);
            }
        }

        private void RaiseDiagnosticsUpdated(
            StateType type, object key, ProviderId providerId, SolutionArgument solution, ImmutableArray<DiagnosticData> diagnostics)
        {
            if (_owner != null)
            {
                var id = new ArgumentKey(providerId, type, key);
                _owner.RaiseDiagnosticsUpdated(this,
                    new DiagnosticsUpdatedArgs(id, _analyzersAndState.Workspace, solution.Solution, solution.ProjectId, solution.DocumentId, diagnostics));
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

        private static async Task<IEnumerable<DiagnosticData>> GetSyntaxDiagnosticsAsync(ProviderId providerId, DiagnosticAnalyzer provider, DiagnosticAnalyzerDriver userDiagnosticDriver)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_SyntaxDiagnostic, GetSyntaxLogMessage, userDiagnosticDriver.Document, userDiagnosticDriver.Span, providerId, userDiagnosticDriver.CancellationToken))
            {
                try
                {
                    Contract.ThrowIfNull(provider);

                    var diagnostics = await userDiagnosticDriver.GetSyntaxDiagnosticsAsync(provider).ConfigureAwait(false);
                    return GetDiagnosticData(userDiagnosticDriver.Document, userDiagnosticDriver.Span, diagnostics);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        private static async Task<IEnumerable<DiagnosticData>> GetSemanticDiagnosticsAsync(ProviderId providerId, DiagnosticAnalyzer provider, DiagnosticAnalyzerDriver userDiagnosticDriver)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_SemanticDiagnostic, GetSemanticLogMessage, userDiagnosticDriver.Document, userDiagnosticDriver.Span, providerId, userDiagnosticDriver.CancellationToken))
            {
                try
                {
                    Contract.ThrowIfNull(provider);

                    var diagnostics = await userDiagnosticDriver.GetSemanticDiagnosticsAsync(provider).ConfigureAwait(false);
                    return GetDiagnosticData(userDiagnosticDriver.Document, userDiagnosticDriver.Span, diagnostics);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        private static async Task<IEnumerable<DiagnosticData>> GetProjectDiagnosticsAsync(ProviderId providerId, DiagnosticAnalyzer provider, DiagnosticAnalyzerDriver userDiagnosticDriver, Action<Project, DiagnosticAnalyzer, CancellationToken> forceAnalyzeAllDocuments)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_ProjectDiagnostic, GetProjectLogMessage, userDiagnosticDriver.Project, providerId, userDiagnosticDriver.CancellationToken))
            {
                try
                {
                    Contract.ThrowIfNull(provider);

                    var diagnostics = await userDiagnosticDriver.GetProjectDiagnosticsAsync(provider, forceAnalyzeAllDocuments).ConfigureAwait(false);
                    return GetDiagnosticData(userDiagnosticDriver.Project, diagnostics);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
        }

        private async Task RemoveAllCacheDataAsync(Document document, CancellationToken cancellationToken)
        {
            var allExistingStates = _analyzersAndState.GetAllExistingDiagnosticStates(document.Project.Id, document.Project.Language);
            await RemoveCacheDataAsync(document, allExistingStates, cancellationToken).ConfigureAwait(false);
        }

        private async Task RemoveCacheDataAsync(Document document, IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> states, CancellationToken cancellationToken)
        {
            try
            {
                // Compiler + User diagnostics
                foreach (var stateProviderIdAndType in states)
                {
                    var state = stateProviderIdAndType.Item1;
                    if (state == null)
                    {
                        continue;
                    }

                    var providerId = stateProviderIdAndType.Item2;
                    var type = stateProviderIdAndType.Item3;
                    await RemoveCacheDataAsync(document, state, providerId, type, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task RemoveCacheDataAsync(Document document, DiagnosticState state, ProviderId providerId, StateType type, CancellationToken cancellationToken)
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

                RaiseDiagnosticsUpdated(type, key, providerId, solutionArgs, ImmutableArray<DiagnosticData>.Empty);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task RemoveCacheDataAsync(Project project, DiagnosticState state, ProviderId providerId, CancellationToken cancellationToken)
        {
            try
            {
                // remove memory cache
                state.Remove(project.Id);

                // remove persistent cache
                await state.PersistAsync(project, AnalysisData.Empty, cancellationToken).ConfigureAwait(false);

                // raise diagnostic updated event
                var solutionArgs = new SolutionArgument(project);
                RaiseDiagnosticsUpdated(StateType.Project, project.Id, providerId, solutionArgs, ImmutableArray<DiagnosticData>.Empty);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task RemoveCacheDataAsync(Project project, IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> states, CancellationToken cancellationToken)
        {
            foreach (var document in project.Documents)
            {
                await RemoveCacheDataAsync(document, states, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task HandleSuppressedAnalyzerAsync(Document document, StateType type, ProviderId providerId, DiagnosticAnalyzer provider, CancellationToken cancellationToken)
        {
            var state = _analyzersAndState.GetOrCreateDiagnosticState(type, providerId, provider, document.Project.Id, document.Project.Language);
            var existingData = await state.TryGetExistingDataAsync(document, cancellationToken).ConfigureAwait(false);
            if (existingData != null && existingData.Items.Length > 0)
            {
                await RemoveCacheDataAsync(document, state, providerId, type, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task HandleSuppressedAnalyzerAsync(Project project, ProviderId providerId, DiagnosticAnalyzer provider, CancellationToken cancellationToken)
        {
            var state = _analyzersAndState.GetOrCreateDiagnosticState(StateType.Project, providerId, provider, project.Id, project.Language);
            var existingData = await state.TryGetExistingDataAsync(project, cancellationToken).ConfigureAwait(false);
            if (existingData != null && existingData.Items.Length > 0)
            {
                await RemoveCacheDataAsync(project, state, providerId, cancellationToken).ConfigureAwait(false);
            }
        }

        private static string GetSyntaxLogMessage(Document document, TextSpan? span, int providerId)
        {
            return string.Format("syntax: {0}, {1}, {2}", document.FilePath ?? document.Name, span.HasValue ? span.Value.ToString() : "Full", providerId.ToString());
        }

        private static string GetSemanticLogMessage(Document document, TextSpan? span, int providerId)
        {
            return string.Format("semantic: {0}, {1}, {2}", document.FilePath ?? document.Name, span.HasValue ? span.Value.ToString() : "Full", providerId.ToString());
        }

        private static string GetProjectLogMessage(Project project, int providerId)
        {
            return string.Format("project: {0}, {1}", project.FilePath ?? project.Name, providerId.ToString());
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
