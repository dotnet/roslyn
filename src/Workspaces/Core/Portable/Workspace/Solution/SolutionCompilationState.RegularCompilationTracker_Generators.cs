// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.SourceGeneratorTelemetry;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed partial class SolutionCompilationState
{
    private sealed partial class RegularCompilationTracker : ICompilationTracker
    {
        private async Task<(Compilation compilationWithGeneratedFiles, CompilationTrackerGeneratorInfo nextGeneratorInfo)> AddExistingOrComputeNewGeneratorInfoAsync(
            CreationPolicy creationPolicy,
            SolutionCompilationState compilationState,
            Compilation compilationWithoutGeneratedFiles,
            CompilationTrackerGeneratorInfo generatorInfo,
            Compilation? compilationWithStaleGeneratedTrees,
            CancellationToken cancellationToken)
        {
            var canSkipRunningGenerators = await CanSkipRunningGeneratorsAsync(creationPolicy, compilationState, cancellationToken).ConfigureAwait(false);
            if (canSkipRunningGenerators)
            {
                // We're either frozen, or we only want required generators and know that there aren't any to run, so we
                // do not want to go through the expensive cost of running generators.  Instead, we just use whatever
                // prior generated docs we have.
                var generatedSyntaxTrees = await generatorInfo.Documents.States.Values.SelectAsArrayAsync(
                    static (state, cancellationToken) => state.GetSyntaxTreeAsync(cancellationToken), cancellationToken).ConfigureAwait(false);

                var compilationWithGeneratedFiles = compilationWithoutGeneratedFiles.AddSyntaxTrees(generatedSyntaxTrees);

                // Return the old generator info as is.
                return (compilationWithGeneratedFiles, generatorInfo);
            }
            else
            {
                // First try to compute the SG docs in the remote process (if we're the host process), syncing the results
                // back over to us to ensure that both processes are in total agreement about the SG docs and their
                // contents.
                var result = await TryComputeNewGeneratorInfoInRemoteProcessAsync(
                    compilationState, compilationWithoutGeneratedFiles, generatorInfo.Documents, compilationWithStaleGeneratedTrees, cancellationToken).ConfigureAwait(false);
                if (result.HasValue)
                {
                    // Since we ran the SG work out of process, we could not have created or modified the driver passed in.
                    // Just return `null` for the driver as there's nothing to track for it on the host side.
                    return (result.Value.compilationWithGeneratedFiles, new(result.Value.generatedDocuments, Driver: null));
                }

                // If that failed (OOP crash, or we are the OOP process ourselves), then generate the SG docs locally.
                var (compilationWithGeneratedFiles, nextGeneratedDocuments, nextGeneratorDriver) = await ComputeNewGeneratorInfoInCurrentProcessAsync(
                    compilationState,
                    compilationWithoutGeneratedFiles,
                    generatorInfo.Documents,
                    generatorInfo.Driver,
                    compilationWithStaleGeneratedTrees,
                    creationPolicy.GeneratedDocumentCreationPolicy,
                    cancellationToken).ConfigureAwait(false);
                return (compilationWithGeneratedFiles, new(nextGeneratedDocuments, nextGeneratorDriver));
            }

            async ValueTask<bool> CanSkipRunningGeneratorsAsync(CreationPolicy creationPolicy, SolutionCompilationState compilationState, CancellationToken cancellationToken)
            {
                // if we don't want to create generated documents, we can skip
                if (creationPolicy.GeneratedDocumentCreationPolicy is GeneratedDocumentCreationPolicy.DoNotCreate)
                    return true;

                // if we only want required documents, we can skip if we don't have any required generators
                if (creationPolicy.GeneratedDocumentCreationPolicy is GeneratedDocumentCreationPolicy.CreateOnlyRequired)
                {
                    var hasRequiredGenerators = await HasRequiredGeneratorsAsync(compilationState, cancellationToken).ConfigureAwait(false);
                    return !hasRequiredGenerators;
                }

                // we need to run generators
                return false;
            }
        }

        private async Task<bool> HasRequiredGeneratorsAsync(SolutionCompilationState compilationState, CancellationToken cancellationToken)
        {
            var presence = await compilationState.GetProjectGeneratorPresenceAsync(ProjectState.Id, cancellationToken).ConfigureAwait(false);
            return presence is SourceGeneratorPresence.ContainsRequiredSourceGenerators;
        }

        private async Task<(Compilation compilationWithGeneratedFiles, TextDocumentStates<SourceGeneratedDocumentState> generatedDocuments)?> TryComputeNewGeneratorInfoInRemoteProcessAsync(
            SolutionCompilationState compilationState,
            Compilation compilationWithoutGeneratedFiles,
            TextDocumentStates<SourceGeneratedDocumentState> oldGeneratedDocuments,
            Compilation? compilationWithStaleGeneratedTrees,
            CancellationToken cancellationToken)
        {
            var solution = compilationState.SolutionState;
            var projectId = this.ProjectState.Id;

            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client is null)
                return null;

            // We're going to be making multiple calls over to OOP.  No point in resyncing data multiple times.  Keep a
            // single connection, and keep this solution instance alive (and synced) on both sides of the connection
            // throughout the calls.
            //
            // CRITICAL: We pass the "compilationState+projectId" as the context for the connection.  All subsequent
            // uses of this connection must do that aas well. This ensures that all calls will see the same exact
            // snapshot on the OOP side, which is necessary for the GetSourceGeneratedDocumentInfoAsync and
            // GetContentsAsync calls to see the exact same data and return sensible results.
            using var connection = client.CreateConnection<IRemoteSourceGenerationService>(callbackTarget: null);
            using var _ = await RemoteKeepAliveSession.CreateAsync(
                compilationState, projectId, cancellationToken).ConfigureAwait(false);

            // First, grab the info from our external host about the generated documents it has for this project.  Note:
            // we ourselves are the innermost "RegularCompilationTracker" responsible for actually running generators.
            // As such, our call to the oop side reflects that by asking for the real source generated docs, and *not*
            // any overlaid 'frozen' source generated documents.
            //
            // CRITICAL: We pass the "compilationState+projectId" as the context for the invocation, matching the
            // KeepAliveSession above.  This ensures the call to GetContentsAsync below sees the exact same solution
            // instance as this call.
            var infosOpt = await connection.TryInvokeAsync(
                compilationState,
                projectId,
                (service, solutionChecksum, cancellationToken) => service.GetSourceGeneratedDocumentInfoAsync(
                    solutionChecksum, projectId, withFrozenSourceGeneratedDocuments: false, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            // Since we called out to the OOP side, we'll want to later report summarized telemetry numbers.
            solution.Services.GetService<ISourceGeneratorTelemetryReporterWorkspaceService>()?.QueueReportingOfTelemetry();

            if (!infosOpt.HasValue)
                return null;

            var infos = infosOpt.Value;

            // If there are no generated documents, bail out immediately.
            if (infos.Length == 0)
                return (compilationWithoutGeneratedFiles, TextDocumentStates<SourceGeneratedDocumentState>.Empty);

            // Next, figure out what is different locally.  Specifically, what documents we don't know about, or we
            // know about but whose text contents are different.
            using var _1 = ArrayBuilder<DocumentId>.GetInstance(out var documentsToAddOrUpdate);
            using var _2 = PooledDictionary<DocumentId, int>.GetInstance(out var documentIdToIndex);

            foreach (var (documentIdentity, contentIdentity, _) in infos)
            {
                var documentId = documentIdentity.DocumentId;
                Contract.ThrowIfFalse(documentId.IsSourceGenerated);

                var existingDocument = oldGeneratedDocuments.GetState(documentId);

                // Can keep what we have if it has the same doc and content identity.
                if (existingDocument?.Identity == documentIdentity &&
                    existingDocument.GetContentIdentity() == contentIdentity)
                {
                    continue;
                }

                // Couldn't find a matching generated doc.  Add this to the list to pull down.
                documentIdToIndex.Add(documentId, documentsToAddOrUpdate.Count);
                documentsToAddOrUpdate.Add(documentId);
            }

            // If we produced just as many documents as before, and none of them required any changes, then we can
            // reuse the prior compilation.
            if (infos.Length == oldGeneratedDocuments.Count &&
                documentsToAddOrUpdate.Count == 0 &&
                compilationWithStaleGeneratedTrees != null &&
                oldGeneratedDocuments.States.All(kvp => kvp.Value.ParseOptions.Equals(this.ProjectState.ParseOptions)))
            {
                // Even though non of the contents changed, it's possible that the timestamps on them did.
                foreach (var (documentIdentity, _, generationDateTime) in infos)
                {
                    var documentId = documentIdentity.DocumentId;
                    oldGeneratedDocuments = oldGeneratedDocuments.SetState(oldGeneratedDocuments.GetRequiredState(documentId).WithGenerationDateTime(generationDateTime));
                }

                // If there are no generated documents though, then just use the compilationWithoutGeneratedFiles so we
                // only hold onto that single compilation from this point on.
                return oldGeneratedDocuments.Count == 0
                    ? (compilationWithoutGeneratedFiles, oldGeneratedDocuments)
                    : (compilationWithStaleGeneratedTrees, oldGeneratedDocuments);
            }

            // Either we generated a different number of files, and/or we had contents of files that changed. Ensure we
            // know the contents of any new/changed files.  Note: we ourselves are the innermost
            // "RegularCompilationTracker" responsible for actually running generators. As such, our call to the oop
            // side reflects that by asking for the real source generated docs, and *not* any overlaid 'frozen' source
            // generated documents.
            //
            // CRITICAL: We pass the "compilationState+projectId" as the context for the invocation, matching the
            // KeepAliveSession above.  This ensures that we see the exact same solution instance on the OOP side as the
            // call to GetSourceGeneratedDocumentInfoAsync above.
            var generatedSourcesOpt = await connection.TryInvokeAsync(
                compilationState,
                projectId,
                (service, solutionChecksum, cancellationToken) => service.GetContentsAsync(
                    solutionChecksum, projectId, documentsToAddOrUpdate.ToImmutable(), withFrozenSourceGeneratedDocuments: false, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!generatedSourcesOpt.HasValue)
                return null;

            var generatedSources = generatedSourcesOpt.Value;
            Contract.ThrowIfTrue(generatedSources.Length != documentsToAddOrUpdate.Count);

            // Now go through and produce the new document states, using what we have already if it is unchanged, or
            // what we have retrieved for anything new/changed.
            using var generatedDocumentsBuilder = TemporaryArray<SourceGeneratedDocumentState>.Empty;
            foreach (var (documentIdentity, contentIdentity, generationDateTime) in infos)
            {
                var documentId = documentIdentity.DocumentId;
                Contract.ThrowIfFalse(documentId.IsSourceGenerated);

                if (documentIdToIndex.TryGetValue(documentId, out var addOrUpdateIndex))
                {
                    // a document whose content we fetched from the remote side.
                    var generatedSource = generatedSources[addOrUpdateIndex];
                    var sourceText = SourceText.From(
                        generatedSource, contentIdentity.EncodingName is null ? null : Encoding.GetEncoding(contentIdentity.EncodingName), contentIdentity.ChecksumAlgorithm);

                    var generatedDocument = SourceGeneratedDocumentState.Create(
                        documentIdentity,
                        sourceText,
                        ProjectState.ParseOptions!,
                        ProjectState.LanguageServices,
                        // Server provided us the checksum, so we just pass that along.  Note: it is critical that we do
                        // this as it may not be possible to reconstruct the same checksum the server produced due to
                        // the lossy nature of source texts.  See comment on GetOriginalSourceTextChecksum for more detail.
                        contentIdentity.OriginalSourceTextContentHash,
                        generationDateTime);
                    Contract.ThrowIfTrue(generatedDocument.GetOriginalSourceTextContentHash() != contentIdentity.OriginalSourceTextContentHash, "Checksums must match!");
                    generatedDocumentsBuilder.Add(generatedDocument);
                }
                else
                {
                    // a document that already matched something locally.
                    var existingDocument = oldGeneratedDocuments.GetRequiredState(documentId);
                    Contract.ThrowIfTrue(existingDocument.Identity != documentIdentity, "Identities must match!");
                    Contract.ThrowIfTrue(existingDocument.GetOriginalSourceTextContentHash() != contentIdentity.OriginalSourceTextContentHash, "Checksums must match!");

                    // ParseOptions may have changed between last generation and this one.  Ensure that they are
                    // properly propagated to the generated doc.
                    generatedDocumentsBuilder.Add(existingDocument
                        .WithParseOptions(this.ProjectState.ParseOptions!)
                        .WithGenerationDateTime(generationDateTime));
                }
            }

            var newGeneratedDocuments = new TextDocumentStates<SourceGeneratedDocumentState>(generatedDocumentsBuilder.ToImmutableAndClear());
            var compilationWithGeneratedFiles = compilationWithoutGeneratedFiles.AddSyntaxTrees(
                await newGeneratedDocuments.States.Values.SelectAsArrayAsync(
                    static (state, cancellationToken) => state.GetSyntaxTreeAsync(cancellationToken), cancellationToken).ConfigureAwait(false));
            return (compilationWithGeneratedFiles, newGeneratedDocuments);
        }

        private async Task<(Compilation compilationWithGeneratedFiles, TextDocumentStates<SourceGeneratedDocumentState> generatedDocuments, GeneratorDriver? generatorDriver)> ComputeNewGeneratorInfoInCurrentProcessAsync(
            SolutionCompilationState compilationState,
            Compilation compilationWithoutGeneratedFiles,
            TextDocumentStates<SourceGeneratedDocumentState> oldGeneratedDocuments,
            GeneratorDriver? generatorDriver,
            Compilation? compilationWithStaleGeneratedTrees,
            GeneratedDocumentCreationPolicy creationPolicy,
            CancellationToken cancellationToken)
        {
            // If we don't have any source generators.  Trivially bail out.
            if (!await compilationState.HasSourceGeneratorsAsync(this.ProjectState.Id, cancellationToken).ConfigureAwait(false))
                return (compilationWithoutGeneratedFiles, TextDocumentStates<SourceGeneratedDocumentState>.Empty, generatorDriver);

            // HACK HACK HACK HACK to address https://github.com/dotnet/roslyn/issues/59818. There, we were running into issues where
            // a generator being present and consuming syntax was causing all red nodes to be processed. This was problematic when
            // Razor design time files are also fed in, since those files tend to be quite large. The Razor design time files
            // aren't produced via a generator, but rather via our legacy IDynamicFileInfo mechanism, so it's also a bit strange
            // we'd even give them to other generators since that doesn't match the real compiler anyways. This simply removes
            // all of those trees in an effort to speed things up, and also ensure the design time compilations are a bit more accurate.
            using var _ = ArrayBuilder<SyntaxTree>.GetInstance(out var treesToRemove);

            foreach (var documentState in ProjectState.DocumentStates.States)
            {
                // This matches the logic in CompileTimeSolutionProvider for which documents are removed when we're
                // activating the generator.
                if (documentState.Value.Attributes.DesignTimeOnly)
                    treesToRemove.Add(await documentState.Value.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
            }

            var compilationToRunGeneratorsOn = compilationWithoutGeneratedFiles.RemoveSyntaxTrees(treesToRemove);
            // END HACK HACK HACK HACK.

            // Hold onto the prior results so we can compare when filtering
            var priorRunResult = generatorDriver?.GetRunResult();

            var generatorDriverCache = compilationState.GetGeneratorDriverInitializationCache(this.ProjectState.Id);

            if (generatorDriver == null)
            {
                generatorDriver = await generatorDriverCache.CreateAndRunGeneratorDriverAsync(this.ProjectState, compilationToRunGeneratorsOn, ShouldGeneratorRun, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                generatorDriver = generatorDriver.RunGenerators(compilationToRunGeneratorsOn, ShouldGeneratorRun, cancellationToken);
            }

            // Since this is our most recent run, we'll update our cache with this one. This has two benefits:
            //
            // 1. If some other fork of this Solution needs a GeneratorDriver created, it'll have one that's probably more update to date.
            //    This is obviously speculative -- if it's a really old Solution fork it might not help, but can't hurt for the more common cases.
            // 2. It ensures that we're not holding an old GeneratorDriver alive, which itself may hold onto state that's no longer applicable.
            generatorDriverCache.UpdateCacheWithGeneratorDriver(generatorDriver);

            CheckGeneratorDriver(generatorDriver, this.ProjectState);

            var runResult = generatorDriver.GetRunResult();

            var telemetryCollector = compilationState.SolutionState.Services.GetService<ISourceGeneratorTelemetryCollectorWorkspaceService>();
            telemetryCollector?.CollectRunResult(
                runResult, generatorDriver.GetTimingInfo(),
                g => GetAnalyzerReference(this.ProjectState, g));

            var telemetryReporter = compilationState.SolutionState.Services.GetService<ISourceGeneratorTelemetryReporterWorkspaceService>();
            telemetryReporter?.QueueReportingOfTelemetry();

            // We may be able to reuse compilationWithStaleGeneratedTrees if the generated trees are identical. We will assign null
            // to compilationWithStaleGeneratedTrees if we at any point realize it can't be used. We'll first check the count of trees
            // if that changed then we absolutely can't reuse it. But if the counts match, we'll then see if each generated tree
            // content is identical to the prior generation run; if we find a match each time, then the set of the generated trees
            // and the prior generated trees are identical.
            if (compilationWithStaleGeneratedTrees != null)
            {
                var generatedTreeCount =
                    runResult.Results.Sum(r => r.GeneratedSources.IsDefaultOrEmpty ? 0 : r.GeneratedSources.Length);

                if (oldGeneratedDocuments.Count != generatedTreeCount)
                    compilationWithStaleGeneratedTrees = null;
            }

            using var generatedDocumentsBuilder = TemporaryArray<SourceGeneratedDocumentState>.Empty;

            // Capture the date now.  We want all the generated files to use this date consistently.
            var generationDateTime = DateTime.Now;
            foreach (var generatorResult in runResult.Results)
            {
                var generatorAnalyzerReference = GetAnalyzerReference(this.ProjectState, generatorResult.Generator);

                foreach (var generatedSource in generatorResult.GeneratedSources)
                {
                    var existing = FindExistingGeneratedDocumentState(
                        oldGeneratedDocuments,
                        generatorResult.Generator,
                        generatorAnalyzerReference,
                        generatedSource.HintName);

                    if (existing != null)
                    {
                        var newDocument = existing
                            .WithText(generatedSource.SourceText)
                            .WithParseOptions(this.ProjectState.ParseOptions!);

                        // If changing the text/parse-options actually produced something new, then we can't use the
                        // stale trees.  We also want to mark this point at the point when the document was generated.
                        if (newDocument != existing)
                        {
                            compilationWithStaleGeneratedTrees = null;
                            newDocument = newDocument.WithGenerationDateTime(generationDateTime);
                        }

                        generatedDocumentsBuilder.Add(newDocument);
                    }
                    else
                    {
                        // NOTE: the use of generatedSource.SyntaxTree to fetch the path and options is OK,
                        // since the tree is a lazy tree and that won't trigger the parse.
                        var identity = SourceGeneratedDocumentIdentity.Generate(
                            ProjectState.Id,
                            generatedSource.HintName,
                            generatorResult.Generator,
                            generatedSource.SyntaxTree.FilePath,
                            generatorAnalyzerReference);

                        generatedDocumentsBuilder.Add(
                            SourceGeneratedDocumentState.Create(
                                identity,
                                generatedSource.SourceText,
                                generatedSource.SyntaxTree.Options,
                                ProjectState.LanguageServices,
                                // Compute the checksum on demand from the given source text.
                                originalSourceTextChecksum: null,
                                generationDateTime));

                        // The count of trees was the same, but something didn't match up. Since we're here, at least one tree
                        // was added, and an equal number must have been removed. Rather than trying to incrementally update
                        // this compilation, we'll just toss this and re-add all the trees.
                        compilationWithStaleGeneratedTrees = null;
                    }
                }
            }

            // If we didn't null out this compilation, it means we can actually use it
            if (compilationWithStaleGeneratedTrees != null)
            {
                // If there are no generated documents though, then just use the compilationWithoutGeneratedFiles so we
                // only hold onto that single compilation from this point on.
                return oldGeneratedDocuments.Count == 0
                    ? (compilationWithoutGeneratedFiles, oldGeneratedDocuments, generatorDriver)
                    : (compilationWithStaleGeneratedTrees, oldGeneratedDocuments, generatorDriver);
            }

            // We produced new documents, so time to create new state for it
            var newGeneratedDocuments = new TextDocumentStates<SourceGeneratedDocumentState>(generatedDocumentsBuilder.ToImmutableAndClear());
            var compilationWithGeneratedFiles = compilationWithoutGeneratedFiles.AddSyntaxTrees(
                await newGeneratedDocuments.States.Values.SelectAsArrayAsync(
                    static (state, cancellationToken) => state.GetSyntaxTreeAsync(cancellationToken), cancellationToken).ConfigureAwait(false));
            return (compilationWithGeneratedFiles, newGeneratedDocuments, generatorDriver);

            static SourceGeneratedDocumentState? FindExistingGeneratedDocumentState(
                TextDocumentStates<SourceGeneratedDocumentState> states,
                ISourceGenerator generator,
                AnalyzerReference analyzerReference,
                string hintName)
            {
                var generatorIdentity = SourceGeneratorIdentity.Create(generator, analyzerReference);

                foreach (var (_, state) in states.States)
                {
                    if (state.Identity.Generator != generatorIdentity)
                        continue;

                    if (state.HintName != hintName)
                        continue;

                    return state;
                }

                return null;
            }

            [Conditional("DEBUG")]
            static void CheckGeneratorDriver(GeneratorDriver generatorDriver, ProjectState projectState)
            {
                // Assert that the generator driver is in sync with our additional document states; there's not a public
                // API to get this, but we'll reflect in DEBUG-only.
                var driverType = generatorDriver.GetType();
                var stateMember = driverType.GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Contract.ThrowIfNull(stateMember);
                var additionalTextsMember = stateMember.FieldType.GetField("AdditionalTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Contract.ThrowIfNull(additionalTextsMember);
                var state = stateMember.GetValue(generatorDriver);
                var additionalTexts = (ImmutableArray<AdditionalText>)additionalTextsMember.GetValue(state)!;

                Contract.ThrowIfFalse(additionalTexts.Length == projectState.AdditionalDocumentStates.Count);
            }

            bool ShouldGeneratorRun(GeneratorFilterContext context)
            {
                // We should never try and run a generator driver if we're not expecting to do any work
                Contract.ThrowIfTrue(creationPolicy is GeneratedDocumentCreationPolicy.DoNotCreate);

                // If we're in Create mode, we're always going to run all generators
                if (creationPolicy is GeneratedDocumentCreationPolicy.Create)
                    return true;

                // If we get here we expect to be in CreateOnlyRequired. Throw to ensure we catch if someone adds a new state
                Contract.ThrowIfFalse(creationPolicy is GeneratedDocumentCreationPolicy.CreateOnlyRequired);

                // We want to only run required generators, but it's also possible that there are generators that 
                // have never been run (for instance, an AddGenerator operation might have occurred between runs).
                // Our model is that it's acceptable for documents to be slightly out of date, but it is
                // fundamentally incorrect to have *no* documents for a generator that could be producing them.

                // If there was no prior run result, then we can't have any documents for this generator, so we
                // need to re-run it.
                if (priorRunResult is null)
                    return true;

                // Next we need to check if this particular generator was run as part of the prior driver execution.
                // Either we have no state for the generator, in which case it can't have run. If we do have state,
                // the contract from the generator driver is that a generator that hasn't run yet produces a default
                // ImmutableArray for GeneratedSources. Note that this is different from an empty array, which
                // indicates that the generator ran, but didn't produce any documents:

                // - GeneratedSources == default ImmutableArray: the generator was not invoked during that run (must run).
                // - GeneratedSources == non-default empty array: the generator ran but produced no documents (may skip).
                // - GeneratedSources == non-default non-empty array: the generator ran and produced documents (may skip).

                if (!priorRunResult.Results.Any(r => r.Generator == context.Generator && !r.GeneratedSources.IsDefault))
                    return true;

                // We have results for this generator, and we're in CreateOnlyRequired, so only run this generator if
                // we consider it to be required.
                return context.Generator.IsRequiredGenerator();
            }
        }
    }
}
