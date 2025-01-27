// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.Wrapping.SeparatedSyntaxList;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.SeparatedSyntaxList;

internal abstract class AbstractCSharpSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax>
    : AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax>
    where TListSyntax : SyntaxNode
    where TListItemSyntax : SyntaxNode
{
    protected AbstractCSharpSeparatedSyntaxListWrapper()
        : base(CSharpIndentationService.Instance)
    {
    }
}
