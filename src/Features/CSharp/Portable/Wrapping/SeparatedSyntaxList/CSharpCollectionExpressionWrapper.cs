// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Wrapping;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.SeparatedSyntaxList;

internal sealed partial class CSharpCollectionExpressionWrapper
    : AbstractCSharpSeparatedSyntaxListWrapper<CollectionExpressionSyntax, CollectionElementSyntax>
{
    protected override string Indent_all_items => FeaturesResources.Indent_all_elements;
    protected override string Unwrap_all_items => FeaturesResources.Unwrap_all_elements;
    protected override string Unwrap_list => FeaturesResources.Unwrap_collection;
    protected override string Wrap_every_item => FeaturesResources.Wrap_collection;
    protected override string Wrap_long_list => FeaturesResources.Wrap_long_collection;

    public override bool Supports_UnwrapGroup_WrapFirst_IndentRest => false;
    public override bool Supports_WrapEveryGroup_UnwrapFirst => false;
    public override bool Supports_WrapLongGroup_UnwrapFirst => false;

    // unreachable as we explicitly declare that we don't support these scenarios.

    protected override string Align_wrapped_items => throw ExceptionUtilities.Unreachable();
    protected override string Indent_wrapped_items => throw ExceptionUtilities.Unreachable();
    protected override string Unwrap_and_indent_all_items => throw ExceptionUtilities.Unreachable();

    protected override bool ShouldMoveOpenBraceToNewLine(SyntaxWrappingOptions options)
        => ((CSharpSyntaxWrappingOptions)options).NewLinesForBracesInObjectCollectionArrayInitializers;

    protected override bool ShouldMoveCloseBraceToNewLine
        => true;

    protected override SyntaxToken FirstToken(CollectionExpressionSyntax listSyntax)
        => listSyntax.OpenBracketToken;

    protected override SyntaxToken LastToken(CollectionExpressionSyntax listSyntax)
        => listSyntax.CloseBracketToken;

    protected override SeparatedSyntaxList<CollectionElementSyntax> GetListItems(CollectionExpressionSyntax listSyntax)
        => listSyntax.Elements;

    protected override CollectionExpressionSyntax? TryGetApplicableList(SyntaxNode node)
        => node as CollectionExpressionSyntax;

    protected override bool PositionIsApplicable(SyntaxNode root, int position, SyntaxNode declaration, bool containsSyntaxError, CollectionExpressionSyntax listSyntax)
    {
        if (containsSyntaxError)
            return false;

        return listSyntax.Span.Contains(position);
    }
}
