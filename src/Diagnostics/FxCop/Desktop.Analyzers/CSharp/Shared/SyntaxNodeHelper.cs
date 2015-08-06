// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Desktop.Analyzers.Common
{
    public sealed class CSharpSyntaxNodeHelper : SyntaxNodeHelper
    {
        private static CSharpSyntaxNodeHelper instance = new CSharpSyntaxNodeHelper();

        public static CSharpSyntaxNodeHelper Default { get { return instance; } }

        private CSharpSyntaxNodeHelper()
        {}

        public override ITypeSymbol GetClassDeclarationTypeSymbol(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node == null)
            {
                return null;
            }

            SyntaxKind kind = node.Kind();
            if (kind == SyntaxKind.ClassDeclaration)
            {
                return semanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)node);
            }

            return null;
        }

        public override SyntaxNode GetAssignmentLeftNode(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            SyntaxKind kind = node.Kind();
            if (kind == SyntaxKind.SimpleAssignmentExpression)
            {
                return ((AssignmentExpressionSyntax)node).Left;
            }

            if (kind == SyntaxKind.VariableDeclarator)
            {
                return ((VariableDeclaratorSyntax)node);
            }

            return null;
        }

        public override SyntaxNode GetAssignmentRightNode(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            SyntaxKind kind = node.Kind();
            if (kind == SyntaxKind.SimpleAssignmentExpression)
            {
                return ((AssignmentExpressionSyntax)node).Right;
            }

            if (kind == SyntaxKind.VariableDeclarator)
            {
                EqualsValueClauseSyntax initializer = ((VariableDeclaratorSyntax)node).Initializer;
                if (initializer != null)
                {
                    return initializer.Value;
                }
            }

            return null;
        }

        public override SyntaxNode GetMemberAccessExpressionNode(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            SyntaxKind kind = node.Kind();
            if (kind == SyntaxKind.SimpleMemberAccessExpression)
            {
                return ((MemberAccessExpressionSyntax)node).Expression;
            }

            return null;
        }

        public override SyntaxNode GetMemberAccessNameNode(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            SyntaxKind kind = node.Kind();
            if (kind == SyntaxKind.SimpleMemberAccessExpression)
            {
                return ((MemberAccessExpressionSyntax)node).Name;
            }

            return null;
        }

        public override SyntaxNode GetInvocationExpressionNode(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            SyntaxKind kind = node.Kind();
            if (kind != SyntaxKind.InvocationExpression)
            {
                return null;
            }

            return ((InvocationExpressionSyntax)node).Expression;
        }

        public override SyntaxNode GetCallTargetNode(SyntaxNode node)
        {
            if (node != null)
            {
                SyntaxKind kind = node.Kind();
                if (kind == SyntaxKind.InvocationExpression)
                {
                    var callExpr = ((InvocationExpressionSyntax)node).Expression;
                    return GetMemberAccessNameNode(callExpr) ?? callExpr;
                }
                else if (kind == SyntaxKind.ObjectCreationExpression)
                {
                    return ((ObjectCreationExpressionSyntax)node).Type;
                }
            }

            return null;
        }

        public override SyntaxNode GetDefaultValueForAnOptionalParameter(SyntaxNode declNode, int paramIndex)
        {
            var methodDecl = declNode as BaseMethodDeclarationSyntax;
            if (methodDecl != null)
            {
                ParameterListSyntax paramList = methodDecl.ParameterList;
                if (paramIndex < paramList.Parameters.Count)
                {
                    EqualsValueClauseSyntax equalsValueNode = paramList.Parameters[paramIndex].Default;
                    if (equalsValueNode != null)
                    {
                        return equalsValueNode.Value;
                    }
                }
            }
            return null;
        }

        protected override IEnumerable<SyntaxNode> GetCallArgumentExpressionNodes(SyntaxNode node, CallKind callKind)
        {
            if (node != null)
            {
                ArgumentListSyntax argList = null;
                SyntaxKind kind = node.Kind();
                if ((kind == SyntaxKind.InvocationExpression) && ((callKind & CallKind.Invocation) != 0))
                {
                    var invocationNode = (InvocationExpressionSyntax)node;
                    argList = invocationNode.ArgumentList;
                }
                else if ((kind == SyntaxKind.ObjectCreationExpression) && ((callKind & CallKind.ObjectCreation) != 0))
                {
                    var invocationNode = (ObjectCreationExpressionSyntax)node;
                    argList = invocationNode.ArgumentList;
                }
                if (argList != null)
                {
                    return argList.Arguments.Select(arg => arg.Expression);
                }
            }

            return Enumerable.Empty<SyntaxNode>();
        }

        public override IEnumerable<SyntaxNode> GetObjectInitializerExpressionNodes(SyntaxNode node)
        {
            var empty = Enumerable.Empty<SyntaxNode>();
            if (node == null)
            {
                return empty;
            }

            SyntaxKind kind = node.Kind();
            if (kind != SyntaxKind.ObjectCreationExpression)
            {
                return empty;
            }

            var objectCreationNode = (ObjectCreationExpressionSyntax)node;
            if (objectCreationNode.Initializer == null)
            {
                return empty;
            }

            return objectCreationNode.Initializer.Expressions;
        }

        public override bool IsMethodInvocationNode(SyntaxNode node)
        { 
            if (node == null)
            {
                return false;
            }
            SyntaxKind kind = node.Kind();
            return kind == SyntaxKind.InvocationExpression || kind == SyntaxKind.ObjectCreationExpression;
        }
    }
}
