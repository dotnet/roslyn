// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class AbstractSyntaxStructureProvider
    {
        public abstract void CollectBlockSpans(
            Document document,
            SyntaxNode node,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken);

        public abstract void CollectBlockSpans(
            Document document,
            SyntaxTrivia trivia,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken);
    }
}
