// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class AbstractSyntaxStructureProvider
    {
        public abstract void CollectBlockSpans(
            Document document,
            SyntaxNode node,
            ref TemporaryArray<BlockSpan> spans,
            CancellationToken cancellationToken);

        public abstract void CollectBlockSpans(
            Document document,
            SyntaxTrivia trivia,
            ref TemporaryArray<BlockSpan> spans,
            CancellationToken cancellationToken);
    }
}
