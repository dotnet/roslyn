// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.SourceGeneratorTelemetry;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionCompilationState
{
    private partial class CompilationTracker : ICompilationTracker
    {
        private async Task<(Compilation compilationWithGeneratedFiles, CompilationTrackerGeneratorInfo nextGeneratorInfo)> AddExistingOrComputeNewGeneratorInfoAsync(
            CreationPolicy creationPolicy,
            SolutionCompilationState compilationState,
            Compilation compilationWithoutGeneratedFiles,
            CompilationTrackerGeneratorInfo generatorInfo,
            Compilation? compilationWithStaleGeneratedTrees,
            CancellationToken cancellationToken)
        {
            if (creationPolicy.GeneratedDocumentCreationPolicy is GeneratedDocumentCreationPolicy.DoNotCreate)
            {
                // We're frozen.  So we do not want to go through the expensive cost of running generators.  Instead, we
                // just whatever prior generated docs we have.
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
                    cancellationToken).ConfigureAwait(false);
                return (compilationWithGeneratedFiles, new(nextGeneratedDocuments, nextGeneratorDriver));
            }
        }

        private async Task<(Compilation compilationWithGeneratedFiles, TextDocumentStates<SourceGeneratedDocumentState> generatedDocuments)?> TryComputeNewGeneratorInfoInRemoteProcessAsync(
            SolutionCompilationState compilationState,
            Compilation compilationWithoutGeneratedFiles,
            TextDocumentStates<SourceGeneratedDocumentState> oldGeneratedDocuments,
            Compilation? compilationWithStaleGeneratedTrees,
            CancellationToken cancellationToken)
        {
            var solution = compilationState.SolutionState;

            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client is null)
                return null;

            // We're going to be making multiple calls over to OOP.  No point in resyncing data multiple times.  Keep a
            // single connection, and keep this solution instance alive (and synced) on both sides of the connection
            // throughout the calls.
            using var connection = client.CreateConnection<IRemoteSourceGenerationService>(callbackTarget: null);
            using var _ = await RemoteKeepAliveSession.CreateAsync(compilationState, cancellationToken).ConfigureAwait(false);

            // First, grab the info from our external host about the generated documents it has for this project.
            var projectId = this.ProjectState.Id;
            var infosOpt = await connection.TryInvokeAsync(
                compilationState,
                projectId,
                (service, solutionChecksum, cancellationToken) => service.GetSourceGenerationInfoAsync(solutionChecksum, projectId, cancellationToken),
                cancellationToken).ConfigureAwait(false);

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

            // Either we generated a different number of files, and/or we had contents of files that changed. Ensure
            // we know the contents of any new/changed files.
            var generatedSourcesOpt = await connection.TryInvokeAsync(
                compilationState,
                projectId,
                (service, solutionChecksum, cancellationToken) => service.GetContentsAsync(
                    solutionChecksum, projectId, documentsToAddOrUpdate.ToImmutable(), cancellationToken),
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
            CancellationToken cancellationToken)
        {
            // If we don't have any source generators.  Trivially bail out.
            if (!await compilationState.HasSourceGeneratorsAsync(this.ProjectState.Id, cancellationToken).ConfigureAwait(false))
                return (compilationWithoutGeneratedFiles, TextDocumentStates<SourceGeneratedDocumentState>.Empty, generatorDriver);

            // If we don't already have an existing generator driver, create one from scratch
            generatorDriver ??= CreateGeneratorDriver(this.ProjectState);

            CheckGeneratorDriver(generatorDriver, this.ProjectState);

            Contract.ThrowIfNull(generatorDriver);

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

            generatorDriver = generatorDriver.RunGenerators(compilationToRunGeneratorsOn, cancellationToken);

            Contract.ThrowIfNull(generatorDriver);

            var runResult = generatorDriver.GetRunResult();

            var telemetryCollector = compilationState.SolutionState.Services.GetService<ISourceGeneratorTelemetryCollectorWorkspaceService>();
            telemetryCollector?.CollectRunResult(
                runResult, generatorDriver.GetTimingInfo(),
                g => GetAnalyzerReference(this.ProjectState, g));

            // We may be able to reuse compilationWithStaleGeneratedTrees if the generated trees are identical. We will assign null
            // to compilationWithStaleGeneratedTrees if we at any point realize it can't be used. We'll first check the count of trees
            // if that changed then we absolutely can't reuse it. But if the counts match, we'll then see if each generated tree
            // content is identical to the prior generation run; if we find a match each time, then the set of the generated trees
            // and the prior generated trees are identical.
            if (compilationWithStaleGeneratedTrees != null)
            {
                var generatedTreeCount =
                    runResult.Results.Sum(r => IsGeneratorRunResultToIgnore(r) || r.GeneratedSources.IsDefaultOrEmpty ? 0 : r.GeneratedSources.Length);

                if (oldGeneratedDocuments.Count != generatedTreeCount)
                    compilationWithStaleGeneratedTrees = null;
            }

            using var generatedDocumentsBuilder = TemporaryArray<SourceGeneratedDocumentState>.Empty;

            // Capture the date now.  We want all the generated files to use this date consistently.
            var generationDateTime = DateTime.Now;
            foreach (var generatorResult in runResult.Results)
            {
                if (IsGeneratorRunResultToIgnore(generatorResult) || generatorResult.GeneratedSources.IsDefaultOrEmpty)
                    continue;

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

            static GeneratorDriver CreateGeneratorDriver(ProjectState projectState)
            {
                var additionalTexts = projectState.AdditionalDocumentStates.SelectAsArray(static documentState => documentState.AdditionalText);
                var compilationFactory = projectState.LanguageServices.GetRequiredService<ICompilationFactoryService>();

                return compilationFactory.CreateGeneratorDriver(
                    projectState.ParseOptions!,
                    GetSourceGenerators(projectState),
                    projectState.AnalyzerOptions.AnalyzerConfigOptionsProvider,
                    additionalTexts);
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
        }
    }
}
