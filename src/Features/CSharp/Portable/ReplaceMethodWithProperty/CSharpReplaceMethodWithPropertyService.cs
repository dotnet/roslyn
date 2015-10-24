// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ReplaceMethodWithProperty;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ReplaceMethodWithProperty
{
    [ExportLanguageService(typeof(IReplaceMethodWithPropertyService), LanguageNames.CSharp), Shared]
    internal class CSharpReplaceMethodWithPropertyService : IReplaceMethodWithPropertyService
    {
        public string GetMethodName(SyntaxNode methodNode)
        {
            return ((MethodDeclarationSyntax)methodNode).Identifier.ValueText;
        }

        public SyntaxNode GetMethodDeclaration(SyntaxToken token)
        {
            var containingMethod = token.Parent.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod == null)
            {
                return null;
            }

            var start = containingMethod.AttributeLists.Count > 0
                ? containingMethod.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
                : containingMethod.SpanStart;

            // Offer this refactoring anywhere in the signature of the method.
            var position = token.SpanStart;
            if (position < start || position > containingMethod.ParameterList.Span.End)
            {
                return null;
            }

            return containingMethod;
        }

        public void RemoveSetMethod(SyntaxEditor editor, SyntaxNode setMethodDeclaration)
        {
            editor.RemoveNode(setMethodDeclaration);
        }

        public void ReplaceGetMethodWithProperty(
            SyntaxEditor editor,
            SemanticModel semanticModel,
            GetAndSetMethods getAndSetMethods,
            string propertyName, bool nameChanged)
        {
            var getMethodDeclaration = getAndSetMethods.GetMethodDeclaration as MethodDeclarationSyntax;
            if (getMethodDeclaration == null)
            {
                return;
            }

            editor.ReplaceNode(getMethodDeclaration,
                ConvertMethodsToProperty(semanticModel, editor.Generator, getAndSetMethods, propertyName, nameChanged));
        }

        public SyntaxNode ConvertMethodsToProperty(
            SemanticModel semanticModel,
            SyntaxGenerator generator, GetAndSetMethods getAndSetMethods,
            string propertyName, bool nameChanged)
        {
            var getMethodDeclaration = getAndSetMethods.GetMethodDeclaration as MethodDeclarationSyntax;
            var getAccessor = CreateGetAccessor(getAndSetMethods);
            var setAccessor = CreateSetAccessor(semanticModel, generator, getAndSetMethods);

            var property = SyntaxFactory.PropertyDeclaration(
                getMethodDeclaration.AttributeLists, getMethodDeclaration.Modifiers, 
                getMethodDeclaration.ReturnType, getMethodDeclaration.ExplicitInterfaceSpecifier, 
                GetPropertyName(getMethodDeclaration.Identifier, propertyName, nameChanged), accessorList: null);

            IEnumerable<SyntaxTrivia> trivia = getMethodDeclaration.GetLeadingTrivia();
            var setMethodDeclaration = getAndSetMethods.SetMethodDeclaration;
            if (setMethodDeclaration != null)
            {
                trivia = trivia.Concat(setMethodDeclaration.GetLeadingTrivia());
            }
            property = property.WithLeadingTrivia(trivia.Where(t => !t.IsDirective));

            if (getMethodDeclaration.ExpressionBody != null && setMethodDeclaration == null)
            {
                property = property.WithExpressionBody(getMethodDeclaration.ExpressionBody);
                property = property.WithSemicolonToken(getMethodDeclaration.SemicolonToken);
            }
            else
            {
                var accessorList = SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getAccessor));
                if (setAccessor != null)
                {
                    accessorList = accessorList.AddAccessors(new[] { setAccessor });
                }

                property = property.WithAccessorList(accessorList);
            }

            return property.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private SyntaxToken GetPropertyName(SyntaxToken identifier, string propertyName, bool nameChanged)
        {
            return nameChanged
                ? SyntaxFactory.Identifier(propertyName)
                : identifier;
        }

        private static AccessorDeclarationSyntax CreateGetAccessor(GetAndSetMethods getAndSetMethods)
        {
            var getMethodDeclaration = getAndSetMethods.GetMethodDeclaration as MethodDeclarationSyntax;

            var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
            if (getMethodDeclaration.ExpressionBody != null && getAndSetMethods.SetMethodDeclaration != null)
            {
                var statement = SyntaxFactory.ReturnStatement(getMethodDeclaration.ExpressionBody.Expression)
                                             .WithSemicolonToken(getMethodDeclaration.SemicolonToken);
                return accessor.WithBody(SyntaxFactory.Block(statement));
            }

            if (getMethodDeclaration.SemicolonToken.Kind() != SyntaxKind.None)
            {
                return accessor.WithSemicolonToken(getMethodDeclaration.SemicolonToken);
            }

            if (getMethodDeclaration.Body != null)
            {
                return accessor.WithBody(getMethodDeclaration.Body.WithAdditionalAnnotations(Formatter.Annotation));
            }

            return accessor;
        }

        private static AccessorDeclarationSyntax CreateSetAccessor(
            SemanticModel semanticModel, SyntaxGenerator generator, GetAndSetMethods getAndSetMethods)
        {
            var setMethodDeclaration = getAndSetMethods.SetMethodDeclaration as MethodDeclarationSyntax;
            var setMethod = getAndSetMethods.SetMethod;
            if (setMethodDeclaration == null || setMethod?.Parameters.Length != 1)
            {
                return null;
            }

            var getMethod = getAndSetMethods.GetMethod;
            var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration);

            if (getMethod.DeclaredAccessibility != setMethod.DeclaredAccessibility)
            {
                accessor = (AccessorDeclarationSyntax)generator.WithAccessibility(accessor, setMethod.DeclaredAccessibility);
            }

            if (setMethodDeclaration.ExpressionBody != null)
            {
                var expression = ReplaceReferencesToParameterWithValue(semanticModel, setMethod.Parameters[0], setMethodDeclaration.ExpressionBody.Expression);
                var statement = SyntaxFactory.ExpressionStatement(expression)
                                             .WithSemicolonToken(setMethodDeclaration.SemicolonToken);
                return accessor.WithBody(SyntaxFactory.Block(statement));
            }

            if (setMethodDeclaration.SemicolonToken.Kind() != SyntaxKind.None)
            {
                return accessor.WithSemicolonToken(setMethodDeclaration.SemicolonToken);
            }

            if (setMethodDeclaration.Body != null)
            {
                var body = ReplaceReferencesToParameterWithValue(semanticModel, setMethod.Parameters[0], setMethodDeclaration.Body);
                return accessor.WithBody(body.WithAdditionalAnnotations(Formatter.Annotation));
            }

            return accessor;
        }

        private static TNode ReplaceReferencesToParameterWithValue<TNode>(SemanticModel semanticModel, IParameterSymbol parameter, TNode node)
            where TNode : SyntaxNode
        {
            var rewriter = new Rewriter(semanticModel, parameter);
            return (TNode)rewriter.Visit(node);
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly SemanticModel _semanticModel;
            private readonly IParameterSymbol _parameter;

            public Rewriter(SemanticModel semanticModel, IParameterSymbol parameter) 
            {
                _semanticModel = semanticModel;
                _parameter = parameter;
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (_parameter.Equals(_semanticModel.GetSymbolInfo(node).Symbol))
                {
                    return SyntaxFactory.IdentifierName("value").WithTriviaFrom(node);
                }

                return node;
            }
        }

        private static Action<SyntaxEditor, InvocationExpressionSyntax, SimpleNameSyntax, SimpleNameSyntax> replaceGetReferenceInvocation =
            (editor, invocation, nameNode, newName) => editor.ReplaceNode(invocation, invocation.Expression.ReplaceNode(nameNode, newName));

        private static Action<SyntaxEditor, InvocationExpressionSyntax, SimpleNameSyntax, SimpleNameSyntax> replaceSetReferenceInvocation =
            (editor, invocation, nameNode, newName) =>
            {
                if (invocation.ArgumentList?.Arguments.Count != 1)
                {
                    var annotation = ConflictAnnotation.Create(FeaturesResources.OnlyMethodsWithASingleArgumentCanBeReplacedWithAProperty);
                    editor.ReplaceNode(nameNode, newName.WithIdentifier(newName.Identifier.WithAdditionalAnnotations(annotation)));
                    return;
                }

                // We use the callback form if "ReplaceNode" here because we want to see the
                // invocation expression after any rewrites we already did when rewriting the
                // 'get' references.
                editor.ReplaceNode(invocation, (i, g) =>
                {
                    var currentInvocation = (InvocationExpressionSyntax)i;
                    // looks like   a.b.Foo(arg)   =>     a.b.NewName = arg
                    nameNode = currentInvocation.Expression.GetRightmostName();
                    currentInvocation = (InvocationExpressionSyntax)g.ReplaceNode(currentInvocation, nameNode, newName);

                    // Wrap the argument in parentheses (in order to not introduce any precedence problems).
                    // But also add a simplification annotation so we can remove the parens if possible.
                    var argumentExpression = currentInvocation.ArgumentList.Arguments[0].Expression.Parenthesize();

                    var expression = SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression, currentInvocation.Expression, argumentExpression);

                    return expression.Parenthesize();
                });
            };


        public void ReplaceGetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged)
        {
            ReplaceInvocation(editor, nameToken, propertyName, nameChanged, replaceGetReferenceInvocation);
        }

        public void ReplaceSetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged)
        {
            ReplaceInvocation(editor, nameToken, propertyName, nameChanged, replaceSetReferenceInvocation);
        }

        public void ReplaceInvocation(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged,
            Action<SyntaxEditor, InvocationExpressionSyntax, SimpleNameSyntax, SimpleNameSyntax> replace)
        {
            if (nameToken.Kind() != SyntaxKind.IdentifierToken)
            {
                return;
            }

            var nameNode = nameToken.Parent as IdentifierNameSyntax;
            if (nameNode == null)
            {
                return;
            }

            var newName = nameChanged
                ? SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(propertyName).WithTriviaFrom(nameToken))
                : nameNode;

            var invocation = nameNode?.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            var invocationExpression = invocation?.Expression;
            if (!IsInvocationName(nameNode, invocationExpression))
            {
                // Wasn't invoked.  Change the name, but report a conflict.
                var annotation = ConflictAnnotation.Create(FeaturesResources.NonInvokedMethodCannotBeReplacedWithProperty);
                editor.ReplaceNode(nameNode, newName.WithIdentifier(newName.Identifier.WithAdditionalAnnotations(annotation)));
                return;
            }

            // It was invoked.  Remove the invocation, and also change the name if necessary.
            replace(editor, invocation, nameNode, newName);

        }

        private static bool IsInvocationName(IdentifierNameSyntax nameNode, ExpressionSyntax invocationExpression)
        {
            if (invocationExpression == nameNode)
            {
                return true;
            }

            if (nameNode.IsAnyMemberAccessExpressionName() && nameNode.Parent == invocationExpression)
            {
                return true;
            }

            return false;
        }
    }
}
