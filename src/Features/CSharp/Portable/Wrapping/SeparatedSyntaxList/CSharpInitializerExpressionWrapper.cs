// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.SeparatedSyntaxList
{
    internal partial class CSharpInitializerExpressionWrapper
        : AbstractCSharpSeparatedSyntaxListWrapper<InitializerExpressionSyntax, ExpressionSyntax>
    {
        protected override string Align_wrapped_items => FeaturesResources.Align_wrapped_parameters;
        protected override string Indent_all_items => FeaturesResources.Indent_all_parameters;
        protected override string Indent_wrapped_items => FeaturesResources.Indent_wrapped_parameters;
        protected override string Unwrap_all_items => FeaturesResources.Unwrap_all_parameters;
        protected override string Unwrap_and_indent_all_items => FeaturesResources.Unwrap_and_indent_all_parameters;
        protected override string Unwrap_list => FeaturesResources.Unwrap_parameter_list;
        protected override string Wrap_every_item => FeaturesResources.Wrap_every_parameter;
        protected override string Wrap_long_list => FeaturesResources.Wrap_long_parameter_list;

        public override bool Supports_UnwrapGroup_WrapFirst_IndentRest => false;
        public override bool Supports_WrapEveryGroup_UnwrapFirst => false;
        public override bool Supports_WrapLongGroup_UnwrapFirst => false;

        protected override bool ShouldMoveOpenBraceToNewLine(OptionSet options)
            => options.GetOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers);

        protected override bool ShouldMoveCloseBraceToNewLine
            => true;

        protected override SeparatedSyntaxList<ExpressionSyntax> GetListItems(InitializerExpressionSyntax listSyntax)
            => listSyntax.Expressions;

        protected override InitializerExpressionSyntax? TryGetApplicableList(SyntaxNode node)
            => node as InitializerExpressionSyntax;

        protected override bool PositionIsApplicable(SyntaxNode root, int position, SyntaxNode declaration, InitializerExpressionSyntax listSyntax)
            => listSyntax.Span.Contains(position);
    }
}
