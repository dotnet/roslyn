// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers
{
    internal static class SemanticTokensRange
    {
        public static Task<int[]> GetSemanticTokensAsync(
            Document document,
            ImmutableArray<LinePositionSpan> spans,
            bool supportsVisualStudioExtensions,
            CancellationToken cancellationToken)
            => SemanticTokensHelpers.HandleRequestHelperAsync(
                document,
                spans,
                supportsVisualStudioExtensions,
                ClassificationOptions.Default,
                cancellationToken);
    }
}
