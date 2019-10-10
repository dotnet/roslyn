using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.SeparatedSyntaxList
{
    internal class CSharpInitializerExpressionWrapper : AbstractCSharpSeparatedSyntaxListWrapper<InitializerExpressionSyntax, ExpressionSyntax>
    {
        protected override string Align_wrapped_items => FeaturesResources.Align_wrapped_elements;
        protected override string Indent_all_items => FeaturesResources.Indent_all_elements;
        protected override string Indent_wrapped_items => FeaturesResources.Indent_wrapped_elements;
        protected override string Unwrap_all_items => FeaturesResources.Unwrap_all_elements;
        protected override string Unwrap_and_indent_all_items => FeaturesResources.Unwrap_and_indent_all_elements;
        protected override string Unwrap_list => FeaturesResources.Unwrap_element_list;
        protected override string Wrap_every_item => FeaturesResources.Wrap_every_element;
        protected override string Wrap_long_list => FeaturesResources.Wrap_long_element_list;

        protected override SeparatedSyntaxList<ExpressionSyntax> GetListItems(InitializerExpressionSyntax listSyntax)
            => listSyntax.Expressions;

        protected override bool PositionIsApplicable(SyntaxNode root, int position, SyntaxNode declaration, InitializerExpressionSyntax listSyntax)
            => true;

        protected override InitializerExpressionSyntax TryGetApplicableList(SyntaxNode node)
            => node switch
            {
                InitializerExpressionSyntax initializerExpressionSyntax => initializerExpressionSyntax,
                _ => null,
            };
    }
}
