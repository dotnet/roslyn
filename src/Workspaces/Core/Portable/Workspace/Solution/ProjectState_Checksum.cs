// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Internal.Log;
using System.Runtime.CompilerServices;
using System;

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

                // these compiler objects doesn't have good place to cache checksum. but rarely ever get changed.
                var compilationOptionsChecksum = SupportsCompilation ? ChecksumCache.GetOrCreate(CompilationOptions, () => serializer.CreateChecksum(CompilationOptions, cancellationToken)) : Checksum.Null;
                var parseOptionsChecksum = SupportsCompilation ? ChecksumCache.GetOrCreate(ParseOptions, () => serializer.CreateChecksum(ParseOptions, cancellationToken)) : Checksum.Null;

                var projectReferenceChecksums = ChecksumCache.GetOrCreate(ProjectReferences, () => new ProjectReferenceChecksumCollection(ProjectReferences.Select(r => serializer.CreateChecksum(r, cancellationToken)).ToArray()));
                var metadataReferenceChecksums = ChecksumCache.GetOrCreate(MetadataReferences, () => new MetadataReferenceChecksumCollection(MetadataReferences.Select(r => serializer.CreateChecksum(r, cancellationToken)).ToArray()));
                var analyzerReferenceChecksums = ChecksumCache.GetOrCreate(AnalyzerReferences, () => new AnalyzerReferenceChecksumCollection(AnalyzerReferences.Select(r => serializer.CreateChecksum(r, cancellationToken)).ToArray()));

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

        /// <summary>
        /// hold onto object checksum that currently doesn't have a place to hold onto checksum
        /// </summary>
        private static class ChecksumCache
        {
            private static readonly ConditionalWeakTable<object, object> s_cache = new ConditionalWeakTable<object, object>();

            public static Checksum GetOrCreate(object value, Func<Checksum> checksumCreator)
            {
                object saved;
                if (s_cache.TryGetValue(value, out saved))
                {
                    return (Checksum)saved;
                }

                // same key should always return same checksum
                return (Checksum)s_cache.GetValue(value, _ => checksumCreator());
            }

            public static T GetOrCreate<T>(object value, Func<T> checksumCreator) where T : IChecksummedObject
            {
                object saved;
                if (s_cache.TryGetValue(value, out saved))
                {
                    return (T)saved;
                }

                // same key should always return same checksum
                return (T)s_cache.GetValue(value, _ => checksumCreator());
            }
        }
    }
}
