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

internal abstract partial class TextDocumentState
{
    public bool TryGetStateChecksums([NotNullWhen(returnValue: true)] out DocumentStateChecksums? stateChecksums)
        => _lazyChecksums.TryGetValue(out stateChecksums);

    public Task<DocumentStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
        => _lazyChecksums.GetValueAsync(cancellationToken);

    public async ValueTask<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
    {
        var documentStateChecksums = await _lazyChecksums.GetValueAsync(cancellationToken).ConfigureAwait(false);
        return documentStateChecksums.Checksum;
    }

    private async Task<DocumentStateChecksums> ComputeChecksumsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (Logger.LogBlock(FunctionId.DocumentState_ComputeChecksumsAsync, FilePath, cancellationToken))
            {
                var infoChecksum = this.Attributes.Checksum;
                var serializableText = await SerializableSourceText.FromTextDocumentStateAsync(this, cancellationToken).ConfigureAwait(false);
                var textChecksum = serializableText.ContentChecksum;

                return new DocumentStateChecksums(this.Id, infoChecksum, textChecksum);
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
