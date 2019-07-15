// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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

        protected override SeparatedSyntaxList<ArgumentSyntax> GetListItems(BaseArgumentListSyntax listSyntax)
            => listSyntax.Arguments;

        protected override BaseArgumentListSyntax TryGetApplicableList(SyntaxNode node)
            => node switch
            {
                InvocationExpressionSyntax invocationExpression => invocationExpression.ArgumentList,
                ElementAccessExpressionSyntax elementAccessExpression => elementAccessExpression.ArgumentList,
                ObjectCreationExpressionSyntax objectCreationExpression => objectCreationExpression.ArgumentList,
                ConstructorInitializerSyntax constructorInitializer => constructorInitializer.ArgumentList,
                _ => (BaseArgumentListSyntax)null,
            };

        protected override bool PositionIsApplicable(
            SyntaxNode root, int position,
            SyntaxNode declaration, BaseArgumentListSyntax listSyntax)
        {
            var startToken = listSyntax.GetFirstToken();

            if (declaration is InvocationExpressionSyntax ||
                declaration is ElementAccessExpressionSyntax)
            {
                // If we have something like  Foo(...)  or  this.Foo(...)  allow anywhere in the Foo(...)
                // section.
                var expr = (declaration as InvocationExpressionSyntax)?.Expression ??
                           (declaration as ElementAccessExpressionSyntax).Expression;
                var name = TryGetInvokedName(expr);

                startToken = name == null ? listSyntax.GetFirstToken() : name.GetFirstToken();
            }
            else if (declaration is ObjectCreationExpressionSyntax)
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
            {
                return false;
            }

            // allow anywhere in the arg list, as long we don't end up walking through something
            // complex like a lambda/anonymous function.
            var token = root.FindToken(position);
            if (token.Parent.Ancestors().Contains(listSyntax))
            {
                for (var current = token.Parent; current != listSyntax; current = current.Parent)
                {
                    if (CSharpSyntaxFactsService.Instance.IsAnonymousFunction(current))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private ExpressionSyntax TryGetInvokedName(ExpressionSyntax expr)
        {
            // `Foo(...)`.  Allow up through the 'Foo' portion
            if (expr is NameSyntax name)
            {
                return name;
            }

            // `this[...]`. Allow up throught the 'this' token.
            if (expr is ThisExpressionSyntax || expr is BaseExpressionSyntax)
            {
                return expr;
            }

            // expr.Foo(...) or expr?.Foo(...)
            // All up through the 'Foo' portion.
            //
            // Otherwise, only allow in the arg list.
            return (expr as MemberAccessExpressionSyntax)?.Name ??
                   (expr as MemberBindingExpressionSyntax)?.Name;
        }
    }
}
