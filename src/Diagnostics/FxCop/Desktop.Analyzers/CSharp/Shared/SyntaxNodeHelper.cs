// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Desktop.Analyzers.Common
{
    public sealed class CSharpSyntaxNodeHelper : SyntaxNodeHelper
    {
        private static CSharpSyntaxNodeHelper instance = new CSharpSyntaxNodeHelper();

        public static CSharpSyntaxNodeHelper Default { get { return instance; } }

        private CSharpSyntaxNodeHelper()
        {
        }

        public override bool ContainsMethodCall(SyntaxNode node, Func<string, bool> predicate)
        {
            if (node == null)
            {
                return false;
            }

            return node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(
                (InvocationExpressionSyntax child) =>
                {
                    return child.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(name => predicate(name.Identifier.ValueText));
                });
        }

        public override IMethodSymbol GetCallerMethodSymbol(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node == null)
            {
                return null;
            }

            MethodDeclarationSyntax declaration = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();           
            if (declaration != null)
            {
                return semanticModel.GetDeclaredSymbol(declaration);
            }

            ConstructorDeclarationSyntax contructor = node.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            if (contructor != null)
            {
                return semanticModel.GetDeclaredSymbol(contructor);
            }

            return null;
        }

        public override ITypeSymbol GetEnclosingTypeSymbol(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node == null)
            {
                return null;
            }

            ClassDeclarationSyntax declaration = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (declaration == null)
            {
                return null;
            }

            return semanticModel.GetDeclaredSymbol(declaration);
        }

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

        public override bool IsObjectCreationExpressionUnderFieldDeclaration(SyntaxNode node)
        {
            return node != null &&
                   node.Kind() == SyntaxKind.ObjectCreationExpression &&
                   node.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().FirstOrDefault() != null;
        }

        public override SyntaxNode GetVariableDeclaratorOfAFieldDeclarationNode(SyntaxNode node)
        {
            if (IsObjectCreationExpressionUnderFieldDeclaration(node))
            {
                return node.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        public override IEnumerable<SyntaxNode> GetDescendantAssignmentExpressionNodes(SyntaxNode node)
        {
            var empty = Enumerable.Empty<SyntaxNode>();
            if (node == null)
            {
                return empty;
            }

            return node.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>();
        }

        public override IEnumerable<SyntaxNode> GetDescendantMemberAccessExpressionNodes(SyntaxNode node)
        {
            var empty = Enumerable.Empty<SyntaxNode>();
            if (node == null)
            {
                return empty;
            }

            return node.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>();
        }
    }
}
