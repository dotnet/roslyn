// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.CSharp.Formatting.Indentation;
using Microsoft.CodeAnalysis.Editor.Wrapping;

namespace Microsoft.CodeAnalysis.CSharp.Editor.Wrapping
{
    internal abstract class AbstractCSharpWrappingCodeRefactoringProvider<TListSyntax, TListItemSyntax>
        : AbstractWrappingCodeRefactoringProvider<TListSyntax, TListItemSyntax>
        where TListSyntax : SyntaxNode
        where TListItemSyntax : SyntaxNode
    {
        protected override ISynchronousIndentationService GetIndentationService()
            => new CSharpIndentationService();
    }
}
