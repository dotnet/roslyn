// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionState
{
    private partial class CompilationTracker : ICompilationTracker
    {
        private async Task<(Compilation compilationWithGeneratedFiles, CompilationTrackerGeneratorInfo generatorInfo)> AddExistingOrComputeNewGeneratorInfoAsync(
            SolutionState solution,
            Compilation compilationWithoutGeneratedFiles,
            CompilationTrackerGeneratorInfo generatorInfo,
            Compilation? compilationWithStaleGeneratedTrees,
            CancellationToken cancellationToken)
        {
            if (generatorInfo.DocumentsAreFinal)
            {
                // We must have ran generators before, but for some reason had to remake the compilation from
                // scratch. This could happen if the trees were strongly held, but the compilation was entirely
                // garbage collected. Just add in the trees we already have. We don't want to rerun since the
                // consumer of this Solution snapshot has already seen the trees and thus needs to ensure identity
                // of them.
                var compilationWithGeneratedFiles = compilationWithoutGeneratedFiles.AddSyntaxTrees(
                    await generatorInfo.Documents.States.Values.SelectAsArrayAsync(state => state.GetSyntaxTreeAsync(cancellationToken)).ConfigureAwait(false));

                // Will reuse the generator info since we're reusing all the trees from within it.
                return (compilationWithGeneratedFiles, generatorInfo);
            }

            if (!this.ProjectState.SourceGenerators.Any())
            {
                // We don't have any source generators.  Trivially bail out.
                var compilationWithGeneratedFiles = compilationWithoutGeneratedFiles;
                return (compilationWithGeneratedFiles, new CompilationTrackerGeneratorInfo(
                    TextDocumentStates<SourceGeneratedDocumentState>.Empty, generatorInfo.Driver, documentsAreFinal: true));
            }

            return await ComputeNewGeneratorInfoAsync(
                solution,
                compilationWithoutGeneratedFiles,
                generatorInfo,
                compilationWithStaleGeneratedTrees,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<(Compilation compilationWithGeneratedFiles, CompilationTrackerGeneratorInfo generatorInfo)> ComputeNewGeneratorInfoAsync(
            SolutionState solution,
            Compilation compilationWithoutGeneratedFiles,
            CompilationTrackerGeneratorInfo generatorInfo,
            Compilation? compilationWithStaleGeneratedTrees,
            CancellationToken cancellationToken)
        {
            var result = await TryComputeNewGeneratorInfoInRemoteProcessAsync(
                solution, compilationWithoutGeneratedFiles, generatorInfo, compilationWithStaleGeneratedTrees, cancellationToken).ConfigureAwait(false);
            if (result.HasValue)
                return result.Value;

            return await ComputeNewGeneratorInfoInCurrentProcessAsync(
                solution, compilationWithoutGeneratedFiles, generatorInfo, compilationWithStaleGeneratedTrees, cancellationToken).ConfigureAwait(false);
        }

        private async Task<(Compilation compilationWithGeneratedFiles, CompilationTrackerGeneratorInfo generatorInfo)?> TryComputeNewGeneratorInfoInRemoteProcessAsync(
            SolutionState solution,
            Compilation compilationWithoutGeneratedFiles,
            CompilationTrackerGeneratorInfo generatorInfo,
            Compilation? compilationWithStaleGeneratedTrees,
            CancellationToken cancellationToken)
        {
            var options = solution.Services.GetRequiredService<IWorkspaceConfigurationService>().Options;
            if (options.RunSourceGeneratorsInProcessOnly)
                return null;

            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client is null)
                return null;

            using var connection = client.CreateConnection<IRemoteSourceGenerationService>(callbackTarget: null);

            // First, grab the info from our external host about the generated documents it has for this project.
            var projectId = this.ProjectState.Id;
            var infosOpt = await connection.TryInvokeAsync(
                solution,
                this.ProjectState.Id,
                (service, solutionChecksum, cancellationToken) => service.GetSourceGenerationInfoAsync(solutionChecksum, this.ProjectState.Id, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!infosOpt.HasValue)
                return null;

            // Next, figure out what is different locally.  Specifically, what documents we don't know about, or we
            // know about but whose text contents are different.
            using var _1 = ArrayBuilder<DocumentId>.GetInstance(out var documentsToAddOrUpdate);
            using var _2 = PooledHashSet<DocumentId>.GetInstance(out var allGeneratedDocumentIds);

            var infos = infosOpt.Value;
            foreach (var (documentIdentity, contentIdentity) in infos)
            {
                allGeneratedDocumentIds.Add(documentIdentity.DocumentId);

                var existingDocument = generatorInfo.Documents.GetState(documentIdentity.DocumentId);
                if (existingDocument?.Identity == documentIdentity)
                {
                    // ensure that the doc we have matches the content expected.
                    if (existingDocument.GetTextChecksum() == contentIdentity.Checksum &&
                        existingDocument.SourceText.Encoding?.WebName == contentIdentity.EncodingName &&
                        existingDocument.SourceText.ChecksumAlgorithm == contentIdentity.ChecksumAlgorithm)
                    {
                        continue;
                    }
                }

                // Couldn't find a matching generated doc.  Add this to the list to pull down.
                documentsToAddOrUpdate.Add(documentIdentity.DocumentId);
            }

            // If we produced just as many documents as before, and none of them required any changes, then we can
            // reuse the prior compilation.
            if (infos.Length == generatorInfo.Documents.Count &&
                documentsToAddOrUpdate.Count == 0 &&
                compilationWithStaleGeneratedTrees != null)
            {
                return (compilationWithStaleGeneratedTrees, generatorInfo.WithDocumentsAreFinal(true));
            }

            // Either we generated a different number of files, and/or we had contents of files that changed. Ensure
            // we know the contents of any new/changed files.
            var generatedSourcesOpt = await connection.TryInvokeAsync(
                solution,
                this.ProjectState.Id,
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
            foreach (var (documentIdentity, contentIdentity) in infos)
            {
                var addOrUpdateIndex = documentsToAddOrUpdate.IndexOf(documentIdentity.DocumentId);
                if (addOrUpdateIndex >= 0)
                {
                    // a document whose content we fetched from the remote side.
                    var generatedSource = generatedSources[addOrUpdateIndex];
                    var sourceText = SourceText.From(
                        generatedSource, contentIdentity.EncodingName is null ? null : Encoding.GetEncoding(contentIdentity.EncodingName), contentIdentity.ChecksumAlgorithm);

                    var generatedDocument = SourceGeneratedDocumentState.Create(
                        documentIdentity,
                        sourceText,
                        ProjectState.ParseOptions!,
                        ProjectState.LanguageServices);
                    Contract.ThrowIfTrue(generatedDocument.GetTextChecksum() != contentIdentity.Checksum, "Checksums must match!");
                    generatedDocumentsBuilder.Add(generatedDocument);
                }
                else
                {
                    // a document that already matched something locally.
                    var existingDocument = generatorInfo.Documents.GetRequiredState(documentIdentity.DocumentId);
                    Contract.ThrowIfTrue(existingDocument.Identity != documentIdentity, "Identies must match!");
                    Contract.ThrowIfTrue(existingDocument.GetTextChecksum() != contentIdentity.Checksum, "Checksums must match!");
                    generatedDocumentsBuilder.Add(existingDocument);
                }
            }

            var generatedDocuments = new TextDocumentStates<SourceGeneratedDocumentState>(generatedDocumentsBuilder.ToImmutableAndClear());
            var compilationWithGeneratedFiles = compilationWithoutGeneratedFiles.AddSyntaxTrees(
                await generatedDocuments.States.Values.SelectAsArrayAsync(state => state.GetSyntaxTreeAsync(cancellationToken)).ConfigureAwait(false));
            return (compilationWithGeneratedFiles, new CompilationTrackerGeneratorInfo(generatedDocuments, generatorInfo.Driver, documentsAreFinal: true));
        }
    }
}
