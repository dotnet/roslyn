// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class AbstractSyntaxTriviaStructureProvider : AbstractSyntaxStructureProvider
    {
        public sealed override void CollectBlockSpans(
            Document document,
            SyntaxNode node,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
