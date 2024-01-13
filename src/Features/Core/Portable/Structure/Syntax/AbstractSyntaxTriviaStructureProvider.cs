// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class AbstractSyntaxTriviaStructureProvider : AbstractSyntaxStructureProvider
    {
        public sealed override void CollectBlockSpans(
            SyntaxToken previousToken,
            SyntaxNode node,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptions options,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
