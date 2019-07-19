// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ReplaceMethodWithProperty;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ReplaceMethodWithProperty
{
    [ExportLanguageService(typeof(IReplaceMethodWithPropertyService), LanguageNames.CSharp), Shared]
    internal class CSharpReplaceMethodWithPropertyService : AbstractReplaceMethodWithPropertyService<MethodDeclarationSyntax>, IReplaceMethodWithPropertyService
    {
        [ImportingConstructor]
        public CSharpReplaceMethodWithPropertyService()
        {
        }

        public void RemoveSetMethod(SyntaxEditor editor, SyntaxNode setMethodDeclaration)
        {
            editor.RemoveNode(setMethodDeclaration);
        }

        public void ReplaceGetMethodWithProperty(
            DocumentOptionSet documentOptions,
            ParseOptions parseOptions,
            SyntaxEditor editor,
            SemanticModel semanticModel,
            GetAndSetMethods getAndSetMethods,
            string propertyName, bool nameChanged)
        {
            if (!(getAndSetMethods.GetMethodDeclaration is MethodDeclarationSyntax getMethodDeclaration))
            {
                return;
            }

            var newProperty = ConvertMethodsToProperty(
                documentOptions, parseOptions,
                semanticModel, editor.Generator,
                getAndSetMethods, propertyName, nameChanged);

            editor.ReplaceNode(getMethodDeclaration, newProperty);
        }

        public SyntaxNode ConvertMethodsToProperty(
            DocumentOptionSet documentOptions, ParseOptions parseOptions,
            SemanticModel semanticModel, SyntaxGenerator generator, GetAndSetMethods getAndSetMethods,
            string propertyName, bool nameChanged)
        {
            var propertyDeclaration = ConvertMethodsToPropertyWorker(
                documentOptions, parseOptions, semanticModel,
                generator, getAndSetMethods, propertyName, nameChanged);

            var expressionBodyPreference = documentOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties).Value;
            if (expressionBodyPreference != ExpressionBodyPreference.Never)
            {
                if (propertyDeclaration.AccessorList?.Accessors.Count == 1 &&
                    propertyDeclaration.AccessorList?.Accessors[0].Kind() == SyntaxKind.GetAccessorDeclaration)
                {
                    var getAccessor = propertyDeclaration.AccessorList.Accessors[0];
                    if (getAccessor.ExpressionBody != null)
                    {
                        return propertyDeclaration.WithExpressionBody(getAccessor.ExpressionBody)
                                                  .WithSemicolonToken(getAccessor.SemicolonToken)
                                                  .WithAccessorList(null);
                    }
                    else if (getAccessor.Body != null &&
                             getAccessor.Body.TryConvertToArrowExpressionBody(
                                 propertyDeclaration.Kind(), parseOptions, expressionBodyPreference,
                                 out var arrowExpression, out var semicolonToken))
                    {
                        return propertyDeclaration.WithExpressionBody(arrowExpression)
                                                  .WithSemicolonToken(semicolonToken)
                                                  .WithAccessorList(null);
                    }
                }
            }
            else
            {
                if (propertyDeclaration.ExpressionBody != null &&
                    propertyDeclaration.ExpressionBody.TryConvertToBlock(
                        propertyDeclaration.SemicolonToken,
                        createReturnStatementForExpression: true,
                        block: out var block))
                {
                    var accessor =
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                     .WithBody(block);

                    var accessorList = SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(accessor));
                    return propertyDeclaration.WithAccessorList(accessorList)
                                              .WithExpressionBody(null)
                                              .WithSemicolonToken(default);
                }
            }

            return propertyDeclaration;
        }

        public PropertyDeclarationSyntax ConvertMethodsToPropertyWorker(
            DocumentOptionSet documentOptions, ParseOptions parseOptions,
            SemanticModel semanticModel, SyntaxGenerator generator, GetAndSetMethods getAndSetMethods,
            string propertyName, bool nameChanged)
        {
            var getMethodDeclaration = getAndSetMethods.GetMethodDeclaration as MethodDeclarationSyntax;
            var getAccessor = CreateGetAccessor(getAndSetMethods, documentOptions, parseOptions);
            var setAccessor = CreateSetAccessor(semanticModel, generator, getAndSetMethods, documentOptions, parseOptions);

            var nameToken = GetPropertyName(getMethodDeclaration.Identifier, propertyName, nameChanged);
            var warning = GetWarning(getAndSetMethods);
            if (warning != null)
            {
                nameToken = nameToken.WithAdditionalAnnotations(WarningAnnotation.Create(warning));
            }

            var property = SyntaxFactory.PropertyDeclaration(
                getMethodDeclaration.AttributeLists, getMethodDeclaration.Modifiers,
                getMethodDeclaration.ReturnType, getMethodDeclaration.ExplicitInterfaceSpecifier,
                nameToken, accessorList: null);

            property = SetLeadingTrivia(
                CSharpSyntaxFactsService.Instance, getAndSetMethods, property);

            var accessorList = SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getAccessor));
            if (setAccessor != null)
            {
                accessorList = accessorList.AddAccessors(setAccessor);
            }

            property = property.WithAccessorList(accessorList);

            return property.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private SyntaxToken GetPropertyName(SyntaxToken identifier, string propertyName, bool nameChanged)
        {
            return nameChanged
                ? SyntaxFactory.Identifier(propertyName)
                : identifier;
        }

        private static AccessorDeclarationSyntax CreateGetAccessor(
            GetAndSetMethods getAndSetMethods, DocumentOptionSet documentOptions, ParseOptions parseOptions)
        {
            var accessorDeclaration = CreateGetAccessorWorker(getAndSetMethods);

            return UseExpressionOrBlockBodyIfDesired(
                documentOptions, parseOptions, accessorDeclaration);
        }

        private static AccessorDeclarationSyntax UseExpressionOrBlockBodyIfDesired(
            DocumentOptionSet documentOptions, ParseOptions parseOptions,
            AccessorDeclarationSyntax accessorDeclaration)
        {
            var expressionBodyPreference = documentOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors).Value;
            if (accessorDeclaration?.Body != null && expressionBodyPreference != ExpressionBodyPreference.Never)
            {
                if (accessorDeclaration.Body.TryConvertToArrowExpressionBody(
                        accessorDeclaration.Kind(), parseOptions, expressionBodyPreference,
                        out var arrowExpression, out var semicolonToken))
                {
                    return accessorDeclaration.WithBody(null)
                                              .WithExpressionBody(arrowExpression)
                                              .WithSemicolonToken(semicolonToken)
                                              .WithAdditionalAnnotations(Formatter.Annotation);
                }
            }
            else if (accessorDeclaration?.ExpressionBody != null && expressionBodyPreference == ExpressionBodyPreference.Never)
            {
                if (accessorDeclaration.ExpressionBody.TryConvertToBlock(
                        accessorDeclaration.SemicolonToken,
                        createReturnStatementForExpression: accessorDeclaration.Kind() == SyntaxKind.GetAccessorDeclaration,
                        block: out var block))
                {
                    return accessorDeclaration.WithExpressionBody(null)
                                              .WithSemicolonToken(default)
                                              .WithBody(block)
                                              .WithAdditionalAnnotations(Formatter.Annotation);
                }
            }

            return accessorDeclaration;
        }

        private static AccessorDeclarationSyntax CreateGetAccessorWorker(GetAndSetMethods getAndSetMethods)
        {
            var getMethodDeclaration = getAndSetMethods.GetMethodDeclaration as MethodDeclarationSyntax;

            var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);

            if (getMethodDeclaration.ExpressionBody != null)
            {
                return accessor.WithExpressionBody(getMethodDeclaration.ExpressionBody)
                               .WithSemicolonToken(getMethodDeclaration.SemicolonToken);
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
            SemanticModel semanticModel, SyntaxGenerator generator, GetAndSetMethods getAndSetMethods,
            DocumentOptionSet documentOptions, ParseOptions parseOptions)
        {
            var accessorDeclaration = CreateSetAccessorWorker(semanticModel, generator, getAndSetMethods);
            return UseExpressionOrBlockBodyIfDesired(documentOptions, parseOptions, accessorDeclaration);
        }

        private static AccessorDeclarationSyntax CreateSetAccessorWorker(
            SemanticModel semanticModel, SyntaxGenerator generator, GetAndSetMethods getAndSetMethods)
        {
            var setMethod = getAndSetMethods.SetMethod;
            if (!(getAndSetMethods.SetMethodDeclaration is MethodDeclarationSyntax setMethodDeclaration) || setMethod?.Parameters.Length != 1)
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
                var oldExpressionBody = setMethodDeclaration.ExpressionBody;
                var expression = ReplaceReferencesToParameterWithValue(
                    semanticModel, setMethod.Parameters[0], oldExpressionBody.Expression);

                return accessor.WithExpressionBody(oldExpressionBody.WithExpression(expression))
                               .WithSemicolonToken(setMethodDeclaration.SemicolonToken);
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

        // We use the callback form if "ReplaceNode" here because we want to see the
        // invocation expression after any rewrites we already did when rewriting previous
        // 'get' references.
        private static readonly Action<SyntaxEditor, InvocationExpressionSyntax, SimpleNameSyntax, SimpleNameSyntax> s_replaceGetReferenceInvocation =
            (editor, invocation, nameNode, newName) => editor.ReplaceNode(invocation, (i, g) =>
            {
                var currentInvocation = (InvocationExpressionSyntax)i;

                var currentName = currentInvocation.Expression.GetRightmostName();
                return currentInvocation.Expression.ReplaceNode(currentName, newName);
            });

        private static readonly Action<SyntaxEditor, InvocationExpressionSyntax, SimpleNameSyntax, SimpleNameSyntax> s_replaceSetReferenceInvocation =
            (editor, invocation, nameNode, newName) =>
            {
                if (invocation.ArgumentList?.Arguments.Count != 1 ||
                    invocation.ArgumentList.Arguments[0].Expression.Kind() == SyntaxKind.DeclarationExpression)
                {
                    var annotation = ConflictAnnotation.Create(FeaturesResources.Only_methods_with_a_single_argument_which_is_not_an_out_variable_declaration_can_be_replaced_with_a_property);
                    editor.ReplaceNode(nameNode, newName.WithIdentifier(newName.Identifier.WithAdditionalAnnotations(annotation)));
                    return;
                }

                // We use the callback form if "ReplaceNode" here because we want to see the
                // invocation expression after any rewrites we already did when rewriting the
                // 'get' references.
                editor.ReplaceNode(invocation, (i, g) =>
                {
                    var currentInvocation = (InvocationExpressionSyntax)i;
                    // looks like   a.b.Goo(arg)   =>     a.b.NewName = arg
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
            => ReplaceInvocation(editor, nameToken, propertyName, nameChanged, s_replaceGetReferenceInvocation);

        public void ReplaceSetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged)
            => ReplaceInvocation(editor, nameToken, propertyName, nameChanged, s_replaceSetReferenceInvocation);

        public void ReplaceInvocation(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged,
            Action<SyntaxEditor, InvocationExpressionSyntax, SimpleNameSyntax, SimpleNameSyntax> replace)
        {
            if (nameToken.Kind() != SyntaxKind.IdentifierToken)
            {
                return;
            }

            if (!(nameToken.Parent is IdentifierNameSyntax nameNode))
            {
                return;
            }

            var invocation = nameNode?.FirstAncestorOrSelf<InvocationExpressionSyntax>();

            var newName = nameNode;
            if (nameChanged)
            {
                if (invocation == null)
                {
                    newName = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(propertyName)
                        .WithTriviaFrom(nameToken));
                }
                else
                {
                    newName = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(propertyName)
                        .WithLeadingTrivia(nameToken.LeadingTrivia)
                        .WithTrailingTrivia(invocation.ArgumentList.CloseParenToken.TrailingTrivia));
                }
            }

            var invocationExpression = invocation?.Expression;
            if (!IsInvocationName(nameNode, invocationExpression))
            {
                // Wasn't invoked.  Change the name, but report a conflict.
                var annotation = ConflictAnnotation.Create(FeaturesResources.Non_invoked_method_cannot_be_replaced_with_property);
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
