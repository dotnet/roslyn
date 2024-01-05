// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.CodeAnalysis.Wrapping;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.SeparatedSyntaxList
{
    internal partial class CSharpParameterWrapper
        : AbstractCSharpSeparatedSyntaxListWrapper<BaseParameterListSyntax, ParameterSyntax>
    {
        protected override string Align_wrapped_items => FeaturesResources.Align_wrapped_parameters;
        protected override string Indent_all_items => FeaturesResources.Indent_all_parameters;
        protected override string Indent_wrapped_items => FeaturesResources.Indent_wrapped_parameters;
        protected override string Unwrap_all_items => FeaturesResources.Unwrap_all_parameters;
        protected override string Unwrap_and_indent_all_items => FeaturesResources.Unwrap_and_indent_all_parameters;
        protected override string Unwrap_list => FeaturesResources.Unwrap_parameter_list;
        protected override string Wrap_every_item => FeaturesResources.Wrap_every_parameter;
        protected override string Wrap_long_list => FeaturesResources.Wrap_long_parameter_list;

        public override bool Supports_UnwrapGroup_WrapFirst_IndentRest => true;
        public override bool Supports_WrapEveryGroup_UnwrapFirst => true;
        public override bool Supports_WrapLongGroup_UnwrapFirst => true;

        protected override bool ShouldMoveOpenBraceToNewLine(SyntaxWrappingOptions options)
            => false;

        protected override bool ShouldMoveCloseBraceToNewLine
            => false;

        protected override SyntaxToken FirstToken(BaseParameterListSyntax listSyntax)
            => listSyntax.GetOpenToken();

        protected override SyntaxToken LastToken(BaseParameterListSyntax listSyntax)
            => listSyntax.GetCloseToken();

        protected override SeparatedSyntaxList<ParameterSyntax> GetListItems(BaseParameterListSyntax listSyntax)
            => listSyntax.Parameters;

        protected override BaseParameterListSyntax? TryGetApplicableList(SyntaxNode node)
            => node.GetParameterList();

        protected override bool PositionIsApplicable(
            SyntaxNode root, int position, SyntaxNode declaration, bool containsSyntaxError, BaseParameterListSyntax listSyntax)
        {
            // CSharpSyntaxGenerator.GetParameterList synthesizes a parameter list for simple-lambdas.
            // In that case, we're not applicable in that list.
            if (declaration.Kind() == SyntaxKind.SimpleLambdaExpression)
                return false;

            var generator = CSharpSyntaxGenerator.Instance;
            var attributes = generator.GetAttributes(declaration);

            // We want to offer this feature in the header of the member.  For now, we consider
            // the header to be the part after the attributes, to the end of the parameter list.
            var firstToken = attributes?.Count > 0
                ? attributes.Last().GetLastToken().GetNextToken()
                : declaration.GetFirstToken();

            var lastToken = listSyntax.GetLastToken();

            var headerSpan = TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End);
            if (!headerSpan.IntersectsWith(position))
                return false;

            if (containsSyntaxError && ContainsOverlappingSyntaxError(declaration, headerSpan))
                return false;

            return true;
        }
    }
}
