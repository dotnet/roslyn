// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.Editor.Wrapping.SeparatedSyntaxList;

namespace Microsoft.CodeAnalysis.CSharp.Editor.Wrapping.SeparatedSyntaxList
{
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
}
