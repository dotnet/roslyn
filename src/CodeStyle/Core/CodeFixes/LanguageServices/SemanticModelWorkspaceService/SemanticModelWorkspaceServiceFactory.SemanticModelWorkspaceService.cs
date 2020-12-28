// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SemanticModelReuse
{
    internal partial class SemanticModelReuseWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private sealed class SemanticModelReuseWorkspaceService : ISemanticModelReuseWorkspaceService
        {
            public SemanticModelReuseWorkspaceService(Workspace _)
            {
            }

            public ValueTask<SemanticModel> ReuseExistingSpeculativeModelAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
            {
                // TODO: port the GetSemanticModelForNodeAsync implementation from Workspaces layer,
                // which currently relies on a bunch of internal APIs.
                // For now, we fall back to the public API to fetch document's SemanticModel.
                return document.GetRequiredSemanticModelAsync(cancellationToken);
            }
        }
    }
}
