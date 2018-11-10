// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Wrapping;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Editor.Wrapping
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpArgumentWrappingCodeRefactoringProvider
        : AbstractCSharpWrappingCodeRefactoringProvider<BaseArgumentListSyntax, ArgumentSyntax>
    {
        protected override string ListName => FeaturesResources.argument_list;
        protected override string ItemNamePlural => FeaturesResources.arguments;
        protected override string ItemNameSingular => FeaturesResources.argument;

        protected override SeparatedSyntaxList<ArgumentSyntax> GetListItems(BaseArgumentListSyntax listSyntax)
            => listSyntax.Arguments;

        protected override BaseArgumentListSyntax GetApplicableList(SyntaxNode node)
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocationExpression: return invocationExpression.ArgumentList;
                case ElementAccessExpressionSyntax elementAccessExpression: return elementAccessExpression.ArgumentList;
                case ObjectCreationExpressionSyntax objectCreationExpression: return objectCreationExpression.ArgumentList;
                case ConstructorInitializerSyntax constructorInitializer: return constructorInitializer.ArgumentList;
            }

            return null;
        }

        protected override bool PositionIsApplicable(
            SyntaxNode root, int position,
            SyntaxNode declaration, BaseArgumentListSyntax listSyntax)
        {
            var startToken = listSyntax.GetFirstToken();

            // If we have something like  Foo(...)  or  this.Foo(...)  allow anywhere in the Foo(...)
            // section.
            if (declaration is InvocationExpressionSyntax ||
                declaration is ElementAccessExpressionSyntax)
            {
                var expr = (declaration as InvocationExpressionSyntax)?.Expression ??
                           (declaration as ElementAccessExpressionSyntax).Expression;
                var name = GetNameExpr(expr);

                startToken = name == null ? listSyntax.GetFirstToken() : name.GetFirstToken();
            }
            else if (declaration is ObjectCreationExpressionSyntax objectCreation)
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

        private ExpressionSyntax GetNameExpr(ExpressionSyntax expr)
        {
            if (expr is NameSyntax name)
            {
                return name;
            }

            if (expr is ThisExpressionSyntax || expr is BaseExpressionSyntax)
            {
                return expr;
            }

            return (expr as MemberAccessExpressionSyntax)?.Name ??
                   (expr as MemberBindingExpressionSyntax)?.Name;
        }
    }
}
