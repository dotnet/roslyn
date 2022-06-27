// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Wrapping;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.SeparatedSyntaxList
{
    internal partial class CSharpArgumentWrapper
        : AbstractCSharpSeparatedSyntaxListWrapper<BaseArgumentListSyntax, ArgumentSyntax>
    {
        protected override string Align_wrapped_items => FeaturesResources.Align_wrapped_arguments;
        protected override string Indent_all_items => FeaturesResources.Indent_all_arguments;
        protected override string Indent_wrapped_items => FeaturesResources.Indent_wrapped_arguments;
        protected override string Unwrap_all_items => FeaturesResources.Unwrap_all_arguments;
        protected override string Unwrap_and_indent_all_items => FeaturesResources.Unwrap_and_indent_all_arguments;
        protected override string Unwrap_list => FeaturesResources.Unwrap_argument_list;
        protected override string Wrap_every_item => FeaturesResources.Wrap_every_argument;
        protected override string Wrap_long_list => FeaturesResources.Wrap_long_argument_list;

        public override bool Supports_UnwrapGroup_WrapFirst_IndentRest => true;
        public override bool Supports_WrapEveryGroup_UnwrapFirst => true;
        public override bool Supports_WrapLongGroup_UnwrapFirst => true;

        protected override bool ShouldMoveOpenBraceToNewLine(SyntaxWrappingOptions options)
            => false;

        protected override bool ShouldMoveCloseBraceToNewLine
            => false;

        protected override SeparatedSyntaxList<ArgumentSyntax> GetListItems(BaseArgumentListSyntax listSyntax)
            => listSyntax.Arguments;

        protected override BaseArgumentListSyntax? TryGetApplicableList(SyntaxNode node)
            => node switch
            {
                InvocationExpressionSyntax invocationExpression => invocationExpression.ArgumentList,
                ElementAccessExpressionSyntax elementAccessExpression => elementAccessExpression.ArgumentList,
                BaseObjectCreationExpressionSyntax objectCreationExpression => objectCreationExpression.ArgumentList,
                ConstructorInitializerSyntax constructorInitializer => constructorInitializer.ArgumentList,
                _ => null,
            };

        protected override bool PositionIsApplicable(
            SyntaxNode root,
            int position,
            SyntaxNode declaration,
            bool containsSyntaxError,
            BaseArgumentListSyntax listSyntax)
        {
            if (containsSyntaxError)
                return false;

            var startToken = listSyntax.GetFirstToken();

            if (declaration is InvocationExpressionSyntax or ElementAccessExpressionSyntax)
            {
                // If we have something like  Foo(...)  or  this.Foo(...)  allow anywhere in the Foo(...)
                // section.
                var expr = (declaration as InvocationExpressionSyntax)?.Expression ??
                           ((ElementAccessExpressionSyntax)declaration).Expression;
                var name = TryGetInvokedName(expr);

                startToken = name == null ? listSyntax.GetFirstToken() : name.GetFirstToken();
            }
            else if (declaration is BaseObjectCreationExpressionSyntax)
            {
                // allow anywhere in `new Foo(...)`
                startToken = declaration.GetFirstToken();
            }
            else if (declaration is ConstructorInitializerSyntax constructorInitializer)
            {
                // allow anywhere in `this(...)` or `base(...)`
                startToken = constructorInitializer.ThisOrBaseKeyword;
            }

            var endToken = listSyntax.GetLastToken();
            var span = TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End);
            if (!span.IntersectsWith(position))
                return false;

            // allow anywhere in the arg list, as long we don't end up walking through something
            // complex like a lambda/anonymous function.
            var token = root.FindToken(position);
            if (token.GetRequiredParent().Ancestors().Contains(listSyntax))
            {
                for (var current = token.Parent; current != listSyntax; current = current?.Parent)
                {
                    if (CSharpSyntaxFacts.Instance.IsAnonymousFunctionExpression(current))
                        return false;
                }
            }

            return true;
        }

        private static ExpressionSyntax? TryGetInvokedName(ExpressionSyntax expr)
        {
            // `Foo(...)`.  Allow up through the 'Foo' portion
            if (expr is NameSyntax name)
                return name;

            // `this[...]`. Allow up through the 'this' token.
            if (expr is ThisExpressionSyntax or BaseExpressionSyntax)
                return expr;

            // expr.Foo(...) or expr?.Foo(...)
            // All up through the 'Foo' portion.
            //
            // Otherwise, only allow in the arg list.
            return (expr as MemberAccessExpressionSyntax)?.Name ??
                   (expr as MemberBindingExpressionSyntax)?.Name;
        }
    }
}
