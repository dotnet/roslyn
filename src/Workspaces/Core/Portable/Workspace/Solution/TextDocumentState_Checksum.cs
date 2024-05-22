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

internal partial class TextDocumentState
{
    public bool TryGetStateChecksums([NotNullWhen(returnValue: true)] out DocumentStateChecksums? stateChecksums)
        => _lazyChecksums.TryGetValue(out stateChecksums);

    public Task<DocumentStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
        => _lazyChecksums.GetValueAsync(cancellationToken);

    public Task<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
    {
        return SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(
            static (lazyChecksums, cancellationToken) => new ValueTask<DocumentStateChecksums>(lazyChecksums.GetValueAsync(cancellationToken)),
            static (documentStateChecksums, _) => documentStateChecksums.Checksum,
            _lazyChecksums,
            cancellationToken).AsTask();
    }

    private async Task<DocumentStateChecksums> ComputeChecksumsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (Logger.LogBlock(FunctionId.DocumentState_ComputeChecksumsAsync, FilePath, cancellationToken))
            {
                var serializer = solutionServices.GetRequiredService<ISerializerService>();

                var infoChecksum = this.Attributes.Checksum;
                var serializableText = await SerializableSourceText.FromTextDocumentStateAsync(this, cancellationToken).ConfigureAwait(false);
                var textChecksum = serializer.CreateChecksum(serializableText, cancellationToken);

                return new DocumentStateChecksums(this.Id, infoChecksum, textChecksum);
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
