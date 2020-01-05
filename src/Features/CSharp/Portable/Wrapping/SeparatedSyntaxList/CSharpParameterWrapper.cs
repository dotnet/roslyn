// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
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

        protected override SeparatedSyntaxList<ParameterSyntax> GetListItems(BaseParameterListSyntax listSyntax)
            => listSyntax.Parameters;

        protected override BaseParameterListSyntax TryGetApplicableList(SyntaxNode node)
            => CSharpSyntaxGenerator.GetParameterList(node);

        protected override bool PositionIsApplicable(
            SyntaxNode root, int position, SyntaxNode declaration, BaseParameterListSyntax listSyntax)
        {
            // CSharpSyntaxGenerator.GetParameterList synthesizes a parameter list for simple-lambdas.
            // In that case, we're not applicable in that list.
            if (declaration.Kind() == SyntaxKind.SimpleLambdaExpression)
            {
                return false;
            }

            var generator = CSharpSyntaxGenerator.Instance;
            var attributes = generator.GetAttributes(declaration);

            // We want to offer this feature in the header of the member.  For now, we consider
            // the header to be the part after the attributes, to the end of the parameter list.
            var firstToken = attributes?.Count > 0
                ? attributes.Last().GetLastToken().GetNextToken()
                : declaration.GetFirstToken();

            var lastToken = listSyntax.GetLastToken();

            var headerSpan = TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End);
            return headerSpan.IntersectsWith(position);
        }
    }
}
