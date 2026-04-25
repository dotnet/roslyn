// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.SemanticTokens;

internal interface ICSharpSemanticTokensProvider
{
    Task<int[]?> GetCSharpSemanticTokensResponseAsync(
        DocumentContext documentContext,
        ImmutableArray<LinePositionSpan> csharpSpans,
        Guid correlationId,
        CancellationToken cancellationToken);
}
