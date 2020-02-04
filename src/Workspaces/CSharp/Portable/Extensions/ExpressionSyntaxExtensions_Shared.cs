// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class ExpressionSyntaxExtensions
    {
        public static bool IsRightSideOfQualifiedName(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.QualifiedName) && ((QualifiedNameSyntax)expression.Parent).Right == expression;
        }

        public static bool IsMemberAccessExpressionName(this ExpressionSyntax expression)
        {
            return (expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) && ((MemberAccessExpressionSyntax)expression.Parent).Name == expression) ||
                   IsMemberBindingExpressionName(expression);
        }

        private static bool IsMemberBindingExpressionName(this ExpressionSyntax expression)
        {
            return expression.IsParentKind(SyntaxKind.MemberBindingExpression) &&
                ((MemberBindingExpressionSyntax)expression.Parent).Name == expression;
        }

        public static bool IsInConstantContext(this ExpressionSyntax expression)
        {
            if (expression.GetAncestor<ParameterSyntax>() != null)
            {
                return true;
            }

            var attributeArgument = expression.GetAncestor<AttributeArgumentSyntax>();
            if (attributeArgument != null)
            {
                if (attributeArgument.NameEquals == null ||
                    expression != attributeArgument.NameEquals.Name)
                {
                    return true;
                }
            }

            if (expression.IsParentKind(SyntaxKind.ConstantPattern))
            {
                return true;
            }

            // note: the above list is not intended to be exhaustive.  If more cases
            // are discovered that should be considered 'constant' contexts in the 
            // language, then this should be updated accordingly.
            return false;
        }

        public static bool IsAttributeNamedArgumentIdentifier(this ExpressionSyntax expression)
        {
            var nameEquals = expression?.Parent as NameEqualsSyntax;
            return nameEquals.IsParentKind(SyntaxKind.AttributeArgument);
        }

        public static ExpressionSyntax Parenthesize(
            this ExpressionSyntax expression, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true)
        {
            // a 'ref' expression should never be parenthesized.  It fundamentally breaks the code.
            // This is because, from the language's perspective there is no such thing as a ref
            // expression.  instead, there are constructs like ```return ref expr``` or 
            // ```x ? ref expr1 : ref expr2```, or ```ref int a = ref expr``` in these cases, the 
            // ref's do not belong to the exprs, but instead belong to the parent construct. i.e.
            // ```return ref``` or ``` ? ref  ... : ref ... ``` or ``` ... = ref ...```.  For 
            // parsing convenience, and to prevent having to update all these constructs, we settled
            // on a ref-expression node.  But this node isn't a true expression that be operated
            // on like with everything else.
            if (expression.IsKind(SyntaxKind.RefExpression))
            {
                return expression;
            }

            var result = ParenthesizeWorker(expression, includeElasticTrivia);
            return addSimplifierAnnotation
                ? result.WithAdditionalAnnotations(Simplifier.Annotation)
                : result;
        }

        private static ExpressionSyntax ParenthesizeWorker(
            this ExpressionSyntax expression, bool includeElasticTrivia)
        {
            var withoutTrivia = expression.WithoutTrivia();
            var parenthesized = includeElasticTrivia
                ? SyntaxFactory.ParenthesizedExpression(withoutTrivia)
                : SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.OpenParenToken, SyntaxTriviaList.Empty),
                    withoutTrivia,
                    SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.CloseParenToken, SyntaxTriviaList.Empty));

            return parenthesized.WithTriviaFrom(expression);
        }
    }
}
