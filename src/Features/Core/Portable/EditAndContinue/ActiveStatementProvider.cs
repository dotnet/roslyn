// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Provides current active statements.
    /// </summary>
    internal delegate Task<ImmutableArray<ActiveStatementDebugInfo>> ActiveStatementProvider(CancellationToken cancellationToken);

    /// <summary>
    /// Provides active statement spans within the specified document of a solution.
    /// </summary>
    internal delegate Task<ImmutableArray<TextSpan>> SolutionActiveStatementSpanProvider(DocumentId documentId, CancellationToken cancellationToken);

    /// <summary>
    /// Provides active statement spans within a document.
    /// </summary>
    internal delegate Task<ImmutableArray<TextSpan>> DocumentActiveStatementSpanProvider(CancellationToken cancellationToken);
}
