// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;

internal interface ICopilotSemanticSearchQueryExecutor
{
    Task<CopilotSemanticSearchQueryResults> ExecuteAsync(string query, int resultCountLimit, CancellationToken cancellationToken);
}

internal readonly struct CopilotSemanticSearchQueryResults
{
    public required IReadOnlyList<string> Symbols { get; init; }
    public required IReadOnlyList<(string id, string message)> CompilationErrors { get; init; }
    public string? Error { get; init; }
    public required bool LimitReached { get; init; }
}
