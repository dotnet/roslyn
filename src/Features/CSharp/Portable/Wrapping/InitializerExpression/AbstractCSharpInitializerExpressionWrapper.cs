// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.Wrapping.InitializerExpression;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.InitializerExpression
{
    internal abstract class AbstractCSharpInitializerExpressionWrapper<TListSyntax, TListItemSyntax>
        : AbstractInitializerExpressionWrapper<TListSyntax, TListItemSyntax>
        where TListSyntax : SyntaxNode
        where TListItemSyntax : SyntaxNode
    {
        protected override string Indent_all_items => FeaturesResources.Indent_all_arguments;
        protected override string Unwrap_all_items => FeaturesResources.Unwrap_all_arguments;
        protected override string Unwrap_list => FeaturesResources.Unwrap_element_list;
        protected override string Wrap_every_item => FeaturesResources.Wrap_every_element;
        protected override string Wrap_long_list => FeaturesResources.Wrap_long_element_list;
        protected override bool DoWrapInitializerOpenBrace => true;

        protected AbstractCSharpInitializerExpressionWrapper() : base(CSharpIndentationService.Instance)
        {
        }
    }
}
