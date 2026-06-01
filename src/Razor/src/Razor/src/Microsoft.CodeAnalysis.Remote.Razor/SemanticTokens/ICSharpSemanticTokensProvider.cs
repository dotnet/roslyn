// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.SemanticTokens;

internal interface ICSharpSemanticTokensProvider
{
    Task<int[]?> GetCSharpSemanticTokensResponseAsync(
        RemoteDocumentContext documentContext,
        ImmutableArray<LinePositionSpan> csharpSpans,
        Guid correlationId,
        CancellationToken cancellationToken);
}
