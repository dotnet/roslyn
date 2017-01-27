﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis
{
    internal partial class ProjectState
    {
        public bool TryGetStateChecksums(out ProjectStateChecksums stateChecksums)
        {
            return _lazyChecksums.TryGetValue(out stateChecksums);
        }

        public Task<ProjectStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
        {
            return _lazyChecksums.GetValueAsync(cancellationToken);
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
                var orderedDocumentIds = ChecksumCache.GetOrCreate(DocumentIds, _ => DocumentIds.OrderBy(id => id.Id).ToImmutableArray());
                var documentChecksumsTasks = orderedDocumentIds.Select(id => DocumentStates[id].GetChecksumAsync(cancellationToken));

                var orderedAdditionalDocumentIds = ChecksumCache.GetOrCreate(AdditionalDocumentIds, _ => AdditionalDocumentIds.OrderBy(id => id.Id).ToImmutableArray());
                var additionalDocumentChecksumTasks = orderedAdditionalDocumentIds.Select(id => AdditionalDocumentStates[id].GetChecksumAsync(cancellationToken));

                var serializer = new Serializer(_solutionServices.Workspace);

                var infoChecksum = serializer.CreateChecksum(ProjectInfo.Attributes, cancellationToken);

                // these compiler objects doesn't have good place to cache checksum. but rarely ever get changed.
                var compilationOptionsChecksum = SupportsCompilation ? ChecksumCache.GetOrCreate(CompilationOptions, _ => serializer.CreateChecksum(CompilationOptions, cancellationToken)) : Checksum.Null;
                var parseOptionsChecksum = SupportsCompilation ? ChecksumCache.GetOrCreate(ParseOptions, _ => serializer.CreateChecksum(ParseOptions, cancellationToken)) : Checksum.Null;

                var projectReferenceChecksums = ChecksumCache.GetOrCreate<ProjectReferenceChecksumCollection>(ProjectReferences, _ => new ProjectReferenceChecksumCollection(ProjectReferences.Select(r => serializer.CreateChecksum(r, cancellationToken)).ToArray()));
                var metadataReferenceChecksums = ChecksumCache.GetOrCreate<MetadataReferenceChecksumCollection>(MetadataReferences, _ => new MetadataReferenceChecksumCollection(MetadataReferences.Select(r => serializer.CreateChecksum(r, cancellationToken)).ToArray()));
                var analyzerReferenceChecksums = ChecksumCache.GetOrCreate<AnalyzerReferenceChecksumCollection>(AnalyzerReferences, _ => new AnalyzerReferenceChecksumCollection(AnalyzerReferences.Select(r => serializer.CreateChecksum(r, cancellationToken)).ToArray()));

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
