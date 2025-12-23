// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class EditSession
{
    internal readonly DebuggingSession DebuggingSession;
    internal readonly EditSessionTelemetry Telemetry;

    // Maps active statement instructions reported by the debugger to their latest spans that might not yet have been applied
    // (remapping not triggered yet). Consumed by the next edit session and updated when changes are committed at the end of the edit session.
    //
    // Consider a function F containing a call to function G. While G is being executed, F is updated a couple of times (in two edit sessions)
    // before the thread returns from G and is remapped to the latest version of F. At the start of the second edit session,
    // the active instruction reported by the debugger is still at the original location since function F has not been remapped yet (G has not returned yet).
    //
    // '>' indicates an active statement instruction for non-leaf frame reported by the debugger.
    // v1 - before first edit, G executing
    // v2 - after first edit, G still executing
    // v3 - after second edit and G returned
    //
    // F v1:        F v2:       F v3:
    // 0: nop       0: nop      0: nop
    // 1> G()       1> nop      1: nop
    // 2: nop       2: G()      2: nop
    // 3: nop       3: nop      3> G()
    //
    // When entering a break state we query the debugger for current active statements.
    // The returned statements reflect the current state of the threads in the runtime.
    // When a change is successfully applied we remember changes in active statement spans.
    // These changes are passed to the next edit session.
    // We use them to map the spans for active statements returned by the debugger.
    //
    // In the above case the sequence of events is
    // 1st break: get active statements returns (F, v=1, il=1, span1) the active statement is up-to-date
    // 1st apply: detected span change for active statement (F, v=1, il=1): span1->span2
    // 2nd break: previously updated statements contains (F, v=1, il=1)->span2
    //            get active statements returns (F, v=1, il=1, span1) which is mapped to (F, v=1, il=1, span2) using previously updated statements
    // 2nd apply: detected span change for active statement (F, v=1, il=1): span2->span3
    // 3rd break: previously updated statements contains (F, v=1, il=1)->span3
    //            get active statements returns (F, v=3, il=3, span3) the active statement is up-to-date
    //
    internal readonly ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> NonRemappableRegions;

    /// <summary>
    /// Gets the capabilities of the runtime with respect to applying code changes.
    /// Retrieved lazily from <see cref="DebuggingSession.DebuggerService"/> since they are only needed when changes are detected in the solution.
    /// </summary>
    internal readonly AsyncLazy<EditAndContinueCapabilities> Capabilities;

    /// <summary>
    /// Map of base active statements.
    /// Calculated lazily based on info retrieved from <see cref="DebuggingSession.DebuggerService"/> since it is only needed when changes are detected in the solution.
    /// </summary>
    internal readonly AsyncLazy<ActiveStatementsMap> BaseActiveStatements;

    /// <summary>
    /// Cache of document EnC analyses.
    /// </summary>
    internal readonly EditAndContinueDocumentAnalysesCache Analyses;

    /// <summary>
    /// True for Edit and Continue edit sessions - when the application is in break state.
    /// False for Hot Reload edit sessions - when the application is running.
    /// </summary>
    internal readonly bool InBreakState;

    internal EditSession(
        DebuggingSession debuggingSession,
        ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> nonRemappableRegions,
        EditSessionTelemetry telemetry,
        AsyncLazy<ActiveStatementsMap>? lazyActiveStatementMap,
        bool inBreakState)
    {
        DebuggingSession = debuggingSession;
        NonRemappableRegions = nonRemappableRegions;
        Telemetry = telemetry;
        InBreakState = inBreakState;

        telemetry.SetBreakState(inBreakState);

        BaseActiveStatements = lazyActiveStatementMap ?? (inBreakState
            ? AsyncLazy.Create(static (self, cancellationToken) =>
                self.GetBaseActiveStatementsAsync(cancellationToken),
                arg: this)
            : AsyncLazy.Create(ActiveStatementsMap.Empty));

        Capabilities = AsyncLazy.Create(static (self, cancellationToken) =>
            self.GetCapabilitiesAsync(cancellationToken),
            arg: this);

        Analyses = new EditAndContinueDocumentAnalysesCache(BaseActiveStatements, Capabilities, debuggingSession.AnalysisLog);
    }

    public TraceLog Log
        => DebuggingSession.SessionLog;

    /// <summary>
    /// The compiler has various scenarios that will cause it to synthesize things that might not be covered
    /// by existing rude edits, but we still need to ensure the runtime supports them before we proceed.
    /// </summary>
    private async Task<Diagnostic?> GetUnsupportedChangesDiagnosticAsync(EmitDifferenceResult emitResult, CancellationToken cancellationToken)
    {
        Debug.Assert(emitResult.Success);
        Debug.Assert(emitResult.Baseline is not null);

        // if there were no changed types then there is nothing to check
        if (emitResult.ChangedTypes.Length == 0)
        {
            return null;
        }

        var capabilities = await Capabilities.GetValueAsync(cancellationToken).ConfigureAwait(false);
        if (!capabilities.HasFlag(EditAndContinueCapabilities.NewTypeDefinition))
        {
            // If the runtime doesn't support adding new types then we expect every row number for any type that is
            // emitted will be less than or equal to the number of rows in the original metadata.
            var highestEmittedTypeDefRow = emitResult.ChangedTypes.Max(t => MetadataTokens.GetRowNumber(t));
            var highestExistingTypeDefRow = emitResult.Baseline.OriginalMetadata.GetMetadataReader().GetTableRowCount(TableIndex.TypeDef);

            if (highestEmittedTypeDefRow > highestExistingTypeDefRow)
            {
                var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.AddingTypeRuntimeCapabilityRequired);
                return Diagnostic.Create(descriptor, Location.None);
            }
        }

        return null;
    }

    /// <summary>
    /// Errors to be reported when a project is updated but the corresponding module does not support EnC.
    /// </summary>
    /// <returns>Non-null diagnostic id if the module blocks EnC operation.</returns>
    public async Task<string?> ReportModuleDiagnosticsAsync(Guid mvid, Project oldProject, Project newProject, ImmutableArray<DocumentAnalysisResults> documentAnalyses, ArrayBuilder<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var availability = await DebuggingSession.DebuggerService.GetAvailabilityAsync(mvid, cancellationToken).ConfigureAwait(false);
        if (availability.Status is ManagedHotReloadAvailabilityStatus.ModuleNotLoaded or ManagedHotReloadAvailabilityStatus.Available)
        {
            return null;
        }

        var descriptor = EditAndContinueDiagnosticDescriptors.GetModuleDiagnosticDescriptor(availability.Status);
        var messageArgs = new[] { newProject.Name, availability.LocalizedMessage };

        await foreach (var location in CreateChangedLocationsAsync(oldProject, newProject, documentAnalyses, cancellationToken).ConfigureAwait(false))
        {
            diagnostics.Add(Diagnostic.Create(descriptor, location, messageArgs));
        }

        return descriptor.Id;
    }

    private static async IAsyncEnumerable<Location> CreateChangedLocationsAsync(Project oldProject, Project newProject, ImmutableArray<DocumentAnalysisResults> documentAnalyses, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var hasRemovedOrAddedDocument = false;
        foreach (var documentAnalysis in documentAnalyses)
        {
            if (!documentAnalysis.HasChanges)
            {
                continue;
            }

            var oldDocument = await oldProject.GetDocumentAsync(documentAnalysis.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            var newDocument = await newProject.GetDocumentAsync(documentAnalysis.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            if (oldDocument == null || newDocument == null)
            {
                hasRemovedOrAddedDocument = true;
                continue;
            }

            var oldText = await oldDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var newTree = await newDocument.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            // document location:
            yield return Location.Create(newTree, GetFirstLineDifferenceSpan(oldText, newText));
        }

        // project location:
        if (hasRemovedOrAddedDocument || documentAnalyses.IsEmpty)
        {
            yield return Location.None;
        }
    }

    private static TextSpan GetFirstLineDifferenceSpan(SourceText oldText, SourceText newText)
    {
        var oldLineCount = oldText.Lines.Count;
        var newLineCount = newText.Lines.Count;

        for (var i = 0; i < Math.Min(oldLineCount, newLineCount); i++)
        {
            var oldLineSpan = oldText.Lines[i].Span;
            var newLineSpan = newText.Lines[i].Span;
            if (oldLineSpan != newLineSpan || !oldText.GetSubText(oldLineSpan).ContentEquals(newText.GetSubText(newLineSpan)))
            {
                return newText.Lines[i].Span;
            }
        }

        return (oldLineCount == newLineCount) ? default :
               (newLineCount > oldLineCount) ? newText.Lines[oldLineCount].Span :
               TextSpan.FromBounds(newText.Lines[newLineCount - 1].End, newText.Lines[newLineCount - 1].EndIncludingLineBreak);
    }

    private async Task<EditAndContinueCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var capabilities = await DebuggingSession.DebuggerService.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            return EditAndContinueCapabilitiesParser.Parse(capabilities);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            return EditAndContinueCapabilities.Baseline;
        }
    }

    private async Task<ActiveStatementsMap> GetBaseActiveStatementsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Last committed solution reflects the state of the source that is in sync with the binaries that are loaded in the debuggee.
            var debugInfos = await DebuggingSession.DebuggerService.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false);
            return ActiveStatementsMap.Create(debugInfos, NonRemappableRegions);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            return ActiveStatementsMap.Empty;
        }
    }

    public static async ValueTask<bool> HasChangesAsync(Solution oldSolution, Solution newSolution, string sourceFilePath, CancellationToken cancellationToken)
    {
        // Note that this path look up does not work for source-generated files:
        var newDocumentIds = newSolution.GetDocumentIdsWithFilePath(sourceFilePath);
        if (newDocumentIds.IsEmpty)
        {
            // file does not exist or has been removed:
            return !oldSolution.GetDocumentIdsWithFilePath(sourceFilePath).IsEmpty;
        }

        // it suffices to check the content of one of the document if there are multiple linked ones:
        var documentId = newDocumentIds.First();
        var oldDocument = oldSolution.GetTextDocument(documentId);
        if (oldDocument == null)
        {
            // file has been added
            return true;
        }

        var newDocument = newSolution.GetRequiredTextDocument(documentId);
        return oldDocument != newDocument && !await ContentEqualsAsync(oldDocument, newDocument, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<bool> HasChangesAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
    {
        if (oldSolution == newSolution)
        {
            return false;
        }

        foreach (var newProject in newSolution.Projects)
        {
            if (!newProject.SupportsEditAndContinue())
            {
                continue;
            }

            var oldProject = oldSolution.GetProject(newProject.Id);
            if (oldProject == null || await HasDifferencesAsync(oldProject, newProject, differences: null, cancellationToken).ConfigureAwait(false))
            {
                // project added or has changes
                return true;
            }
        }

        foreach (var oldProject in oldSolution.Projects)
        {
            if (!oldProject.SupportsEditAndContinue())
            {
                continue;
            }

            var newProject = newSolution.GetProject(oldProject.Id);
            if (newProject == null)
            {
                // project removed
                return true;
            }
        }

        return false;
    }

    private static async ValueTask<bool> ContentEqualsAsync(TextDocument oldDocument, TextDocument newDocument, CancellationToken cancellationToken)
    {
        // Check if the currently observed document content has changed compared to the base document content.
        // This is an important optimization that aims to avoid IO while stepping in sources that have not changed.
        //
        // We may be comparing out-of-date committed document content but we only make a decision based on that content
        // if it matches the current content. If the current content is equal to baseline content that does not match
        // the debuggee then the workspace has not observed the change made to the file on disk since baseline was captured
        // (there had to be one as the content doesn't match). When we are about to apply changes it is ok to ignore this
        // document because the user does not see the change yet in the buffer (if the doc is open) and won't be confused
        // if it is not applied yet. The change will be applied later after it's observed by the workspace.
        var oldSource = await oldDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var newSource = await newDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        return oldSource.ContentEquals(newSource);
    }

    internal static async ValueTask<bool> HasDifferencesAsync(Project oldProject, Project newProject, ProjectDifferences? differences, CancellationToken cancellationToken)
    {
        if (!newProject.SupportsEditAndContinue())
        {
            return false;
        }

        if (oldProject.State == newProject.State)
        {
            return false;
        }

        if (AbstractEditAndContinueAnalyzer.EnableProjectLevelAnalysis && HasProjectLevelDifferences(oldProject, newProject, differences) && differences == null)
        {
            return true;
        }

        foreach (var documentId in newProject.State.DocumentStates.GetChangedStateIds(oldProject.State.DocumentStates, ignoreUnchangedContent: true))
        {
            var document = newProject.GetRequiredDocument(documentId);
            if (!document.State.SupportsEditAndContinue())
            {
                continue;
            }

            if (await ContentEqualsAsync(oldProject.GetRequiredDocument(documentId), document, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            if (differences == null)
            {
                return true;
            }

            differences.ChangedOrAddedDocuments.Add(document);
        }

        foreach (var documentId in newProject.State.DocumentStates.GetAddedStateIds(oldProject.State.DocumentStates))
        {
            var document = newProject.GetRequiredDocument(documentId);
            if (!document.State.SupportsEditAndContinue())
            {
                continue;
            }

            if (differences == null)
            {
                return true;
            }

            differences.ChangedOrAddedDocuments.Add(document);
        }

        foreach (var documentId in newProject.State.DocumentStates.GetRemovedStateIds(oldProject.State.DocumentStates))
        {
            var document = oldProject.GetRequiredDocument(documentId);
            if (!document.State.SupportsEditAndContinue())
            {
                continue;
            }

            if (differences == null)
            {
                return true;
            }

            differences.DeletedDocuments.Add(document);
        }

        // The following will check for any changes in non-generated document content (editorconfig, additional docs).
        // If we already have changes and we are collecting document differences, we don't need to check for these as
        // tey do not contribute directly to changed documents. They may trigger changes in source-generated documents though, so
        // if we are not collecting differences and no changes have been observed so far (if they were we would have returned above),
        // we need to check for changes in editorconfig and additional documents.

        if (differences?.Any() == true)
        {
            return true;
        }

        foreach (var documentId in newProject.State.AdditionalDocumentStates.GetChangedStateIds(oldProject.State.AdditionalDocumentStates, ignoreUnchangedContent: true))
        {
            var document = newProject.GetRequiredAdditionalDocument(documentId);
            if (!await ContentEqualsAsync(oldProject.GetRequiredAdditionalDocument(documentId), document, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        foreach (var documentId in newProject.State.AnalyzerConfigDocumentStates.GetChangedStateIds(oldProject.State.AnalyzerConfigDocumentStates, ignoreUnchangedContent: true))
        {
            var document = newProject.GetRequiredAnalyzerConfigDocument(documentId);
            if (!await ContentEqualsAsync(oldProject.GetRequiredAnalyzerConfigDocument(documentId), document, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        if (newProject.State.AdditionalDocumentStates.GetRemovedStateIds(oldProject.State.AdditionalDocumentStates).Any() ||
            newProject.State.AdditionalDocumentStates.GetAddedStateIds(oldProject.State.AdditionalDocumentStates).Any() ||
            newProject.State.AnalyzerConfigDocumentStates.GetRemovedStateIds(oldProject.State.AnalyzerConfigDocumentStates).Any() ||
            newProject.State.AnalyzerConfigDocumentStates.GetAddedStateIds(oldProject.State.AnalyzerConfigDocumentStates).Any())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Return true if projects might have differences in state other than document content that migth affect EnC.
    /// The checks need to be fast. May return true even if the changes don't actually affect the behavior.
    /// </summary>
    internal static bool HasProjectLevelDifferences(Project oldProject, Project newProject, ProjectDifferences? differences)
    {
        Debug.Assert(oldProject.CompilationOptions != null);
        Debug.Assert(newProject.CompilationOptions != null);

        if (oldProject.ParseOptions != newProject.ParseOptions ||
            HasDifferences(oldProject.CompilationOptions, newProject.CompilationOptions) ||
            oldProject.AssemblyName != newProject.AssemblyName)
        {
            if (differences != null)
            {
                differences.HasSettingChange = true;
            }
            else
            {
                return true;
            }
        }

        if (!oldProject.MetadataReferences.SequenceEqual(newProject.MetadataReferences) ||
            !oldProject.ProjectReferences.SequenceEqual(newProject.ProjectReferences))
        {
            if (differences != null)
            {
                differences.HasReferenceChange = true;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True if given compilation options differ in a way that might affect EnC.
    /// </summary>
    internal static bool HasDifferences(CompilationOptions oldOptions, CompilationOptions newOptions)
        => !oldOptions
            .WithSyntaxTreeOptionsProvider(newOptions.SyntaxTreeOptionsProvider)
            .WithStrongNameProvider(newOptions.StrongNameProvider)
            .WithXmlReferenceResolver(newOptions.XmlReferenceResolver)
            .Equals(newOptions);

    internal static async Task GetProjectDifferencesAsync(TraceLog log, Project? oldProject, Project newProject, ProjectDifferences documentDifferences, ArrayBuilder<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        documentDifferences.Clear();

        if (oldProject == null)
        {
            return;
        }

        if (!await HasDifferencesAsync(oldProject, newProject, documentDifferences, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var oldSourceGeneratedDocumentStates = await GetSourceGeneratedDocumentStatesAsync(log, oldProject, diagnostics, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var newSourceGeneratedDocumentStates = await GetSourceGeneratedDocumentStatesAsync(log, newProject, diagnostics, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var documentId in newSourceGeneratedDocumentStates.GetChangedStateIds(oldSourceGeneratedDocumentStates, ignoreUnchangedContent: true))
        {
            var newState = newSourceGeneratedDocumentStates.GetRequiredState(documentId);
            if (newState.Attributes.DesignTimeOnly)
            {
                continue;
            }

            documentDifferences.ChangedOrAddedDocuments.Add(newProject.GetOrCreateSourceGeneratedDocument(newState));
        }

        foreach (var documentId in newSourceGeneratedDocumentStates.GetAddedStateIds(oldSourceGeneratedDocumentStates))
        {
            var newState = newSourceGeneratedDocumentStates.GetRequiredState(documentId);
            if (newState.Attributes.DesignTimeOnly)
            {
                continue;
            }

            documentDifferences.ChangedOrAddedDocuments.Add(newProject.GetOrCreateSourceGeneratedDocument(newState));
        }

        foreach (var documentId in newSourceGeneratedDocumentStates.GetRemovedStateIds(oldSourceGeneratedDocumentStates))
        {
            var oldState = oldSourceGeneratedDocumentStates.GetRequiredState(documentId);
            if (oldState.Attributes.DesignTimeOnly)
            {
                continue;
            }

            documentDifferences.DeletedDocuments.Add(oldProject.GetOrCreateSourceGeneratedDocument(oldState));
        }
    }

    private static async ValueTask<TextDocumentStates<SourceGeneratedDocumentState>> GetSourceGeneratedDocumentStatesAsync(TraceLog log, Project project, ArrayBuilder<Diagnostic>? diagnostics, CancellationToken cancellationToken)
    {
        var generatorDiagnostics = await project.Solution.CompilationState.GetSourceGeneratorDiagnosticsAsync(project.State, cancellationToken).ConfigureAwait(false);

        diagnostics?.AddRange(generatorDiagnostics);

        if (diagnostics == null)
        {
            foreach (var generatorDiagnostic in generatorDiagnostics)
            {
                log.Write($"Source generator failed: {generatorDiagnostic}", LogMessageSeverity.Info);
            }
        }

        return await project.Solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(project.State, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Enumerates <see cref="DocumentId"/>s of changed (not added or removed) <see cref="Document"/>s (not additional nor analyzer config).
    /// </summary>
    internal static async IAsyncEnumerable<DocumentId> GetChangedDocumentsAsync(TraceLog log, Project oldProject, Project newProject, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Debug.Assert(oldProject.Id == newProject.Id);

        if (!newProject.SupportsEditAndContinue() || oldProject.State == newProject.State)
        {
            yield break;
        }

        foreach (var documentId in newProject.State.DocumentStates.GetChangedStateIds(oldProject.State.DocumentStates, ignoreUnchangedContent: true))
        {
            yield return documentId;
        }

        // Given the following assumptions:
        // - source generators are deterministic,
        // - metadata references and compilation options have not changed (TODO -- need to check),
        // - source documents have not changed,
        // - additional documents have not changed,
        // - analyzer config documents have not changed,
        // the outputs of source generators will not change.

        if (!newProject.State.DocumentStates.HasAnyStateChanges(oldProject.State.DocumentStates) &&
            !newProject.State.AdditionalDocumentStates.HasAnyStateChanges(oldProject.State.AdditionalDocumentStates) &&
            !newProject.State.AnalyzerConfigDocumentStates.HasAnyStateChanges(oldProject.State.AnalyzerConfigDocumentStates))
        {
            // Based on the above assumption there are no changes in source generated files.
            yield break;
        }

        var oldSourceGeneratedDocumentStates = await GetSourceGeneratedDocumentStatesAsync(log, oldProject, diagnostics: null, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var newSourceGeneratedDocumentStates = await GetSourceGeneratedDocumentStatesAsync(log, newProject, diagnostics: null, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var documentId in newSourceGeneratedDocumentStates.GetChangedStateIds(oldSourceGeneratedDocumentStates, ignoreUnchangedContent: true))
        {
            yield return documentId;
        }
    }

    private async Task<(ImmutableArray<DocumentAnalysisResults> results, bool hasOutOfSyncDocument)> AnalyzeProjectDifferencesAsync(
        Solution newSolution,
        ProjectDifferences differences,
        ActiveStatementSpanProvider newDocumentActiveStatementSpanProvider,
        ArrayBuilder<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<(Document? oldDocument, Document? newDocument)>.GetInstance(out var documents);
        var hasOutOfSyncDocument = false;

        foreach (var newDocument in differences.ChangedOrAddedDocuments)
        {
            var (oldDocument, oldDocumentState) = await DebuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(newDocument, cancellationToken, reloadOutOfSyncDocument: true).ConfigureAwait(false);
            switch (oldDocumentState)
            {
                case CommittedSolution.DocumentState.DesignTimeOnly:
                    break;

                case CommittedSolution.DocumentState.Indeterminate:
                    var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.UnableToReadSourceFileOrPdb);
                    diagnostics.Add(Diagnostic.Create(descriptor, Location.Create(newDocument.FilePath!, textSpan: default, lineSpan: default), [newDocument.FilePath]));
                    break;

                case CommittedSolution.DocumentState.OutOfSync:
                    // TODO: https://github.com/dotnet/roslyn/issues/78125
                    // consider reporting EditAndContinueErrorCode.DocumentIsOutOfSyncWithDebuggee warning if project doesn't specify SingleTargetBuildForStartupProjects property
                    hasOutOfSyncDocument = true;
                    break;

                case CommittedSolution.DocumentState.MatchesBuildOutput:
                    // Include the document regardless of whether the module it was built into has been loaded or not.
                    // If the module has been built it might get loaded later during the debugging session,
                    // at which point we apply all changes that have been made to the project so far.

                    documents.Add((oldDocument, newDocument));
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(oldDocumentState);
            }
        }

        foreach (var oldDocument in differences.DeletedDocuments)
        {
            documents.Add((oldDocument, newDocument: null));
        }

        // No need to report rude edits if project has any documents that are out of sync. No deltas will be emitted for such project.
        var analyses = hasOutOfSyncDocument
            ? []
            : await Analyses.GetDocumentAnalysesAsync(DebuggingSession.LastCommittedSolution, newSolution, documents, newDocumentActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);

        return (analyses, hasOutOfSyncDocument);
    }

    private static ProjectAnalysisSummary GetProjectAnalysisSummary(ImmutableArray<DocumentAnalysisResults> documentAnalyses)
    {
        var hasChanges = false;
        var hasSignificantValidChanges = false;

        foreach (var analysis in documentAnalyses)
        {
            // skip documents that actually were not changed:
            if (!analysis.HasChanges)
            {
                continue;
            }

            // rude edit detection wasn't completed due to errors that prevent us from analyzing the document:
            if (analysis.AnalysisBlocked || analysis.HasBlockingRudeEdits)
            {
                return ProjectAnalysisSummary.InvalidChanges;
            }

            hasChanges = true;
            hasSignificantValidChanges |= analysis.HasSignificantValidChanges;
        }

        if (!hasChanges)
        {
            // we get here if a document is closed and reopen without any actual change:
            return ProjectAnalysisSummary.NoChanges;
        }

        if (!hasSignificantValidChanges)
        {
            return ProjectAnalysisSummary.ValidInsignificantChanges;
        }

        return ProjectAnalysisSummary.ValidChanges;
    }

    private static bool HasProjectSettingsBlockingRudeEdits(Project oldProject, Project newProject, ArrayBuilder<Diagnostic> diagnostics)
    {
        Contract.ThrowIfFalse(oldProject.Language == newProject.Language);

        var analyzer = newProject.GetRequiredLanguageService<IEditAndContinueAnalyzer>();

        var hasBlockingRudeEdit = false;

        foreach (var diagnostic in analyzer.GetProjectSettingRudeEdits(oldProject, newProject))
        {
            hasBlockingRudeEdit |= diagnostic.Severity == DiagnosticSeverity.Error;
            diagnostics.Add(diagnostic);
        }

        return hasBlockingRudeEdit;
    }

    private static bool HasReferenceRudeEdits(ImmutableDictionary<string, OneOrMany<AssemblyIdentity>> oldReferencedAssemblies, Compilation newCompilation, ArrayBuilder<Diagnostic> projectDiagnostics)
    {
        var hasRudeEdit = false;

        foreach (var newReference in newCompilation.ReferencedAssemblyNames)
        {
            if (oldReferencedAssemblies.TryGetValue(newReference.Name, out var oldReferences))
            {
                if (oldReferences.Contains(newReference))
                {
                    // found exact match
                    continue;
                }

                hasRudeEdit = true;

                if (oldReferences.Count > 1)
                {
                    // For simplicity we disallow changing references to assembly references that don't have unique simple name.
                    // This case should be very rare in practice.

                    projectDiagnostics.Add(Diagnostic.Create(
                        EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ChangingMultiVersionReferences),
                        Location.None,
                        [newReference.Name, string.Join(", ", oldReferences.ToImmutable().Select(static r => $"'{r}'"))]));

                    break;
                }

                // Reference identity changed.

                projectDiagnostics.Add(Diagnostic.Create(
                    EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ChangingReference),
                    Location.None,
                    [oldReferences[0].ToString(), newReference.ToString()]));
            }
        }

        return hasRudeEdit;
    }

    private static bool HasAddedReference(Compilation oldCompilation, Compilation newCompilation)
    {
        using var pooledOldNames = SharedPools.StringIgnoreCaseHashSet.GetPooledObject();
        var oldNames = pooledOldNames.Object;
        Debug.Assert(oldNames.Comparer == AssemblyIdentityComparer.SimpleNameComparer);

        oldNames.AddRange(oldCompilation.ReferencedAssemblyNames.Select(static r => r.Name));
        return newCompilation.ReferencedAssemblyNames.Any(static (newReference, oldNames) => !oldNames.Contains(newReference.Name), oldNames);
    }

    internal static async ValueTask<ProjectChanges> GetProjectChangesAsync(
        ActiveStatementsMap baseActiveStatements,
        Compilation oldCompilation,
        Compilation newCompilation,
        Project oldProject,
        Project newProject,
        ImmutableArray<DocumentAnalysisResults> changedDocumentAnalyses,
        CancellationToken cancellationToken)
    {
        try
        {
            using var _1 = ArrayBuilder<SemanticEditInfo>.GetInstance(out var allEdits);
            using var _2 = ArrayBuilder<SequencePointUpdates>.GetInstance(out var allLineEdits);
            using var _3 = ArrayBuilder<DocumentActiveStatementChanges>.GetInstance(out var activeStatementsInChangedDocuments);

            var analyzer = newProject.Services.GetRequiredService<IEditAndContinueAnalyzer>();
            var requiredCapabilities = EditAndContinueCapabilities.None;

            foreach (var analysis in changedDocumentAnalyses)
            {
                if (!analysis.HasSignificantValidChanges)
                {
                    continue;
                }

                // Active statements are calculated if document changed and has no syntax errors:
                Contract.ThrowIfTrue(analysis.ActiveStatements.IsDefault);

                allEdits.AddRange(analysis.SemanticEdits);
                allLineEdits.AddRange(analysis.LineEdits);
                requiredCapabilities |= analysis.RequiredCapabilities;

                if (analysis.ActiveStatements.Length > 0)
                {
                    var oldDocument = await oldProject.GetDocumentAsync(analysis.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                    var oldActiveStatements = (oldDocument == null) ? [] :
                        await baseActiveStatements.GetOldActiveStatementsAsync(analyzer, oldDocument, cancellationToken).ConfigureAwait(false);

                    activeStatementsInChangedDocuments.Add(new(oldActiveStatements, analysis.ActiveStatements, analysis.ExceptionRegions));
                }
            }

            var (mergedEdits, addedSymbols) = await MergePartialEditsAsync(oldCompilation, newCompilation, oldProject, newProject, allEdits, cancellationToken).ConfigureAwait(false);

            return new ProjectChanges(
                mergedEdits,
                allLineEdits.ToImmutable(),
                addedSymbols,
                activeStatementsInChangedDocuments.ToImmutable(),
                requiredCapabilities);
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    internal static async ValueTask<(ImmutableArray<SemanticEdit> mergedEdits, ImmutableHashSet<ISymbol> addedSymbols)> MergePartialEditsAsync(
        Compilation oldCompilation,
        Compilation newCompilation,
        Project oldProject,
        Project newProject,
        IReadOnlyList<SemanticEditInfo> edits,
        CancellationToken cancellationToken)
    {
        using var _0 = ArrayBuilder<SemanticEdit>.GetInstance(edits.Count, out var mergedEditsBuilder);
        using var _1 = PooledHashSet<ISymbol>.GetInstance(out var addedSymbolsBuilder);
        using var _2 = ArrayBuilder<(ISymbol? oldSymbol, ISymbol? newSymbol)>.GetInstance(edits.Count, out var resolvedSymbols);

        ResolveSymbols(edits, oldCompilation, newCompilation, resolvedSymbols, cancellationToken);

        for (var i = 0; i < edits.Count; i++)
        {
            var edit = edits[i];

            if (edit.PartialType == null)
            {
                var (oldSymbol, newSymbol) = resolvedSymbols[i];

                if (edit.Kind == SemanticEditKind.Insert)
                {
                    Contract.ThrowIfNull(newSymbol);
                    addedSymbolsBuilder.Add(newSymbol);
                }

                mergedEditsBuilder.Add(new SemanticEdit(
                    edit.Kind,
                    oldSymbol: oldSymbol,
                    newSymbol: newSymbol,
                    syntaxMap: edit.SyntaxMaps.MatchingNodes,
                    runtimeRudeEdit: edit.SyntaxMaps.RuntimeRudeEdits));
            }
        }

        // no partial type merging needed:
        if (edits.Count == mergedEditsBuilder.Count)
        {
            return (mergedEdits: mergedEditsBuilder.ToImmutable(), addedSymbols: [.. addedSymbolsBuilder]);
        }

        // Calculate merged syntax map for each partial type symbol:

        var symbolKeyComparer = SymbolKey.GetComparer(ignoreCase: false, ignoreAssemblyKeys: true);
        var mergedUpdateEditSyntaxMaps = new Dictionary<SymbolKey, (Func<SyntaxNode, SyntaxNode?>? matchingNodes, Func<SyntaxNode, RuntimeRudeEdit?>? runtimeRudeEdits)>(symbolKeyComparer);

        var updatesByPartialType = edits
            .Where(static edit => edit is { PartialType: not null, Kind: SemanticEditKind.Update })
            .GroupBy(keySelector: static edit => edit.PartialType!.Value, symbolKeyComparer);

        foreach (var partialTypeEdits in updatesByPartialType)
        {
            Func<SyntaxNode, SyntaxNode?>? mergedMatchingNodes;
            Func<SyntaxNode, RuntimeRudeEdit?>? mergedRuntimeRudeEdits;

            if (partialTypeEdits.Any(static e => e.SyntaxMaps.HasMap))
            {
                var newMaps = partialTypeEdits.SelectAsArray(
                    predicate: static edit => edit.SyntaxMaps.HasMap,
                    selector: static edit => edit.SyntaxMaps);
#if DEBUG
                // All edits of constructors of the same partial class share the same syntax mapping function
                // that aggregates all syntax maps of member bodies (changed or unchanged) comprising the constructor's emitted body
                // (the constructor and any contributing member initializers) in the analyzed document (tree).
                // See AbstractEditAndContinueAnalyzer.AddConstructorEdits.
                foreach (var g in newMaps.GroupBy(m => m.NewTree))
                {
                    var first = g.First();
                    foreach (var m in g)
                    {
                        Debug.Assert(m.MatchingNodes == first.MatchingNodes);
                        Debug.Assert(m.RuntimeRudeEdits == first.RuntimeRudeEdits);
                    }
                }
#endif
                var treeMap = await GetPartialTypeDeclarationTreeMapAsync(
                    partialTypeEdits.Key,
                    newCompilation,
                    oldProject,
                    newProject,
                    cancellationToken).ConfigureAwait(false);

                mergedMatchingNodes = newNode =>
                {
                    var syntaxMapsForTree = newMaps.FirstOrDefault(static (m, newNode) => m.NewTree == newNode.SyntaxTree, newNode);
                    if (syntaxMapsForTree.NewTree != null)
                    {
                        Contract.ThrowIfNull(syntaxMapsForTree.MatchingNodes);
                        return syntaxMapsForTree.MatchingNodes(newNode);
                    }

                    // The node is in a syntax tree that may only differ in trivia that do not affect active statements.
                    // Otherwise, it would already be mapped via a syntax map above correspondign to the changed syntax tree.
                    return treeMap.TryGetValue(newNode.SyntaxTree, out var oldRoot)
                        ? oldRoot.FindCorrespondingNodeInEquivalentTree(newNode)
                        : null;
                };

                mergedRuntimeRudeEdits = node => newMaps.FirstOrDefault(static (m, node) => m.NewTree == node.SyntaxTree, node).RuntimeRudeEdits?.Invoke(node);
            }
            else
            {
                mergedMatchingNodes = null;
                mergedRuntimeRudeEdits = null;
            }

            mergedUpdateEditSyntaxMaps.Add(partialTypeEdits.Key, (mergedMatchingNodes, mergedRuntimeRudeEdits));
        }

        // Deduplicate edits based on their target symbol and use merged syntax map calculated above for a given partial type.

        using var _3 = PooledHashSet<ISymbol>.GetInstance(out var visitedSymbols);

        for (var i = 0; i < edits.Count; i++)
        {
            var edit = edits[i];

            if (edit.PartialType != null)
            {
                var (oldSymbol, newSymbol) = resolvedSymbols[i];
                if (visitedSymbols.Add(newSymbol ?? oldSymbol!))
                {
                    var syntaxMaps = (edit.Kind == SemanticEditKind.Update) ? mergedUpdateEditSyntaxMaps[edit.PartialType.Value] : default;
                    mergedEditsBuilder.Add(new SemanticEdit(edit.Kind, oldSymbol, newSymbol, syntaxMaps.matchingNodes, syntaxMaps.runtimeRudeEdits));
                }
            }
        }

        return (mergedEdits: mergedEditsBuilder.ToImmutable(), addedSymbols: [.. addedSymbolsBuilder]);
    }

    private static void ResolveSymbols(
        IReadOnlyList<SemanticEditInfo> edits,
        Compilation oldCompilation,
        Compilation newCompilation,
        ArrayBuilder<(ISymbol? oldSymbol, ISymbol? newSymbol)> resolvedSymbols,
        CancellationToken cancellationToken)
    {
        foreach (var edit in edits)
        {
            SymbolKeyResolution oldResolution;
            if (edit.Kind is SemanticEditKind.Update or SemanticEditKind.Delete)
            {
                oldResolution = edit.Symbol.Resolve(oldCompilation, cancellationToken: cancellationToken);
                Contract.ThrowIfNull(oldResolution.Symbol);
            }
            else
            {
                oldResolution = default;
            }

            SymbolKeyResolution newResolution;
            if (edit.Kind is SemanticEditKind.Update or SemanticEditKind.Insert or SemanticEditKind.Replace)
            {
                newResolution = edit.Symbol.Resolve(newCompilation, cancellationToken: cancellationToken);
                Contract.ThrowIfNull(newResolution.Symbol);
            }
            else if (edit.Kind == SemanticEditKind.Delete && edit.DeletedSymbolContainer is not null)
            {
                // For deletes, we use NewSymbol to reference the containing type of the deleted member
                newResolution = edit.DeletedSymbolContainer.Value.Resolve(newCompilation, cancellationToken: cancellationToken);
                Contract.ThrowIfNull(newResolution.Symbol);
            }
            else
            {
                newResolution = default;
            }

            resolvedSymbols.Add((oldResolution.Symbol, newResolution.Symbol));
        }
    }

    /// <summary>
    /// Maps all syntax trees containing partial declarations of the specified type in the new compilation
    /// to the corresponding syntax roots in the old compilation.
    /// </summary>
    private static async ValueTask<ImmutableDictionary<SyntaxTree, SyntaxNode>> GetPartialTypeDeclarationTreeMapAsync(
        SymbolKey typeKey,
        Compilation newCompilation,
        Project oldProject,
        Project newProject,
        CancellationToken cancellationToken)
    {
        var newType = typeKey.Resolve(newCompilation, cancellationToken: cancellationToken).Symbol;
        Contract.ThrowIfNull(newType);

        var map = ImmutableDictionary.CreateBuilder<SyntaxTree, SyntaxNode>();
        foreach (var newSyntaxRef in newType.DeclaringSyntaxReferences)
        {
            if (map.ContainsKey(newSyntaxRef.SyntaxTree))
            {
                continue;
            }

            var documentId = newProject.GetRequiredDocument(newSyntaxRef.SyntaxTree).Id;

            var oldDocument = await oldProject.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            if (oldDocument == null)
            {
                // The document didn't exist in the old project. This can happen if the document was added
                // and contains a partial declaration of an existing type.
                continue;
            }

            var oldRoot = await oldDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            map.Add(newSyntaxRef.SyntaxTree, oldRoot);
        }

        return map.ToImmutable();
    }

    public async ValueTask<SolutionUpdate> EmitSolutionUpdateAsync(
        Solution solution,
        ActiveStatementSpanProvider solutionActiveStatementSpanProvider,
        UpdateId updateId,
        ImmutableDictionary<ProjectId, RunningProjectOptions> runningProjects,
        CancellationToken cancellationToken)
    {
        var projectDiagnostics = ArrayBuilder<Diagnostic>.GetInstance();

        try
        {
            Log.Write($"Found {updateId.SessionId} potentially changed document(s) in project {updateId.Ordinal} '{solution.FilePath}'");

            using var _1 = ArrayBuilder<ManagedHotReloadUpdate>.GetInstance(out var deltas);
            using var _2 = ArrayBuilder<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)>.GetInstance(out var nonRemappableRegions);
            using var _3 = ArrayBuilder<ProjectBaseline>.GetInstance(out var newProjectBaselines);
            using var _4 = ArrayBuilder<ProjectId>.GetInstance(out var addedUnbuiltProjects);
            using var _5 = ArrayBuilder<ProjectId>.GetInstance(out var projectsToRedeploy);
            using var _6 = PooledDictionary<ProjectId, ArrayBuilder<Diagnostic>>.GetInstance(out var diagnosticBuilders);

            // Project differences for currently analyzed project. Reused and cleared.
            using var projectDifferences = new ProjectDifferences();

            // After all projects have been analyzed "true" value indicates changed document that is only included in stale projects.
            var changedDocumentsStaleness = new Dictionary<string, bool>(SolutionState.FilePathComparer);

            void UpdateChangedDocumentsStaleness(bool isStale)
            {
                foreach (var changedDocument in projectDifferences.ChangedOrAddedDocuments)
                {
                    var path = changedDocument.FilePath;

                    // Only documents that support EnC (have paths) are added to the list.
                    Contract.ThrowIfNull(path);

                    if (isStale)
                    {
                        _ = changedDocumentsStaleness.TryAdd(path, true);
                    }
                    else
                    {
                        changedDocumentsStaleness[path] = false;
                    }
                }
            }

            Diagnostic? syntaxError = null;
            ProjectAnalysisSummary? projectSummaryToReport = null;

            var oldSolution = DebuggingSession.LastCommittedSolution;
            var staleProjects = oldSolution.StaleProjects;

            var hasPersistentErrors = false;
            foreach (var newProject in solution.Projects)
            {
                try
                {
                    if (!newProject.SupportsEditAndContinue(Log))
                    {
                        continue;
                    }

                    var oldProject = oldSolution.GetProject(newProject.Id);
                    Debug.Assert(oldProject == null || oldProject.SupportsEditAndContinue());

                    await GetProjectDifferencesAsync(Log, oldProject, newProject, projectDifferences, projectDiagnostics, cancellationToken).ConfigureAwait(false);
                    projectDifferences.Log(Log, newProject);

                    if (projectDifferences.IsEmpty)
                    {
                        continue;
                    }

                    var (mvid, mvidReadError) = await DebuggingSession.GetProjectModuleIdAsync(newProject, cancellationToken).ConfigureAwait(false);

                    // We don't consider document changes in stale projects until they are rebuilt (removed from stale set).
                    if (staleProjects.TryGetValue(newProject.Id, out var staleModuleId))
                    {
                        // The module hasn't been rebuilt or we are unable to read the MVID -- keep treating the project as stale.
                        if (mvid == staleModuleId || mvidReadError != null)
                        {
                            Log.Write($"EnC state of {newProject.GetLogDisplay()} queried: project is stale");

                            // Track changed documents that are only included in stale or unbuilt projects:
                            UpdateChangedDocumentsStaleness(isStale: true);
                            continue;
                        }

                        staleProjects = staleProjects.Remove(newProject.Id);
                    }

                    if (mvidReadError != null)
                    {
                        // The error hasn't been reported by GetDocumentDiagnosticsAsync since it might have been intermittent.
                        // The MVID is required for emit so we consider the error permanent and report it here.
                        // Bail before analyzing documents as the analysis needs to read the PDB which will likely fail if we can't even read the MVID.
                        projectDiagnostics.Add(mvidReadError);
                        continue;
                    }

                    if (mvid == Guid.Empty)
                    {
                        // If the project has been added to the solution, ask the project system to build it.
                        if (oldProject == null)
                        {
                            Log.Write($"Project build requested for {newProject.GetLogDisplay()}");
                            addedUnbuiltProjects.Add(newProject.Id);
                        }
                        else
                        {
                            Log.Write($"Changes not applied to {newProject.GetLogDisplay()}: project not built");
                        }

                        // Track changed documents that are only included in stale or unbuilt projects:
                        UpdateChangedDocumentsStaleness(isStale: true);
                        continue;
                    }

                    if (oldProject == null)
                    {
                        continue;
                    }

                    // Ensure that all changed documents are in-sync. Once a document is in-sync it can't get out-of-sync.
                    // Therefore, results of further computations based on base snapshots of changed documents can't be invalidated by
                    // incoming events updating the content of out-of-sync documents.
                    //
                    // If in past we concluded that a document is out-of-sync, attempt to check one more time before we block apply.
                    // The source file content might have been updated since the last time we checked.
                    //
                    // TODO (investigate): https://github.com/dotnet/roslyn/issues/38866
                    // It is possible that the result of Rude Edit semantic analysis of an unchanged document will change if there
                    // another document is updated. If we encounter a significant case of this we should consider caching such a result per project,
                    // rather then per document. Also, we might be observing an older semantics if the document that is causing the change is out-of-sync --
                    // e.g. the binary was built with an overload C.M(object), but a generator updated class C to also contain C.M(string),
                    // which change we have not observed yet. Then call-sites of C.M in a changed document observed by the analysis will be seen as C.M(object)
                    // instead of the true C.M(string).

                    var (changedDocumentAnalyses, hasOutOfSyncChangedDocument) =
                        await AnalyzeProjectDifferencesAsync(solution, projectDifferences, solutionActiveStatementSpanProvider, projectDiagnostics, cancellationToken).ConfigureAwait(false);

                    if (hasOutOfSyncChangedDocument)
                    {
                        // The project is considered stale as long as it has at least one document that is out-of-sync.
                        // Treat the project the same as if it hasn't been built. We won't produce delta for it until it gets rebuilt.
                        Log.Write($"Changes not applied to {newProject.GetLogDisplay()}: binaries not up-to-date");

                        staleProjects = staleProjects.Add(newProject.Id, mvid);
                        UpdateChangedDocumentsStaleness(isStale: true);

                        continue;
                    }

                    UpdateChangedDocumentsStaleness(isStale: false);

                    foreach (var changedDocumentAnalysis in changedDocumentAnalyses)
                    {
                        if (changedDocumentAnalysis.SyntaxError != null)
                        {
                            // only remember the first syntax error we encounter:
                            syntaxError ??= changedDocumentAnalysis.SyntaxError;
                            hasPersistentErrors = true;

                            Log.Write($"Changed document '{changedDocumentAnalysis.FilePath}' has syntax error: {changedDocumentAnalysis.SyntaxError}");
                        }
                        else if (changedDocumentAnalysis.HasChanges)
                        {
                            Log.Write($"Document changed, added, or deleted: '{changedDocumentAnalysis.FilePath}'");
                        }

                        Telemetry.LogAnalysisTime(changedDocumentAnalysis.ElapsedTime);
                    }

                    var projectSummary = GetProjectAnalysisSummary(changedDocumentAnalyses);

                    if (AbstractEditAndContinueAnalyzer.EnableProjectLevelAnalysis && HasProjectSettingsBlockingRudeEdits(oldProject, newProject, projectDiagnostics))
                    {
                        // If the project settings have changed and the change is a rude edit,
                        // block applying the changes even if there no other changes to the project documents.
                        // This is to avoid advancing the solution snapshot to an inconsistent state.
                        projectSummary = ProjectAnalysisSummary.InvalidChanges;
                    }

                    projectSummaryToReport = projectSummary;
                    Log.Write($"Project summary for {newProject.GetLogDisplay()}: {projectSummary}");

                    // Unsupported changes in referenced assemblies will be reported below.
                    if (projectSummary is ProjectAnalysisSummary.NoChanges or ProjectAnalysisSummary.ValidInsignificantChanges &&
                        !projectDifferences.HasReferenceChange)
                    {
                        continue;
                    }

                    // The capability of a module to apply edits may change during edit session if the user attaches debugger to 
                    // an additional process that doesn't support EnC (or detaches from such process). Before we apply edits 
                    // we need to check with the debugger.
                    var moduleBlockingDiagnosticId = await ReportModuleDiagnosticsAsync(mvid, oldProject, newProject, changedDocumentAnalyses, projectDiagnostics, cancellationToken).ConfigureAwait(false);

                    // Report rude edit diagnostics - these can be blocking (errors) or non-blocking (warnings):
                    foreach (var analysis in changedDocumentAnalyses)
                    {
                        if (!analysis.RudeEdits.IsEmpty)
                        {
                            var document = await solution.GetDocumentAsync(analysis.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                            var tree = (document != null) ? await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false) : null;

                            foreach (var rudeEdit in analysis.RudeEdits)
                            {
                                projectDiagnostics.Add(rudeEdit.ToDiagnostic(tree));
                            }

                            Telemetry.LogRudeEditDiagnostics(analysis.RudeEdits, newProject.State.Attributes.TelemetryId);
                        }
                    }

                    if (projectSummary == ProjectAnalysisSummary.InvalidChanges)
                    {
                        // Write document changes to log directory so that we can inspect them for rude edits:
                        await LogDocumentChangesAsync(generation: null, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (moduleBlockingDiagnosticId != null)
                    {
                        continue;
                    }

                    var oldCompilation = await oldProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfNull(oldCompilation);

                    // Report diagnosics even when the module is never going to be loaded (e.g. in multi-targeting scenario, where only one framework being debugged).
                    // This is consistent with reporting compilation errors - the IDE reports them for all TFMs regardless of what framework the app is running on.
                    var projectBaselines = DebuggingSession.GetOrCreateEmitBaselines(mvid, oldProject, oldCompilation, projectDiagnostics, out var baselineAccessLock);
                    if (projectBaselines.IsEmpty)
                    {
                        continue;
                    }

                    var newCompilation = await newProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfNull(newCompilation);

                    // Compare referenced assemblies against the baseline. The runtime does not provide means to replace already loaded assemblies
                    // and therefore the versions of referenced assemblies in the new compilation can't change from the baseline.
                    if (projectBaselines.Any(baseline => HasReferenceRudeEdits(baseline.InitiallyReferencedAssemblies, newCompilation, projectDiagnostics)))
                    {
                        continue;
                    }

                    // If the project references new dependencies, the host needs to invoke ReferenceCopyLocalPathsOutputGroup target on this project
                    // to deploy these dependencies to the projects output directory. The deployment shouldn't overwrite existing files.
                    // It should only happen if the project has no rude edits (especially not rude edits related to references) -- we bailed above if so.
                    if (HasAddedReference(oldCompilation, newCompilation))
                    {
                        projectsToRedeploy.Add(newProject.Id);
                    }

                    if (projectSummary is ProjectAnalysisSummary.NoChanges or ProjectAnalysisSummary.ValidInsignificantChanges)
                    {
                        continue;
                    }

                    Log.Write($"Emitting update of {newProject.GetLogDisplay()}");

                    await LogDocumentChangesAsync(projectBaselines.First().Generation + 1, cancellationToken).ConfigureAwait(false);

                    var oldActiveStatementsMap = await BaseActiveStatements.GetValueAsync(cancellationToken).ConfigureAwait(false);
                    var projectChanges = await GetProjectChangesAsync(oldActiveStatementsMap, oldCompilation, newCompilation, oldProject, newProject, changedDocumentAnalyses, cancellationToken).ConfigureAwait(false);

                    // The compiler only uses this predicate to determine if CS7101: "Member 'X' added during the current debug session
                    // can only be accessed from within its declaring assembly 'Lib'" should be reported. 
                    // Prior to .NET 8 Preview 4 the runtime failed to apply such edits.
                    // This was fixed in Preview 4 along with support for generics. If we see a generic capability we can disable reporting
                    // this compiler error. Otherwise, we leave the check as is in order to detect at least some runtime failures on .NET Framework.
                    // Note that the analysis in the compiler detecting the circumstances under which the runtime fails
                    // to apply the change has both false positives (flagged generic updates that shouldn't be flagged) and negatives
                    // (didn't flag cases like https://github.com/dotnet/roslyn/issues/68293).
                    var capabilities = await Capabilities.GetValueAsync(cancellationToken).ConfigureAwait(false);
                    var requiredCapabilities = projectChanges.RequiredCapabilities.ToStringArray();

                    var isAddedSymbolPredicate = capabilities.HasFlag(EditAndContinueCapabilities.GenericAddMethodToExistingType) ?
                        static _ => false : (Func<ISymbol, bool>)projectChanges.AddedSymbols.Contains;

                    foreach (var projectBaseline in projectBaselines)
                    {
                        using var pdbStream = SerializableBytes.CreateWritableStream();
                        using var metadataStream = SerializableBytes.CreateWritableStream();
                        using var ilStream = SerializableBytes.CreateWritableStream();

                        EmitDifferenceResult emitResult;

                        // The lock protects underlying baseline readers from being disposed while emitting delta.
                        // If the lock is disposed at this point the session has been incorrectly disposed while operations on it are in progress.
                        using (baselineAccessLock.DisposableRead())
                        {
                            DebuggingSession.ThrowIfDisposed();

                            var emitDifferenceTimer = SharedStopwatch.StartNew();

                            emitResult = newCompilation.EmitDifference(
                                projectBaseline.EmitBaseline,
                                projectChanges.SemanticEdits,
                                isAddedSymbolPredicate,
                                metadataStream,
                                ilStream,
                                pdbStream,
                                new EmitDifferenceOptions()
                                {
                                    EmitFieldRva = capabilities.HasFlag(EditAndContinueCapabilities.AddFieldRva),
                                    MethodImplEntriesSupported = capabilities.HasFlag(EditAndContinueCapabilities.AddExplicitInterfaceImplementation)
                                },
                                cancellationToken);

                            Telemetry.LogEmitDifferenceTime(emitDifferenceTimer.Elapsed);
                        }

                        // TODO: https://github.com/dotnet/roslyn/issues/36061
                        // We should only report diagnostics from emit phase.
                        // Syntax and semantic diagnostics are already reported by the diagnostic analyzer.
                        // Currently we do not have means to distinguish between diagnostics reported from compilation and emit phases.
                        // Querying diagnostics of the entire compilation or just the updated files migth be slow.
                        // In fact, it is desirable to allow emitting deltas for symbols affected by the change while allowing untouched
                        // method bodies to have errors.
                        if (!emitResult.Diagnostics.IsEmpty)
                        {
                            projectDiagnostics.AddRange(emitResult.Diagnostics);
                        }

                        if (!emitResult.Success)
                        {
                            hasPersistentErrors = true;

                            // Stop emitting deltas, we will discard the updates emitted so far.
                            // Persistent errors need to be fixed before we attempt rebuilding the projects.
                            // The baseline solution snapshot will not be moved forward and next call to
                            // EmitSolutionUpdatesAsync will calculate changes for all updated projects again.
                            break;
                        }

                        Contract.ThrowIfNull(emitResult.Baseline);

                        var unsupportedChangesDiagnostic = await GetUnsupportedChangesDiagnosticAsync(emitResult, cancellationToken).ConfigureAwait(false);
                        if (unsupportedChangesDiagnostic is not null)
                        {
                            projectDiagnostics.Add(unsupportedChangesDiagnostic);
                            continue;
                        }

                        var updatedMethodTokens = emitResult.UpdatedMethods.SelectAsArray(h => MetadataTokens.GetToken(h));
                        var changedTypeTokens = emitResult.ChangedTypes.SelectAsArray(h => MetadataTokens.GetToken(h));

                        // Determine all active statements whose span changed and exception region span deltas.
                        GetActiveStatementAndExceptionRegionSpans(
                            projectBaseline.ModuleId,
                            oldActiveStatementsMap,
                            updatedMethodTokens,
                            NonRemappableRegions,
                            projectChanges.ActiveStatementChanges,
                            out var activeStatementsInUpdatedMethods,
                            out var moduleNonRemappableRegions,
                            out var exceptionRegionUpdates);

                        var delta = new ManagedHotReloadUpdate(
                            projectBaseline.ModuleId,
                            newCompilation.AssemblyName ?? newProject.Name, // used for display in debugger diagnostics
                            newProject.Id,
                            ilStream.ToImmutableArray(),
                            metadataStream.ToImmutableArray(),
                            pdbStream.ToImmutableArray(),
                            changedTypeTokens,
                            requiredCapabilities,
                            updatedMethodTokens,
                            projectChanges.LineChanges,
                            activeStatementsInUpdatedMethods,
                            exceptionRegionUpdates);

                        deltas.Add(delta);

                        nonRemappableRegions.Add((mvid, moduleNonRemappableRegions));
                        newProjectBaselines.Add(new ProjectBaseline(mvid, projectBaseline.ProjectId, emitResult.Baseline, projectBaseline.InitiallyReferencedAssemblies, projectBaseline.Generation + 1));

                        var fileLog = Log.FileLog;
                        if (fileLog != null)
                        {
                            await LogDeltaFilesAsync(fileLog, delta, projectBaseline.Generation, oldProject, newProject, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    async ValueTask LogDocumentChangesAsync(int? generation, CancellationToken cancellationToken)
                    {
                        var fileLog = Log.FileLog;
                        if (fileLog != null)
                        {
                            foreach (var changedDocumentAnalysis in changedDocumentAnalyses)
                            {
                                if (changedDocumentAnalysis.HasChanges)
                                {
                                    var oldDocument = await oldProject.GetDocumentAsync(changedDocumentAnalysis.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                                    var newDocument = await newProject.GetDocumentAsync(changedDocumentAnalysis.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                                    await fileLog.WriteDocumentChangeAsync(oldDocument, newDocument, updateId, generation, cancellationToken).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (projectSummaryToReport.HasValue || !projectDiagnostics.IsEmpty)
                    {
                        Telemetry.LogProjectAnalysisSummary(projectSummaryToReport, newProject.State.ProjectInfo.Attributes.TelemetryId, projectDiagnostics);
                    }

                    if (!projectDiagnostics.IsEmpty)
                    {
                        diagnosticBuilders.Add(newProject.Id, projectDiagnostics);
                        projectDiagnostics = ArrayBuilder<Diagnostic>.GetInstance();
                    }
                }
            }

            // Report stale document updates.
            // We report a warning when a changed/added document is only included in (linked to) stale projects.

            foreach (var (documentPath, isStale) in changedDocumentsStaleness)
            {
                if (isStale)
                {
                    foreach (var documentId in solution.GetDocumentIdsWithFilePath(documentPath))
                    {
                        var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.UpdatingDocumentInStaleProject);
                        var diagnostic = Diagnostic.Create(descriptor, Location.Create(documentPath, textSpan: default, lineSpan: default), [documentPath]);
                        diagnosticBuilders.MultiAdd(documentId.ProjectId, diagnostic);
                    }
                }
            }

            var diagnostics = diagnosticBuilders.SelectAsArray(entry => new ProjectDiagnostics(entry.Key, entry.Value.ToImmutableAndFree()));

            Telemetry.LogRuntimeCapabilities(await Capabilities.GetValueAsync(cancellationToken).ConfigureAwait(false));

            if (syntaxError != null)
            {
                Telemetry.LogSyntaxError();
            }

            if (hasPersistentErrors)
            {
                return SolutionUpdate.Empty(diagnostics, syntaxError, staleProjects, ModuleUpdateStatus.Blocked);
            }

            // syntax error is a persistent error
            Contract.ThrowIfTrue(syntaxError != null);

            var updates = deltas.ToImmutable();

            EmitSolutionUpdateResults.GetProjectsToRebuildAndRestart(
                solution,
                updates,
                diagnostics,
                addedUnbuiltProjects,
                runningProjects,
                out var projectsToRestart,
                out var projectsToRebuild);

            var moduleUpdates = new ModuleUpdates(deltas.IsEmpty && projectsToRebuild.IsEmpty ? ModuleUpdateStatus.None : ModuleUpdateStatus.Ready, updates);

            return new SolutionUpdate(
                moduleUpdates,
                staleProjects,
                nonRemappableRegions.ToImmutable(),
                newProjectBaselines.ToImmutable(),
                diagnostics,
                syntaxError: null,
                projectsToRestart,
                projectsToRebuild,
                projectsToRedeploy.ToImmutable());
        }
        catch (Exception e) when (LogException(e) && FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
        finally
        {
            projectDiagnostics.Free();
        }

        bool LogException(Exception e)
        {
            Log.Write($"Exception while emitting update: {e}", LogMessageSeverity.Error);
            return true;
        }
    }

    private async ValueTask LogDeltaFilesAsync(TraceLog.FileLogger log, ManagedHotReloadUpdate delta, int baselineGeneration, Project oldProject, Project newProject, CancellationToken cancellationToken)
    {
        var sessionId = DebuggingSession.Id;

        if (baselineGeneration == 0)
        {
            var oldCompilationOutputs = DebuggingSession.GetCompilationOutputs(oldProject);

            await log.WriteAsync(
                async (stream, cancellationToken) => await oldCompilationOutputs.TryCopyAssemblyToAsync(stream, cancellationToken).ConfigureAwait(false),
                sessionId,
                newProject.Name,
                PathUtilities.GetFileName(oldCompilationOutputs.AssemblyDisplayPath) ?? oldProject.Name + ".dll",
                cancellationToken).ConfigureAwait(false);

            await log.WriteAsync(
                async (stream, cancellationToken) => await oldCompilationOutputs.TryCopyPdbToAsync(stream, cancellationToken).ConfigureAwait(false),
                sessionId,
                newProject.Name,
                PathUtilities.GetFileName(oldCompilationOutputs.PdbDisplayPath) ?? oldProject.Name + ".pdb",
                cancellationToken).ConfigureAwait(false);
        }

        var generation = baselineGeneration + 1;
        log.Write(sessionId, delta.ILDelta, newProject.Name, generation + ".il");
        log.Write(sessionId, delta.MetadataDelta, newProject.Name, generation + ".meta");
        log.Write(sessionId, delta.PdbDelta, newProject.Name, generation + ".pdb");
    }

    // internal for testing
    internal static void GetActiveStatementAndExceptionRegionSpans(
        Guid moduleId,
        ActiveStatementsMap oldActiveStatementMap,
        ImmutableArray<int> updatedMethodTokens,
        ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> previousNonRemappableRegions,
        ImmutableArray<DocumentActiveStatementChanges> activeStatementsInChangedDocuments,
        out ImmutableArray<ManagedActiveStatementUpdate> activeStatementsInUpdatedMethods,
        out ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)> nonRemappableRegions,
        out ImmutableArray<ManagedExceptionRegionUpdate> exceptionRegionUpdates)
    {
        using var _1 = PooledDictionary<(ManagedModuleMethodId MethodId, SourceFileSpan BaseSpan), SourceFileSpan>.GetInstance(out var changedNonRemappableSpans);
        var activeStatementsInUpdatedMethodsBuilder = ArrayBuilder<ManagedActiveStatementUpdate>.GetInstance();
        var nonRemappableRegionsBuilder = ArrayBuilder<(ManagedModuleMethodId Method, NonRemappableRegion Region)>.GetInstance();

        // Process active statements and their exception regions in changed documents of this project/module:
        foreach (var (oldActiveStatements, newActiveStatements, newExceptionRegions) in activeStatementsInChangedDocuments)
        {
            Debug.Assert(oldActiveStatements.Length == newExceptionRegions.Length);
            Debug.Assert(newActiveStatements.Length == newExceptionRegions.Length);

            for (var i = 0; i < newActiveStatements.Length; i++)
            {
                var (_, oldActiveStatement, oldActiveStatementExceptionRegions) = oldActiveStatements[i];
                var newActiveStatement = newActiveStatements[i];
                var newActiveStatementExceptionRegions = newExceptionRegions[i];

                var instructionId = newActiveStatement.InstructionId;
                var methodId = instructionId.Method.Method;

                var isMethodUpdated = updatedMethodTokens.Contains(methodId.Token);
                if (isMethodUpdated)
                {
                    activeStatementsInUpdatedMethodsBuilder.Add(new ManagedActiveStatementUpdate(methodId, instructionId.ILOffset, newActiveStatement.Span.ToSourceSpan()));
                }

                Debug.Assert(!oldActiveStatement.IsStale);

                // Adds a region with specified PDB spans.
                void AddNonRemappableRegion(SourceFileSpan oldSpan, SourceFileSpan newSpan, bool isExceptionRegion)
                {
                    // TODO: Remove comparer, the path should match exactly. Workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1830914.
                    Debug.Assert(string.Equals(oldSpan.Path, newSpan.Path,
                        EditAndContinueMethodDebugInfoReader.IgnoreCaseWhenComparingDocumentNames ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));

                    // The up-to-date flag is copied when new active statement is created from the corresponding old one.
                    Debug.Assert(oldActiveStatement.IsMethodUpToDate == newActiveStatement.IsMethodUpToDate);

                    if (oldActiveStatement.IsMethodUpToDate)
                    {
                        // Start tracking non-remappable regions for active statements in methods that were up-to-date
                        // when break state was entered and now being updated (regardless of whether the active span changed or not).
                        if (isMethodUpdated)
                        {
                            nonRemappableRegionsBuilder.Add((methodId, new NonRemappableRegion(oldSpan, newSpan, isExceptionRegion)));
                        }
                        else if (!isExceptionRegion)
                        {
                            // If the method has been up-to-date and it is not updated now then either the active statement span has not changed,
                            // or the entire method containing it moved. In neither case do we need to start tracking non-remapable region
                            // for the active statement since movement of whole method bodies (if any) is handled only on PDB level without
                            // triggering any remapping on the IL level.
                            //
                            // That said, we still add a non-remappable region for this active statement, so that we know in future sessions
                            // that this active statement existed and its span has not changed. We don't report these regions to the debugger,
                            // but we use them to map active statement spans to the baseline snapshots of following edit sessions.
                            nonRemappableRegionsBuilder.Add((methodId, new NonRemappableRegion(oldSpan, oldSpan, isExceptionRegion: false)));
                        }
                    }
                    else if (oldSpan.Span != newSpan.Span)
                    {
                        // The method is not up-to-date hence we might have a previous non-remappable span mapping that needs to be brought forward to the new snapshot.
                        changedNonRemappableSpans[(methodId, oldSpan)] = newSpan;
                    }
                }

                AddNonRemappableRegion(oldActiveStatement.FileSpan, newActiveStatement.FileSpan, isExceptionRegion: false);

                // The spans of the exception regions are known (non-default) for active statements in changed documents
                // as we ensured earlier that all changed documents are in-sync.

                for (var j = 0; j < oldActiveStatementExceptionRegions.Spans.Length; j++)
                {
                    AddNonRemappableRegion(oldActiveStatementExceptionRegions.Spans[j], newActiveStatementExceptionRegions[j], isExceptionRegion: true);
                }
            }
        }

        activeStatementsInUpdatedMethods = activeStatementsInUpdatedMethodsBuilder.ToImmutableAndFree();

        // Gather all active method instances contained in this project/module that are not up-to-date:
        using var _2 = PooledHashSet<ManagedModuleMethodId>.GetInstance(out var unremappedActiveMethods);
        foreach (var (instruction, baseActiveStatement) in oldActiveStatementMap.InstructionMap)
        {
            if (moduleId == instruction.Method.Module && !baseActiveStatement.IsMethodUpToDate)
            {
                unremappedActiveMethods.Add(instruction.Method.Method);
            }
        }

        // Update previously calculated non-remappable region mappings.
        // These map to the old snapshot and we need them to map to the new snapshot, which will be the baseline for the next session.
        if (unremappedActiveMethods.Count > 0)
        {
            foreach (var (methodInstance, regionsInMethod) in previousNonRemappableRegions)
            {
                // Skip non-remappable regions that belong to method instances that are from a different module.
                if (methodInstance.Module != moduleId)
                {
                    continue;
                }

                // Skip no longer active methods - all active statements in these method instances have been remapped to newer versions.
                // New active statement can't appear in a stale method instance since such instance can't be invoked.
                if (!unremappedActiveMethods.Contains(methodInstance.Method))
                {
                    continue;
                }

                foreach (var region in regionsInMethod)
                {
                    // We have calculated changes against a base snapshot (last break state):
                    var baseSpan = region.NewSpan;

                    NonRemappableRegion newRegion;
                    if (changedNonRemappableSpans.TryGetValue((methodInstance.Method, baseSpan), out var newSpan))
                    {
                        // all spans must be of the same size:
                        Debug.Assert(newSpan.Span.End.Line - newSpan.Span.Start.Line == baseSpan.Span.End.Line - baseSpan.Span.Start.Line);
                        Debug.Assert(region.OldSpan.Span.End.Line - region.OldSpan.Span.Start.Line == baseSpan.Span.End.Line - baseSpan.Span.Start.Line);
                        Debug.Assert(newSpan.Path == region.OldSpan.Path);

                        newRegion = region.WithNewSpan(newSpan);
                    }
                    else
                    {
                        newRegion = region;
                    }

                    nonRemappableRegionsBuilder.Add((methodInstance.Method, newRegion));
                }
            }
        }

        nonRemappableRegions = nonRemappableRegionsBuilder.ToImmutableAndFree();

        // Note: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1319289
        //
        // The update should include the file name, otherwise it is not possible for the debugger to find
        // the right IL span of the exception handler in case when multiple handlers in the same method
        // have the same mapped span but different mapped file name:
        //
        //   try { active statement }
        //   #line 20 "bar"
        //   catch (IOException) { }
        //   #line 20 "baz"
        //   catch (Exception) { }
        //
        // The range span in exception region updates is the new span. Deltas are inverse.
        //   old = new + delta
        //   new = old – delta
        exceptionRegionUpdates = nonRemappableRegions.SelectAsArray(
            r => r.Region.IsExceptionRegion,
            r => new ManagedExceptionRegionUpdate(
                r.Method,
                -r.Region.OldSpan.Span.GetLineDelta(r.Region.NewSpan.Span),
                r.Region.NewSpan.Span.ToSourceSpan()));
    }
}
