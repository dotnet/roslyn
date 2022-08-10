// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
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
            => GetParseOptionsChecksum(_solutionServices.GetService<ISerializerService>());

        private Checksum GetParseOptionsChecksum(ISerializerService serializer)
            => this.SupportsCompilation
                ? ChecksumCache.GetOrCreate(this.ParseOptions, _ => serializer.CreateParseOptionsChecksum(this.ParseOptions))
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

                    var serializer = _solutionServices.GetService<ISerializerService>();

                    var infoChecksum = serializer.CreateChecksum(ProjectInfo.Attributes, cancellationToken);

                    // these compiler objects doesn't have good place to cache checksum. but rarely ever get changed.
                    var compilationOptionsChecksum = SupportsCompilation ? ChecksumCache.GetOrCreate(CompilationOptions, _ => serializer.CreateChecksum(CompilationOptions, cancellationToken)) : Checksum.Null;
                    cancellationToken.ThrowIfCancellationRequested();
                    var parseOptionsChecksum = GetParseOptionsChecksum(serializer);

                    var projectReferenceChecksums = ChecksumCache.GetOrCreate<ChecksumCollection>(ProjectReferences, _ => new ChecksumCollection(ProjectReferences.SelectAsArray(r => serializer.CreateChecksum(r, cancellationToken))));
                    var metadataReferenceChecksums = ChecksumCache.GetOrCreate<ChecksumCollection>(MetadataReferences, _ => new ChecksumCollection(MetadataReferences.SelectAsArray(r => serializer.CreateChecksum(r, cancellationToken))));
                    var analyzerReferenceChecksums = ChecksumCache.GetOrCreate<ChecksumCollection>(AnalyzerReferences, _ => new ChecksumCollection(AnalyzerReferences.SelectAsArray(r => serializer.CreateChecksum(r, cancellationToken))));

                    var documentChecksums = new ChecksumCollection(await documentChecksumsTasks.WhenAll().ConfigureAwait(false));
                    var additionalDocumentChecksums = new ChecksumCollection(await additionalDocumentChecksumTasks.WhenAll().ConfigureAwait(false));
                    var analyzerConfigDocumentChecksums = new ChecksumCollection(await analyzerConfigDocumentChecksumTasks.WhenAll().ConfigureAwait(false));

                    return new ProjectStateChecksums(
                        infoChecksum,
                        compilationOptionsChecksum,
                        parseOptionsChecksum,
                        documentChecksums,
                        projectReferenceChecksums,
                        metadataReferenceChecksums,
                        analyzerReferenceChecksums,
                        additionalDocumentChecksums,
                        analyzerConfigDocumentChecksums);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
