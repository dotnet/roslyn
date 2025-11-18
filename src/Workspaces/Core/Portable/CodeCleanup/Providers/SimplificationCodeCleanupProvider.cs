// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers;

internal sealed class SimplificationCodeCleanupProvider : ICodeCleanupProvider
{
    public string Name => PredefinedCodeCleanupProviderNames.Simplification;

    public Task<Document> CleanupAsync(Document document, ImmutableArray<TextSpan> spans, CodeCleanupOptions options, CancellationToken cancellationToken)
        => Simplifier.ReduceAsync(document, spans, options.SimplifierOptions, cancellationToken);

    public Task<SyntaxNode> CleanupAsync(SyntaxNode root, ImmutableArray<TextSpan> spans, SyntaxFormattingOptions options, SolutionServices services, CancellationToken cancellationToken)
    {
        // Simplifier doesn't work without semantic information
        return Task.FromResult(root);
    }
}
