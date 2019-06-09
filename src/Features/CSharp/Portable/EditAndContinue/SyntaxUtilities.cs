﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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
            SyntaxNode BlockOrExpression(BlockSyntax blockBodyOpt, ArrowExpressionClauseSyntax expressionBodyOpt)
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
                    if (propertyDeclaration.Initializer != null)
                    {
                        result = propertyDeclaration.Initializer.Value;
                        break;
                    }

                    return propertyDeclaration.ExpressionBody?.Expression;

                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)node).ExpressionBody?.Expression;

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

            SyntaxNode originalLeftNode = leftNode;
            int leftPosition = leftNode.SpanStart;
            leftNode = leftRoot;
            SyntaxNode rightNode = rightRoot;

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
        {
            return listOpt != null && listOpt.ChildNodesAndTokens().Count != 0;
        }

        public static SyntaxNode TryGetEffectiveGetterBody(SyntaxNode declaration)
        {
            if (declaration.IsKind(SyntaxKind.PropertyDeclaration))
            {
                var property = (PropertyDeclarationSyntax)declaration;
                return TryGetEffectiveGetterBody(property.ExpressionBody, property.AccessorList);
            }

            if (declaration.IsKind(SyntaxKind.IndexerDeclaration))
            {
                var indexer = (IndexerDeclarationSyntax)declaration;
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
            if (node.IsKind(SyntaxKind.FieldDeclaration))
            {
                return ((FieldDeclarationSyntax)node).Modifiers;
            }

            if (node.IsKind(SyntaxKind.PropertyDeclaration))
            {
                return ((PropertyDeclarationSyntax)node).Modifiers;
            }

            return null;
        }

        public static bool IsMethod(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                    return true;

                case SyntaxKind.IndexerDeclaration:
                    // expression bodied indexer
                    return ((IndexerDeclarationSyntax)declaration).ExpressionBody != null;

                default:
                    return false;
            }
        }

        public static bool IsParameterlessConstructor(SyntaxNode declaration)
        {
            if (!declaration.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                return false;
            }

            var ctor = (ConstructorDeclarationSyntax)declaration;
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

        public static bool IsAsyncMethodOrLambda(SyntaxNode declaration)
        {
            if (declaration is AnonymousFunctionExpressionSyntax anonymousFunction)
            {
                return anonymousFunction.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
            }

            // expression bodied methods:
            if (declaration.IsKind(SyntaxKind.ArrowExpressionClause))
            {
                declaration = declaration.Parent;
            }

            if (!declaration.IsKind(SyntaxKind.MethodDeclaration))
            {
                return false;
            }

            var method = (MethodDeclarationSyntax)declaration;
            return method.Modifiers.Any(SyntaxKind.AsyncKeyword);
        }

        public static ImmutableArray<SyntaxNode> GetAwaitExpressions(SyntaxNode body)
        {
            // skip lambda bodies:
            return ImmutableArray.CreateRange(body.DescendantNodesAndSelf(LambdaUtilities.IsNotLambda).Where(n => n.IsKind(SyntaxKind.AwaitExpression)));
        }

        public static ImmutableArray<SyntaxNode> GetYieldStatements(SyntaxNode body)
        {
            // lambdas and expression-bodied methods can't be iterators:
            if (!body.Parent.IsKind(SyntaxKind.MethodDeclaration))
            {
                return ImmutableArray<SyntaxNode>.Empty;
            }

            // enumerate statements:
            return ImmutableArray.CreateRange(body.DescendantNodes(n => !(n is ExpressionSyntax))
                   .Where(n => n.IsKind(SyntaxKind.YieldBreakStatement) || n.IsKind(SyntaxKind.YieldReturnStatement)));
        }

        public static bool IsIteratorMethod(SyntaxNode declaration)
        {
            // lambdas and expression-bodied methods can't be iterators:
            if (!declaration.IsKind(SyntaxKind.MethodDeclaration))
            {
                return false;
            }

            // enumerate statements:
            return declaration.DescendantNodes(n => !(n is ExpressionSyntax))
                   .Any(n => n.IsKind(SyntaxKind.YieldBreakStatement) || n.IsKind(SyntaxKind.YieldReturnStatement));
        }
    }
}
