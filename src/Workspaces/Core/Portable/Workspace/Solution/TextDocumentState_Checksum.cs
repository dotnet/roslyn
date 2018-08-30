// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis
{
    internal partial class TextDocumentState
    {
        public bool TryGetStateChecksums(out DocumentStateChecksums stateChecksums)
        {
            return _lazyChecksums.TryGetValue(out stateChecksums);
        }

        public Task<DocumentStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
        {
            return _lazyChecksums.GetValueAsync(cancellationToken);
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
                var textAndVersionTask = GetTextAndVersionAsync(cancellationToken);

                var serializer = solutionServices.Workspace.Services.GetService<ISerializerService>();

                var infoChecksum = serializer.CreateChecksum(Attributes, cancellationToken);
                var textChecksum = serializer.CreateChecksum((await textAndVersionTask.ConfigureAwait(false)).Text, cancellationToken);

                return new DocumentStateChecksums(infoChecksum, textChecksum);
            }
        }
    }
}
