// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal sealed class SemanticModelGetter(Solution solution, CancellationToken cancellationToken)
{
    /// <summary>
    /// Returns <see cref="SemanticModel"/> for any <see cref="SyntaxTree"/> in the <see cref="Solution"/>.
    /// </summary>
    public Task<SemanticModel> GetSemanticModelAsync(SyntaxTree tree)
        // TODO: consider caching the model for the duration of the query execution
        => solution.GetRequiredDocument(tree).GetRequiredSemanticModelAsync(cancellationToken).AsTask();
}
