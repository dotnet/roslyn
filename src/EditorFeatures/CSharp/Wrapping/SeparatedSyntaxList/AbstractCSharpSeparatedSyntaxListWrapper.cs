// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.CSharp.Formatting.Indentation;
using Microsoft.CodeAnalysis.Editor.Wrapping.SeparatedSyntaxList;

namespace Microsoft.CodeAnalysis.CSharp.Editor.Wrapping.SeparatedSyntaxList
{
    internal abstract class AbstractCSharpSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax>
        : AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax>
        where TListSyntax : SyntaxNode
        where TListItemSyntax : SyntaxNode
    {
        // In our scenario we want to control all formatting ourselves. So tell the indenter to not
        // depend on a formatter being available so it does all the work to figure out indentation
        // itself.
        protected override IBlankLineIndentationService GetIndentationService()
            => new CSharpIndentationService();
    }
}
