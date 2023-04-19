// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Provides active statement spans within the specified document of a solution.
    /// </summary>
    internal delegate ValueTask<ImmutableArray<ActiveStatementSpan>> ActiveStatementSpanProvider(DocumentId? documentId, string filePath, CancellationToken cancellationToken);
}
