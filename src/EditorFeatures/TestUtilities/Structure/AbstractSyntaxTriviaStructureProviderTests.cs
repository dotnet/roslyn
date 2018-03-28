// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    public abstract class AbstractSyntaxTriviaStructureProviderTests : AbstractSyntaxStructureProviderTests
    {
        internal abstract AbstractSyntaxStructureProvider CreateProvider();

        internal sealed override async Task<ImmutableArray<BlockSpan>> GetBlockSpansWorkerAsync(Document document, int position)
        {
            var root = await document.GetSyntaxRootAsync();
            var trivia = root.FindTrivia(position, findInsideTrivia: true);

            var outliner = CreateProvider();
            var actualRegions = ArrayBuilder<BlockSpan>.GetInstance();
            outliner.CollectBlockSpans(document, trivia, actualRegions, CancellationToken.None);

            // TODO: Determine why we get null outlining spans.
            return actualRegions.ToImmutableAndFree();
        }
    }
}
