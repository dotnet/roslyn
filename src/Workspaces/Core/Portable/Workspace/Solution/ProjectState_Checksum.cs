// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class ProjectState
    {
        public bool TryGetStateChecksums(out ProjectStateChecksums stateChecksums)
            => _lazyChecksums.TryGetValue(out stateChecksums);

        public Task<ProjectStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
            => _lazyChecksums.GetValueAsync(cancellationToken);

        public Task<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
        {
            return SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(
                static (lazyChecksums, cancellationToken) => new ValueTask<ProjectStateChecksums>(lazyChecksums.GetValueAsync(cancellationToken)),
                static (projectStateChecksums, _) => projectStateChecksums.Checksum,
                _lazyChecksums,
                cancellationToken).AsTask();
        }

        public Checksum GetParseOptionsChecksum()
            => GetParseOptionsChecksum(LanguageServices.SolutionServices.GetService<ISerializerService>());

        private Checksum GetParseOptionsChecksum(ISerializerService serializer)
            => this.SupportsCompilation
                ? ChecksumCache.GetOrCreate(this.ParseOptions, static (options, serializer) => serializer.CreateParseOptionsChecksum(options), serializer)
                : Checksum.Null;

        private async Task<ProjectStateChecksums> ComputeChecksumsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (Logger.LogBlock(FunctionId.ProjectState_ComputeChecksumsAsync, FilePath, cancellationToken))
                {
                    var documentChecksumsTasks = DocumentStates.SelectAsArray(static (state, token) => state.GetChecksumAsync(token), cancellationToken);
                    var additionalDocumentChecksumTasks = AdditionalDocumentStates.SelectAsArray(static (state, token) => state.GetChecksumAsync(token), cancellationToken);
                    var analyzerConfigDocumentChecksumTasks = AnalyzerConfigDocumentStates.SelectAsArray(static (state, token) => state.GetChecksumAsync(token), cancellationToken);

                    var serializer = LanguageServices.SolutionServices.GetService<ISerializerService>();

                    var infoChecksum = this.ProjectInfo.Attributes.Checksum;

                    // these compiler objects doesn't have good place to cache checksum. but rarely ever get changed.
                    var compilationOptionsChecksum = SupportsCompilation
                        ? ChecksumCache.GetOrCreate(CompilationOptions, static (options, tuple) => tuple.serializer.CreateChecksum(options, tuple.cancellationToken), (serializer, cancellationToken))
                        : Checksum.Null;
                    cancellationToken.ThrowIfCancellationRequested();
                    var parseOptionsChecksum = GetParseOptionsChecksum(serializer);

                    var projectReferenceChecksums = ChecksumCache.GetOrCreateChecksumCollection(ProjectReferences, serializer, cancellationToken);
                    var metadataReferenceChecksums = ChecksumCache.GetOrCreateChecksumCollection(MetadataReferences, serializer, cancellationToken);
                    var analyzerReferenceChecksums = ChecksumCache.GetOrCreateChecksumCollection(AnalyzerReferences, serializer, cancellationToken);

                    var documentChecksums = new ChecksumCollection(await documentChecksumsTasks.WhenAll().ConfigureAwait(false));
                    var additionalDocumentChecksums = new ChecksumCollection(await additionalDocumentChecksumTasks.WhenAll().ConfigureAwait(false));
                    var analyzerConfigDocumentChecksums = new ChecksumCollection(await analyzerConfigDocumentChecksumTasks.WhenAll().ConfigureAwait(false));

                    return new ProjectStateChecksums(
                        this.Id,
                        infoChecksum,
                        compilationOptionsChecksum,
                        parseOptionsChecksum,
                        projectReferenceChecksums,
                        metadataReferenceChecksums,
                        analyzerReferenceChecksums,
                        new(documentChecksums, DocumentStates.SelectAsArray(static s => s.Id)),
                        new(additionalDocumentChecksums, AdditionalDocumentStates.SelectAsArray(static s => s.Id)),
                        new(analyzerConfigDocumentChecksums, AnalyzerConfigDocumentStates.SelectAsArray(static s => s.Id)));
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }
}
