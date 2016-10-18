// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis
{
    internal partial class ProjectState
    {
        public bool TryGetStateChecksums(out ProjectStateChecksums stateChecksums)
        {
            return _lazyChecksums.TryGetValue(out stateChecksums);
        }

        public async Task<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
        {
            var collection = await _lazyChecksums.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return collection.Checksum;
        }

        private async Task<ProjectStateChecksums> ComputeChecksumsAsync(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.ProjectState_ComputeChecksumsAsync, FilePath, cancellationToken))
            {
                // get states by id order to have deterministic checksum
                var documentChecksumsTasks = DocumentIds.Select(id => DocumentStates[id].GetChecksumAsync(cancellationToken));
                var additionalDocumentChecksumTasks = AdditionalDocumentIds.Select(id => AdditionalDocumentStates[id].GetChecksumAsync(cancellationToken));

                var serializer = new Serializer(_solutionServices.Workspace.Services);

                var infoChecksum = serializer.CreateChecksum(ProjectInfo.Attributes, cancellationToken);

                var compilationOptionsChecksum = SupportsCompilation ? serializer.CreateChecksum(CompilationOptions, cancellationToken) : Checksum.Null;
                var parseOptionsChecksum = SupportsCompilation ? serializer.CreateChecksum(ParseOptions, cancellationToken) : Checksum.Null;

                var projectReferenceChecksums = new ProjectReferenceChecksumCollection(ProjectReferences.Select(r => serializer.CreateChecksum(r, cancellationToken)).ToArray());
                var metadataReferenceChecksums = new MetadataReferenceChecksumCollection(MetadataReferences.Select(r => serializer.CreateChecksum(r, cancellationToken)).ToArray());
                var analyzerReferenceChecksums = new AnalyzerReferenceChecksumCollection(AnalyzerReferences.Select(r => serializer.CreateChecksum(r, cancellationToken)).ToArray());

                var documentChecksums = await Task.WhenAll(documentChecksumsTasks).ConfigureAwait(false);
                var additionalChecksums = await Task.WhenAll(additionalDocumentChecksumTasks).ConfigureAwait(false);

                return new ProjectStateChecksums(
                    infoChecksum,
                    compilationOptionsChecksum,
                    parseOptionsChecksum,
                    new DocumentChecksumCollection(documentChecksums),
                    projectReferenceChecksums,
                    metadataReferenceChecksums,
                    analyzerReferenceChecksums,
                    new TextDocumentChecksumCollection(additionalChecksums));
            }
        }
    }
}
