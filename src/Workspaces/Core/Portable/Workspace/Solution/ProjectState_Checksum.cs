// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed partial class ProjectState
{
    public bool TryGetStateChecksums([NotNullWhen(true)] out ProjectStateChecksums? stateChecksums)
        => LazyChecksums.TryGetValue(out stateChecksums);

    public Task<ProjectStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
        => LazyChecksums.GetValueAsync(cancellationToken);

    public async ValueTask<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
    {
        var projectStateChecksums = await this.LazyChecksums.GetValueAsync(cancellationToken).ConfigureAwait(false);
        return projectStateChecksums.Checksum;
    }

    public Checksum GetParseOptionsChecksum()
        => GetParseOptionsChecksum(LanguageServices.SolutionServices.GetRequiredService<ISerializerService>());

    private Checksum GetParseOptionsChecksum(ISerializerService serializer)
        => this.SupportsCompilation
            ? ChecksumCache.GetOrCreate(this.ParseOptions!, static (options, serializer) => serializer.CreateParseOptionsChecksum(options), serializer)
            : Checksum.Null;

    private async Task<ProjectStateChecksums> ComputeChecksumsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (Logger.LogBlock(FunctionId.ProjectState_ComputeChecksumsAsync, FilePath, cancellationToken))
            {
                var documentChecksumsTask = DocumentStates.GetDocumentChecksumsAndIdsAsync(cancellationToken);
                var additionalDocumentChecksumsTask = AdditionalDocumentStates.GetDocumentChecksumsAndIdsAsync(cancellationToken);
                var analyzerConfigDocumentChecksumsTask = AnalyzerConfigDocumentStates.GetDocumentChecksumsAndIdsAsync(cancellationToken);

                var serializer = LanguageServices.SolutionServices.GetRequiredService<ISerializerService>();

                var infoChecksum = this.ProjectInfo.Attributes.Checksum;

                // these compiler objects doesn't have good place to cache checksum. but rarely ever get changed.
                var compilationOptionsChecksum = SupportsCompilation
                    ? ChecksumCache.GetOrCreate(CompilationOptions!, static (options, tuple) => tuple.serializer.CreateChecksum(options, tuple.cancellationToken), (serializer, cancellationToken))
                    : Checksum.Null;
                cancellationToken.ThrowIfCancellationRequested();
                var parseOptionsChecksum = GetParseOptionsChecksum(serializer);

                var projectReferenceChecksums = ChecksumCache.GetOrCreateChecksumCollection(ProjectReferences, serializer, cancellationToken);
                var metadataReferenceChecksums = ChecksumCache.GetOrCreateChecksumCollection(MetadataReferences, serializer, cancellationToken);
                var analyzerReferenceChecksums = ChecksumCache.GetOrCreateChecksumCollection(AnalyzerReferences, serializer, cancellationToken);

                return new ProjectStateChecksums(
                    this.Id,
                    infoChecksum,
                    compilationOptionsChecksum,
                    parseOptionsChecksum,
                    projectReferenceChecksums,
                    metadataReferenceChecksums,
                    analyzerReferenceChecksums,
                    documentChecksums: await documentChecksumsTask.ConfigureAwait(false),
                    await additionalDocumentChecksumsTask.ConfigureAwait(false),
                    await analyzerConfigDocumentChecksumsTask.ConfigureAwait(false));
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
