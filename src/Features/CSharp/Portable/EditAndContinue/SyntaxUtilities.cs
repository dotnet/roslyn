// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue
{
    internal static class SyntaxUtilities
    {
        public static SyntaxNode TryGetMethodDeclarationBody(SyntaxNode node)
        {
            static SyntaxNode BlockOrExpression(BlockSyntax blockBodyOpt, ArrowExpressionClauseSyntax expressionBodyOpt)
                => (SyntaxNode)blockBodyOpt ?? expressionBodyOpt?.Expression;

            SyntaxNode result;
            switch (node.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    var methodDeclaration = (MethodDeclarationSyntax)node;
                    result = BlockOrExpression(methodDeclaration.Body, methodDeclaration.ExpressionBody);
                    break;

                case SyntaxKind.ConversionOperatorDeclaration:
                    var conversionDeclaration = (ConversionOperatorDeclarationSyntax)node;
                    result = BlockOrExpression(conversionDeclaration.Body, conversionDeclaration.ExpressionBody);
                    break;

                case SyntaxKind.OperatorDeclaration:
                    var operatorDeclaration = (OperatorDeclarationSyntax)node;
                    result = BlockOrExpression(operatorDeclaration.Body, operatorDeclaration.ExpressionBody);
                    break;

                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                    var accessorDeclaration = (AccessorDeclarationSyntax)node;
                    result = BlockOrExpression(accessorDeclaration.Body, accessorDeclaration.ExpressionBody);
                    break;

                case SyntaxKind.ConstructorDeclaration:
                    var constructorDeclaration = (ConstructorDeclarationSyntax)node;
                    result = BlockOrExpression(constructorDeclaration.Body, constructorDeclaration.ExpressionBody);
                    break;

                case SyntaxKind.DestructorDeclaration:
                    var destructorDeclaration = (DestructorDeclarationSyntax)node;
                    result = BlockOrExpression(destructorDeclaration.Body, destructorDeclaration.ExpressionBody);
                    break;

                case SyntaxKind.PropertyDeclaration:
                    var propertyDeclaration = (PropertyDeclarationSyntax)node;
                    result = propertyDeclaration.Initializer?.Value;
                    break;

                case SyntaxKind.ArrowExpressionClause:
                    // We associate the body of expression-bodied property/indexer with the ArrowExpressionClause
                    // since that's the syntax node associated with the getter symbol.
                    // The property/indexer itself is considered to not have a body unless the property has an initializer.
                    result = node.Parent.IsKind(SyntaxKind.PropertyDeclaration, SyntaxKind.IndexerDeclaration) ?
                        ((ArrowExpressionClauseSyntax)node).Expression : null;
                    break;

                default:
                    return null;
            }

            if (result != null)
            {
                AssertIsBody(result, allowLambda: false);
            }

            return result;
        }

        [Conditional("DEBUG")]
        public static void AssertIsBody(SyntaxNode syntax, bool allowLambda)
        {
            // lambda/query
            if (LambdaUtilities.IsLambdaBody(syntax))
            {
                Debug.Assert(allowLambda);
                Debug.Assert(syntax is ExpressionSyntax || syntax is BlockSyntax);
                return;
            }

            // block body
            if (syntax is BlockSyntax)
            {
                return;
            }

            // expression body
            if (syntax is ExpressionSyntax && syntax.Parent is ArrowExpressionClauseSyntax)
            {
                return;
            }

            // field initializer
            if (syntax is ExpressionSyntax && syntax.Parent.Parent is VariableDeclaratorSyntax)
            {
                return;
            }

            // property initializer
            if (syntax is ExpressionSyntax && syntax.Parent.Parent is PropertyDeclarationSyntax)
            {
                return;
            }

            Debug.Assert(false);
        }

        public static void FindLeafNodeAndPartner(SyntaxNode leftRoot, int leftPosition, SyntaxNode rightRoot, out SyntaxNode leftNode, out SyntaxNode rightNodeOpt)
        {
            leftNode = leftRoot;
            rightNodeOpt = rightRoot;
            while (true)
            {
                if (rightNodeOpt != null && leftNode.RawKind != rightNodeOpt.RawKind)
                {
                    rightNodeOpt = null;
                }

                var leftChild = leftNode.ChildThatContainsPosition(leftPosition, out var childIndex);
                if (leftChild.IsToken)
                {
                    return;
                }

                if (rightNodeOpt != null)
                {
                    var rightNodeChildNodesAndTokens = rightNodeOpt.ChildNodesAndTokens();
                    if (childIndex >= 0 && childIndex < rightNodeChildNodesAndTokens.Count)
                    {
                        rightNodeOpt = rightNodeChildNodesAndTokens[childIndex].AsNode();
                    }
                    else
                    {
                        rightNodeOpt = null;
                    }
                }

                leftNode = leftChild.AsNode();
            }
        }

        public static SyntaxNode FindPartner(SyntaxNode leftRoot, SyntaxNode rightRoot, SyntaxNode leftNode)
        {
            // Finding a partner of a zero-width node is complicated and not supported atm:
            Debug.Assert(leftNode.FullSpan.Length > 0);
            Debug.Assert(leftNode.SyntaxTree == leftRoot.SyntaxTree);

            var originalLeftNode = leftNode;
            var leftPosition = leftNode.SpanStart;
            leftNode = leftRoot;
            var rightNode = rightRoot;

            while (leftNode != originalLeftNode)
            {
                Debug.Assert(leftNode.RawKind == rightNode.RawKind);
                var leftChild = leftNode.ChildThatContainsPosition(leftPosition, out var childIndex);

                // Can only happen when searching for zero-width node.
                Debug.Assert(!leftChild.IsToken);

                rightNode = rightNode.ChildNodesAndTokens()[childIndex].AsNode();
                leftNode = leftChild.AsNode();
            }

            return rightNode;
        }

        public static bool Any(TypeParameterListSyntax listOpt)
            => listOpt != null && listOpt.ChildNodesAndTokens().Count != 0;

        public static SyntaxNode TryGetEffectiveGetterBody(SyntaxNode declaration)
        {
            if (declaration.IsKind(SyntaxKind.PropertyDeclaration, out PropertyDeclarationSyntax property))
            {
                return TryGetEffectiveGetterBody(property.ExpressionBody, property.AccessorList);
            }

            if (declaration.IsKind(SyntaxKind.IndexerDeclaration, out IndexerDeclarationSyntax indexer))
            {
                return TryGetEffectiveGetterBody(indexer.ExpressionBody, indexer.AccessorList);
            }

            return null;
        }

        public static SyntaxNode TryGetEffectiveGetterBody(ArrowExpressionClauseSyntax propertyBody, AccessorListSyntax accessorList)
        {
            if (propertyBody != null)
            {
                return propertyBody.Expression;
            }

            var firstGetter = accessorList?.Accessors.Where(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)).FirstOrDefault();
            if (firstGetter == null)
            {
                return null;
            }

            return (SyntaxNode)firstGetter.Body ?? firstGetter.ExpressionBody?.Expression;
        }

        public static SyntaxTokenList? TryGetFieldOrPropertyModifiers(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.FieldDeclaration, out FieldDeclarationSyntax fieldDecl))
                return fieldDecl.Modifiers;

            if (node.IsKind(SyntaxKind.PropertyDeclaration, out PropertyDeclarationSyntax propertyDecl))
                return propertyDecl.Modifiers;

            return null;
        }

        public static bool IsParameterlessConstructor(SyntaxNode declaration)
        {
            if (!declaration.IsKind(SyntaxKind.ConstructorDeclaration, out ConstructorDeclarationSyntax ctor))
            {
                return false;
            }

            return ctor.ParameterList.Parameters.Count == 0;
        }

        public static bool HasBackingField(PropertyDeclarationSyntax property)
        {
            if (property.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
                property.Modifiers.Any(SyntaxKind.ExternKeyword))
            {
                return false;
            }

            return property.ExpressionBody == null
                && property.AccessorList.Accessors.Any(e => e.Body == null);
        }

        /// <summary>
        /// True if the specified declaration node is an async method, anonymous function, lambda, local function.
        /// </summary>
        public static bool IsAsyncDeclaration(SyntaxNode declaration)
        {
            // lambdas and anonymous functions
            if (declaration is AnonymousFunctionExpressionSyntax anonymousFunction)
            {
                return anonymousFunction.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
            }

            // expression bodied methods/local functions:
            if (declaration.IsKind(SyntaxKind.ArrowExpressionClause))
            {
                declaration = declaration.Parent;
            }

            return declaration switch
            {
                MethodDeclarationSyntax method => method.Modifiers.Any(SyntaxKind.AsyncKeyword),
                LocalFunctionStatementSyntax localFunction => localFunction.Modifiers.Any(SyntaxKind.AsyncKeyword),
                _ => false
            };
        }

        /// <summary>
        /// Returns a list of all await expressions, await foreach statements, await using declarations and yield statements in the given body,
        /// in the order in which they occur.
        /// </summary>
        /// <returns>
        /// <see cref="AwaitExpressionSyntax"/> for await expressions,
        /// <see cref="YieldStatementSyntax"/> for yield break and yield return statements,
        /// <see cref="CommonForEachStatementSyntax"/> for await foreach statements,
        /// <see cref="VariableDeclaratorSyntax"/> for await using declarators.
        /// </returns>
        public static IEnumerable<SyntaxNode> GetSuspensionPoints(SyntaxNode body)
            => body.DescendantNodesAndSelf(LambdaUtilities.IsNotLambda).Where(IsSuspensionPoint);

        public static bool IsSuspensionPoint(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.AwaitExpression) || node.IsKind(SyntaxKind.YieldBreakStatement) || node.IsKind(SyntaxKind.YieldReturnStatement))
            {
                return true;
            }

            // await foreach statement translates to two suspension points: await MoveNextAsync and await DisposeAsync
            if (node is CommonForEachStatementSyntax foreachStatement && foreachStatement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword))
            {
                return true;
            }

            // each declarator in the declaration translates to a suspension point: await DisposeAsync
            if (node.IsKind(SyntaxKind.VariableDeclarator) &&
                node.Parent.Parent.IsKind(SyntaxKind.LocalDeclarationStatement, out LocalDeclarationStatementSyntax localDecl) &&
                localDecl.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword))
            {
                return true;
            }

            return false;
        }
    }
}
