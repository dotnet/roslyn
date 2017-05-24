// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.DocumentHighlighting
{
    internal abstract partial class AbstractDocumentHighlightsService : IDocumentHighlightsService
    {
        private static async Task<SolutionAndSessionHolder> TryGetRemoteSessionAsync(
            Solution solution, CancellationToken cancellationToken)
        {
            var outOfProcessAllowed = solution.Workspace.Options.GetOption(DocumentHighlightingOptions.OutOfProcessAllowed);
            if (!outOfProcessAllowed)
            {
                return null;
            }

            var client = await solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            return await client.TryCreateCodeAnalysisServiceSessionAsync(
                solution, cancellationToken).ConfigureAwait(false);
        }
    }
}