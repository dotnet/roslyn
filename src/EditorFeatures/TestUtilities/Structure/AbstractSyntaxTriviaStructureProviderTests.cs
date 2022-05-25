// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    public abstract class AbstractSyntaxTriviaStructureProviderTests : AbstractSyntaxStructureProviderTests
    {
        internal abstract AbstractSyntaxStructureProvider CreateProvider();

        internal sealed override async Task<ImmutableArray<BlockSpan>> GetBlockSpansWorkerAsync(Document document, BlockStructureOptions options, int position)
        {
            var root = await document.GetSyntaxRootAsync();
            var trivia = root.FindTrivia(position, findInsideTrivia: true);

            var outliner = CreateProvider();
            using var actualRegions = TemporaryArray<BlockSpan>.Empty;
            outliner.CollectBlockSpans(trivia, ref actualRegions.AsRef(), options, CancellationToken.None);

            // TODO: Determine why we get null outlining spans.
            return actualRegions.ToImmutableAndClear();
        }
    }
}
