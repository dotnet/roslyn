// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal class EnumMemberDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<EnumMemberDeclarationSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        EnumMemberDeclarationSyntax enumMemberDeclaration,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        CSharpStructureHelpers.CollectCommentBlockSpans(enumMemberDeclaration, spans, options);
    }
}
