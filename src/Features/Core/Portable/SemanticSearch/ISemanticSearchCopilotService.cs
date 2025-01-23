// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal interface ISemanticSearchCopilotService
{
    bool IsAvailable { get; }

    /// <summary>
    /// Translates natural language <paramref name="text"/> to C# query.
    /// </summary>
    ValueTask<SemanticSearchCopilotGeneratedQuery> TryGetQueryAsync(string text, SemanticSearchCopilotContext context, CancellationToken cancellationToken);
}
