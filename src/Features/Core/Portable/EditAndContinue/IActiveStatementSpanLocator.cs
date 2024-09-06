// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal interface IActiveStatementSpanLocator : IWorkspaceService
{
    /// <summary>
    /// Returns current locations of the active statement tracking spans in the specified document snapshot (#line target document).
    /// </summary>
    /// <returns>Empty array if tracking spans are not available for the document.</returns>
    ValueTask<ImmutableArray<ActiveStatementSpan>> GetSpansAsync(Solution solution, DocumentId? documentId, string filePath, CancellationToken cancellationToken);
}
