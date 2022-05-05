// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal readonly struct CommonQuickInfoContext
    {
        public readonly Workspace Workspace;
        public readonly SemanticModel SemanticModel;
        public readonly int Position;
        public readonly CancellationToken CancellationToken;

        public CommonQuickInfoContext(
            Workspace workspace,
            SemanticModel semanticModel,
            int position,
            CancellationToken cancellationToken)
        {
            Workspace = workspace;
            SemanticModel = semanticModel;
            Position = position;
            CancellationToken = cancellationToken;
        }
    }
}
