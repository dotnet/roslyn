// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis
{
    internal partial class TextDocumentState
    {
        public bool TryGetStateChecksums(out DocumentStateChecksums stateChecksums)
        {
            return _lazyChecksums.TryGetValue(out stateChecksums);
        }

        public async Task<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
        {
            var collection = await _lazyChecksums.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return collection.Checksum;
        }

        private async Task<DocumentStateChecksums> ComputeChecksumsAsync(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.DocumentState_ComputeChecksumsAsync, FilePath, cancellationToken))
            {
                var textTask = GetTextAsync(cancellationToken);

                var serializer = new Serializer(solutionServices.Workspace.Services);

                var infoChecksum = serializer.CreateChecksum(Info.Attributes, cancellationToken);
                var textChecksum = serializer.CreateChecksum(await textTask.ConfigureAwait(false), cancellationToken);

                return new DocumentStateChecksums(infoChecksum, textChecksum);
            }
        }
    }
}
