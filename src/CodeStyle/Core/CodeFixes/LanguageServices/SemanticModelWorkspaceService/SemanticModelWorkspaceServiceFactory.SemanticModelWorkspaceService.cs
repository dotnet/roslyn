// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SemanticModelWorkspaceService
{
    internal partial class SemanticModelWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private sealed class SemanticModelService : ISemanticModelService
        {
            public Task<SemanticModel> GetSemanticModelForNodeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken = default)
            {
                // get the base one
                return document.GetSemanticModelAsync(cancellationToken);
            }
        }
    }
}
