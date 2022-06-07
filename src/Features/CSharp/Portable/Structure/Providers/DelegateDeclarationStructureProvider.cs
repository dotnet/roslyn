// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class DelegateDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<DelegateDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            SyntaxToken previousToken,
            DelegateDeclarationSyntax delegateDeclaration,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptions options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(delegateDeclaration, ref spans, options);
        }
    }
}
