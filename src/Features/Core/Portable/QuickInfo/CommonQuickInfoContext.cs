// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal readonly struct CommonQuickInfoContext
    {
        public readonly Workspace Workspace;
        public readonly DocumentId DocumentId;
        public readonly SemanticModel SemanticModel;
        public readonly ImmutableArray<(DocumentId documentId, SemanticModel semanticModel)> LinkedSemanticModels;
        public readonly int Position;
        public readonly CancellationToken CancellationToken;

        public CommonQuickInfoContext(
            Workspace workspace,
            DocumentId documentId,
            SemanticModel semanticModel,
            ImmutableArray<(DocumentId documentId, SemanticModel semanticModel)> linkedSemanticModels,
            int position,
            CancellationToken cancellationToken)
        {
            Workspace = workspace;
            DocumentId = documentId;
            SemanticModel = semanticModel;
            LinkedSemanticModels = linkedSemanticModels;
            Position = position;
            CancellationToken = cancellationToken;
        }
    }
}
