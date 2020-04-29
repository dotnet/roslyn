// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class TextDocumentState
    {
        public bool TryGetStateChecksums([NotNullWhen(returnValue: true)] out DocumentStateChecksums? stateChecksums)
            => _lazyChecksums.TryGetValue(out stateChecksums);

        public Task<DocumentStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
            => _lazyChecksums.GetValueAsync(cancellationToken);

        public async Task<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
        {
            var collection = await _lazyChecksums.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return collection.Checksum;
        }

        private async Task<DocumentStateChecksums> ComputeChecksumsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (Logger.LogBlock(FunctionId.DocumentState_ComputeChecksumsAsync, FilePath, cancellationToken))
                {
                    var textAndVersionTask = GetTextAndVersionAsync(cancellationToken);

                    var serializer = solutionServices.Workspace.Services.GetRequiredService<ISerializerService>();

                    var infoChecksum = serializer.CreateChecksum(Attributes, cancellationToken);
                    var textChecksum = serializer.CreateChecksum((await textAndVersionTask.ConfigureAwait(false)).Text, cancellationToken);

                    return new DocumentStateChecksums(infoChecksum, textChecksum);
                }
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
