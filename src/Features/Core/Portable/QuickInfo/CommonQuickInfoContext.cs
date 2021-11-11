// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal readonly struct CommonQuickInfoContext
    {
        public readonly HostWorkspaceServices Services;
        public readonly SemanticModel SemanticModel;
        public readonly int Position;
        public readonly SymbolDescriptionOptions Options;
        public readonly CancellationToken CancellationToken;

        public CommonQuickInfoContext(
            HostWorkspaceServices services,
            SemanticModel semanticModel,
            int position,
            SymbolDescriptionOptions options,
            CancellationToken cancellationToken)
        {
            Services = services;
            SemanticModel = semanticModel;
            Position = position;
            Options = options;
            CancellationToken = cancellationToken;
        }
    }
}
