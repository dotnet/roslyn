// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ReplaceMethodWithProperty;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ReplaceMethodWithProperty;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportLanguageService(typeof(IReplaceMethodWithPropertyService), LanguageNames.CSharp), Shared]
internal class CSharpReplaceMethodWithPropertyService : AbstractReplaceMethodWithPropertyService<MethodDeclarationSyntax>, IReplaceMethodWithPropertyService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpReplaceMethodWithPropertyService()
    {
    }

    public void RemoveSetMethod(SyntaxEditor editor, SyntaxNode setMethodDeclaration)
        => editor.RemoveNode(setMethodDeclaration);

    public void ReplaceGetMethodWithProperty(
        CodeGenerationOptions options,
        ParseOptions parseOptions,
        SyntaxEditor editor,
        SemanticModel semanticModel,
        GetAndSetMethods getAndSetMethods,
        string propertyName, bool nameChanged,
        CancellationToken cancellationToken)
    {
        if (getAndSetMethods.GetMethodDeclaration is not MethodDeclarationSyntax getMethodDeclaration)
        {
            return;
        }

        var languageVersion = parseOptions.LanguageVersion();
        var newProperty = ConvertMethodsToProperty(
            (CSharpCodeGenerationOptions)options, languageVersion,
            semanticModel, editor.Generator,
            getAndSetMethods, propertyName, nameChanged, cancellationToken);

        editor.ReplaceNode(getMethodDeclaration, newProperty);
    }

    public static SyntaxNode ConvertMethodsToProperty(
        CSharpCodeGenerationOptions options, LanguageVersion languageVersion,
        SemanticModel semanticModel, SyntaxGenerator generator, GetAndSetMethods getAndSetMethods,
        string propertyName, bool nameChanged, CancellationToken cancellationToken)
    {
        var propertyDeclaration = ConvertMethodsToPropertyWorker(
            options, languageVersion, semanticModel,
            generator, getAndSetMethods, propertyName, nameChanged, cancellationToken);

        var expressionBodyPreference = options.PreferExpressionBodiedProperties.Value;
        if (expressionBodyPreference != ExpressionBodyPreference.Never)
        {
            if (propertyDeclaration.AccessorList is { Accessors: [(kind: SyntaxKind.GetAccessorDeclaration) getAccessor] })
            {
                if (getAccessor.ExpressionBody != null)
                {
                    return propertyDeclaration.WithExpressionBody(getAccessor.ExpressionBody)
                                              .WithSemicolonToken(getAccessor.SemicolonToken)
                                              .WithAccessorList(null);
                }
                else if (getAccessor.Body != null &&
                         getAccessor.Body.TryConvertToArrowExpressionBody(
                             propertyDeclaration.Kind(), languageVersion, expressionBodyPreference, cancellationToken,
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
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                 .WithBody(block);

                var accessorList = AccessorList([accessor]);
                return propertyDeclaration.WithAccessorList(accessorList)
                                          .WithExpressionBody(null)
                                          .WithSemicolonToken(default);
            }
        }

        return propertyDeclaration;
    }

    public static PropertyDeclarationSyntax ConvertMethodsToPropertyWorker(
        CSharpCodeGenerationOptions options, LanguageVersion languageVersion,
        SemanticModel semanticModel, SyntaxGenerator generator, GetAndSetMethods getAndSetMethods,
        string propertyName, bool nameChanged, CancellationToken cancellationToken)
    {
        var getMethodDeclaration = (MethodDeclarationSyntax)getAndSetMethods.GetMethodDeclaration;
        var setMethodDeclaration = getAndSetMethods.SetMethodDeclaration as MethodDeclarationSyntax;
        var getAccessor = CreateGetAccessor(getAndSetMethods, options, languageVersion, cancellationToken);
        var setAccessor = CreateSetAccessor(semanticModel, generator, getAndSetMethods, options, languageVersion, cancellationToken);

        var nameToken = GetPropertyName(getMethodDeclaration.Identifier, propertyName, nameChanged);
        var warning = GetWarning(getAndSetMethods);
        if (warning != null)
        {
            nameToken = nameToken.WithAdditionalAnnotations(WarningAnnotation.Create(warning));
        }

        var property = PropertyDeclaration(
            getMethodDeclaration.AttributeLists, getMethodDeclaration.Modifiers,
            getMethodDeclaration.ReturnType, getMethodDeclaration.ExplicitInterfaceSpecifier,
            nameToken, accessorList: null);

        // copy 'unsafe' from the set method, if it hasn't been already copied from the get method
        if (setMethodDeclaration?.Modifiers.Any(SyntaxKind.UnsafeKeyword) == true
            && !property.Modifiers.Any(SyntaxKind.UnsafeKeyword))
        {
            property = property.AddModifiers(UnsafeKeyword);
        }

        property = SetLeadingTrivia(
            CSharpSyntaxFacts.Instance, getAndSetMethods, property);

        var accessorList = AccessorList([getAccessor]);
        if (setAccessor != null)
        {
            accessorList = accessorList.AddAccessors(setAccessor);
        }

        property = property.WithAccessorList(accessorList);

        return property.WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static SyntaxToken GetPropertyName(SyntaxToken identifier, string propertyName, bool nameChanged)
    {
        return nameChanged
            ? Identifier(propertyName)
            : identifier;
    }

    private static AccessorDeclarationSyntax CreateGetAccessor(
        GetAndSetMethods getAndSetMethods, CSharpCodeGenerationOptions options, LanguageVersion languageVersion, CancellationToken cancellationToken)
    {
        var accessorDeclaration = CreateGetAccessorWorker(getAndSetMethods);

        return UseExpressionOrBlockBodyIfDesired(
            options, languageVersion, accessorDeclaration, cancellationToken);
    }

    private static AccessorDeclarationSyntax UseExpressionOrBlockBodyIfDesired(
        CSharpCodeGenerationOptions options, LanguageVersion languageVersion,
        AccessorDeclarationSyntax accessorDeclaration, CancellationToken cancellationToken)
    {
        var expressionBodyPreference = options.PreferExpressionBodiedAccessors.Value;
        if (accessorDeclaration?.Body != null && expressionBodyPreference != ExpressionBodyPreference.Never)
        {
            if (accessorDeclaration.Body.TryConvertToArrowExpressionBody(
                    accessorDeclaration.Kind(), languageVersion, expressionBodyPreference, cancellationToken,
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

        var accessor = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);

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
        CSharpCodeGenerationOptions options, LanguageVersion languageVersion, CancellationToken cancellationToken)
    {
        var accessorDeclaration = CreateSetAccessorWorker(semanticModel, generator, getAndSetMethods);
        return UseExpressionOrBlockBodyIfDesired(options, languageVersion, accessorDeclaration, cancellationToken);
    }

    private static AccessorDeclarationSyntax CreateSetAccessorWorker(
        SemanticModel semanticModel, SyntaxGenerator generator, GetAndSetMethods getAndSetMethods)
    {
        var setMethod = getAndSetMethods.SetMethod;
        if (getAndSetMethods.SetMethodDeclaration is not MethodDeclarationSyntax setMethodDeclaration || setMethod?.Parameters.Length != 1)
        {
            return null;
        }

        var getMethod = getAndSetMethods.GetMethod;
        var accessor = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration);

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

    private class Rewriter(SemanticModel semanticModel, IParameterSymbol parameter) : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel = semanticModel;
        private readonly IParameterSymbol _parameter = parameter;

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (_parameter.Equals(_semanticModel.GetSymbolInfo(node).Symbol))
            {
                return IdentifierName("value").WithTriviaFrom(node);
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

                var expression = AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression, currentInvocation.Expression, argumentExpression);

                return expression.Parenthesize();
            });
        };

    public void ReplaceGetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged)
        => ReplaceInvocation(editor, nameToken, propertyName, nameChanged, s_replaceGetReferenceInvocation);

    public void ReplaceSetReference(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged)
        => ReplaceInvocation(editor, nameToken, propertyName, nameChanged, s_replaceSetReferenceInvocation);

    public static void ReplaceInvocation(SyntaxEditor editor, SyntaxToken nameToken, string propertyName, bool nameChanged,
        Action<SyntaxEditor, InvocationExpressionSyntax, SimpleNameSyntax, SimpleNameSyntax> replace)
    {
        if (nameToken.Kind() != SyntaxKind.IdentifierToken)
        {
            return;
        }

        if (nameToken.Parent is not IdentifierNameSyntax nameNode)
        {
            return;
        }

        var invocation = nameNode?.FirstAncestorOrSelf<InvocationExpressionSyntax>();

        var newName = nameNode;
        if (nameChanged)
        {
            newName = IdentifierName(Identifier(propertyName));
        }

        newName = newName.WithTriviaFrom(invocation is null ? nameToken.Parent : invocation);

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
