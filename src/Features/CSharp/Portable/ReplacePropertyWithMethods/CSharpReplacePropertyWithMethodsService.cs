// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ReplacePropertyWithMethods;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ReplacePropertyWithMethods;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportLanguageService(typeof(IReplacePropertyWithMethodsService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpReplacePropertyWithMethodsService() :
    AbstractReplacePropertyWithMethodsService<IdentifierNameSyntax, ExpressionSyntax, NameMemberCrefSyntax, StatementSyntax, PropertyDeclarationSyntax>
{
    public override async Task<ImmutableArray<SyntaxNode>> GetReplacementMembersAsync(
        Document document,
        IPropertySymbol property,
        SyntaxNode propertyDeclarationNode,
        IFieldSymbol? propertyBackingField,
        string desiredGetMethodName,
        string desiredSetMethodName,
        CancellationToken cancellationToken)
    {
        if (propertyDeclarationNode is not PropertyDeclarationSyntax propertyDeclaration)
            return [];

        var options = (CSharpCodeGenerationOptions)await document.GetCodeGenerationOptionsAsync(cancellationToken).ConfigureAwait(false);
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var languageVersion = syntaxTree.Options.LanguageVersion();

        return ConvertPropertyToMembers(
            languageVersion,
            SyntaxGenerator.GetGenerator(document), property,
            propertyDeclaration, propertyBackingField,
            options.PreferExpressionBodiedMethods.Value, desiredGetMethodName, desiredSetMethodName,
            cancellationToken);
    }

    private static ImmutableArray<SyntaxNode> ConvertPropertyToMembers(
        LanguageVersion languageVersion,
        SyntaxGenerator generator,
        IPropertySymbol property,
        PropertyDeclarationSyntax propertyDeclaration,
        IFieldSymbol? propertyBackingField,
        ExpressionBodyPreference expressionBodyPreference,
        string desiredGetMethodName,
        string desiredSetMethodName,
        CancellationToken cancellationToken)
    {
        using var result = TemporaryArray<SyntaxNode>.Empty;

        if (propertyBackingField != null)
        {
            var initializer = propertyDeclaration.Initializer?.Value;
            result.Add(generator.FieldDeclaration(propertyBackingField, initializer));
        }

        var getMethod = property.GetMethod;
        if (getMethod != null)
        {
            result.Add(GetGetMethod(
                languageVersion,
                generator, propertyDeclaration, propertyBackingField,
                getMethod, desiredGetMethodName, expressionBodyPreference,
                cancellationToken));
        }

        var setMethod = property.SetMethod;
        if (setMethod != null)
        {
            // Only copy leading trivia to the setter if we didn't already copy it to the getter.
            result.Add(GetSetMethod(
                languageVersion,
                generator, propertyDeclaration, propertyBackingField,
                setMethod, desiredSetMethodName, expressionBodyPreference,
                copyLeadingTrivia: getMethod is null,
                cancellationToken));
        }

        return result.ToImmutableAndClear();
    }

    private static SyntaxNode GetSetMethod(
        LanguageVersion languageVersion,
        SyntaxGenerator generator,
        PropertyDeclarationSyntax propertyDeclaration,
        IFieldSymbol? propertyBackingField,
        IMethodSymbol setMethod,
        string desiredSetMethodName,
        ExpressionBodyPreference expressionBodyPreference,
        bool copyLeadingTrivia,
        CancellationToken cancellationToken)
    {
        var methodDeclaration = GetSetMethodWorker();

        if (copyLeadingTrivia)
            methodDeclaration = CopyLeadingTrivia(propertyDeclaration, methodDeclaration, ConvertValueToParamRewriter.Instance);

        return UseExpressionOrBlockBodyIfDesired(
            languageVersion, methodDeclaration, expressionBodyPreference,
            createReturnStatementForExpression: false, cancellationToken);

        MethodDeclarationSyntax GetSetMethodWorker()
        {
            var setAccessorDeclaration = (AccessorDeclarationSyntax)setMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            var methodDeclaration = (MethodDeclarationSyntax)generator.MethodDeclaration(setMethod, desiredSetMethodName);

            // property has unsafe, but generator didn't add it to the method, so we have to add it here
            if (propertyDeclaration.Modifiers.Any(SyntaxKind.UnsafeKeyword)
                && !methodDeclaration.Modifiers.Any(SyntaxKind.UnsafeKeyword))
            {
                methodDeclaration = methodDeclaration.AddModifiers(UnsafeKeyword);
            }

            methodDeclaration = methodDeclaration.WithAttributeLists(setAccessorDeclaration.AttributeLists);

            // If this is a partial definition, return a declaration without a body
            if (setMethod.IsPartialDefinition)
                return methodDeclaration;

            if (setAccessorDeclaration.Body != null)
            {
                return methodDeclaration.WithBody(setAccessorDeclaration.Body)
                                        .WithAdditionalAnnotations(Formatter.Annotation)
                                        .WithSemicolonToken(default);
            }
            else if (setAccessorDeclaration.ExpressionBody != null)
            {
                return methodDeclaration.WithBody(null)
                                        .WithExpressionBody(setAccessorDeclaration.ExpressionBody)
                                        .WithSemicolonToken(setAccessorDeclaration.SemicolonToken);
            }
            else if (propertyBackingField != null)
            {
                return methodDeclaration.WithBody(Block(
                    (StatementSyntax)generator.ExpressionStatement(
                        generator.AssignmentStatement(
                            GetFieldReference(generator, propertyBackingField),
                            generator.IdentifierName("value")))));
            }

            return methodDeclaration;
        }
    }

    private static SyntaxNode GetGetMethod(
        LanguageVersion languageVersion,
        SyntaxGenerator generator,
        PropertyDeclarationSyntax propertyDeclaration,
        IFieldSymbol? propertyBackingField,
        IMethodSymbol getMethod,
        string desiredGetMethodName,
        ExpressionBodyPreference expressionBodyPreference,
        CancellationToken cancellationToken)
    {
        var methodDeclaration = GetGetMethodWorker();

        methodDeclaration = CopyLeadingTrivia(propertyDeclaration, methodDeclaration, ConvertValueToReturnsRewriter.Instance);

        return UseExpressionOrBlockBodyIfDesired(
            languageVersion, methodDeclaration, expressionBodyPreference,
            createReturnStatementForExpression: true, cancellationToken);

        MethodDeclarationSyntax GetGetMethodWorker()
        {
            var methodDeclaration = (MethodDeclarationSyntax)generator.MethodDeclaration(getMethod, desiredGetMethodName);

            // property has unsafe, but generator didn't add it to the method, so we have to add it here
            if (propertyDeclaration.Modifiers.Any(SyntaxKind.UnsafeKeyword)
                && !methodDeclaration.Modifiers.Any(SyntaxKind.UnsafeKeyword))
            {
                methodDeclaration = methodDeclaration.AddModifiers(UnsafeKeyword);
            }

            // If this is a partial definition, return a declaration without a body
            if (getMethod.IsPartialDefinition)
                return methodDeclaration;

            if (propertyDeclaration.ExpressionBody != null)
            {
                return methodDeclaration.WithBody(null)
                                        .WithExpressionBody(propertyDeclaration.ExpressionBody)
                                        .WithSemicolonToken(propertyDeclaration.SemicolonToken);
            }
            else
            {
                var getAccessorDeclaration = (AccessorDeclarationSyntax)getMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);

                methodDeclaration = methodDeclaration.WithAttributeLists(getAccessorDeclaration.AttributeLists);

                if (getAccessorDeclaration?.ExpressionBody != null)
                {
                    return methodDeclaration.WithBody(null)
                                            .WithExpressionBody(getAccessorDeclaration.ExpressionBody)
                                            .WithSemicolonToken(getAccessorDeclaration.SemicolonToken);
                }
                else if (getAccessorDeclaration?.Body != null)
                {
                    return methodDeclaration.WithBody(getAccessorDeclaration.Body)
                                            .WithAdditionalAnnotations(Formatter.Annotation);
                }
                else if (propertyBackingField != null)
                {
                    var fieldReference = GetFieldReference(generator, propertyBackingField);
                    return methodDeclaration.WithBody(
                        Block(
                            (StatementSyntax)generator.ReturnStatement(fieldReference)));
                }
            }

            return methodDeclaration;
        }
    }

    private static MethodDeclarationSyntax CopyLeadingTrivia(
        PropertyDeclarationSyntax propertyDeclaration,
        MethodDeclarationSyntax methodDeclaration,
        CSharpSyntaxRewriter documentationCommentRewriter)
    {
        var leadingTrivia = propertyDeclaration.GetLeadingTrivia();
        return methodDeclaration.WithLeadingTrivia(leadingTrivia.Select(trivia => ConvertTrivia(trivia, documentationCommentRewriter)));
    }

    private static SyntaxTrivia ConvertTrivia(SyntaxTrivia trivia, CSharpSyntaxRewriter rewriter)
    {
        return trivia.Kind() is SyntaxKind.MultiLineDocumentationCommentTrivia or SyntaxKind.SingleLineDocumentationCommentTrivia
            ? ConvertDocumentationComment(trivia, rewriter)
            : trivia;
    }

    private static SyntaxTrivia ConvertDocumentationComment(SyntaxTrivia trivia, CSharpSyntaxRewriter rewriter)
    {
        var structure = trivia.GetStructure();
        var rewritten = rewriter.Visit(structure);
        Contract.ThrowIfNull(rewritten);
        return Trivia((StructuredTriviaSyntax)rewritten);
    }

    private static MethodDeclarationSyntax UseExpressionOrBlockBodyIfDesired(
        LanguageVersion languageVersion,
        MethodDeclarationSyntax methodDeclaration,
        ExpressionBodyPreference expressionBodyPreference,
        bool createReturnStatementForExpression,
        CancellationToken cancellationToken)
    {
        if (methodDeclaration.Body != null && expressionBodyPreference != ExpressionBodyPreference.Never)
        {
            if (methodDeclaration.Body.TryConvertToArrowExpressionBody(
                    methodDeclaration.Kind(), languageVersion, expressionBodyPreference, cancellationToken,
                    out var arrowExpression, out var semicolonToken))
            {
                return methodDeclaration.WithBody(null)
                                        .WithExpressionBody(arrowExpression)
                                        .WithSemicolonToken(semicolonToken)
                                        .WithAdditionalAnnotations(Formatter.Annotation);
            }
        }
        else if (methodDeclaration.ExpressionBody != null && expressionBodyPreference == ExpressionBodyPreference.Never)
        {
            if (methodDeclaration.ExpressionBody.TryConvertToBlock(
                    methodDeclaration.SemicolonToken, createReturnStatementForExpression, out var block))
            {
                return methodDeclaration.WithExpressionBody(null)
                                        .WithSemicolonToken(default)
                                        .WithBody(block)
                                        .WithAdditionalAnnotations(Formatter.Annotation);
            }
        }

        return methodDeclaration;
    }

    /// <summary>
    /// Used by the documentation comment rewriters to identify top-level <c>&lt;value&gt;</c> nodes.
    /// </summary>
    private static bool IsValueName(XmlNameSyntax name)
        => name.Prefix == null &&
           name.LocalName.ValueText == "value";

    public override SyntaxNode GetPropertyNodeToReplace(SyntaxNode propertyDeclaration)
    {
        // For C# we'll have the property declaration that we want to replace.
        return propertyDeclaration;
    }

    protected override NameMemberCrefSyntax? TryGetCrefSyntax(IdentifierNameSyntax identifierName)
        => identifierName.Parent as NameMemberCrefSyntax;

    protected override NameMemberCrefSyntax CreateCrefSyntax(NameMemberCrefSyntax originalCref, SyntaxToken identifierToken, SyntaxNode? parameterType)
    {
        CrefParameterListSyntax parameterList;
        if (parameterType is TypeSyntax typeSyntax)
        {
            var parameter = CrefParameter(typeSyntax);
            parameterList = CrefParameterList([parameter]);
        }
        else
        {
            parameterList = CrefParameterList();
        }

        // XmlCrefAttribute replaces <T> with {T}, which is required for C# documentation comments
        var crefAttribute = XmlCrefAttribute(
            NameMemberCref(IdentifierName(identifierToken), parameterList));
        return (NameMemberCrefSyntax)crefAttribute.Cref;
    }

    protected override ExpressionSyntax UnwrapCompoundAssignment(
        SyntaxNode compoundAssignment, ExpressionSyntax readExpression)
    {
        var parent = (AssignmentExpressionSyntax)compoundAssignment;

        var operatorKind = parent.Kind() switch
        {
            SyntaxKind.AddAssignmentExpression => SyntaxKind.AddExpression,
            SyntaxKind.AndAssignmentExpression => SyntaxKind.BitwiseAndExpression,
            SyntaxKind.CoalesceAssignmentExpression => SyntaxKind.CoalesceExpression,
            SyntaxKind.DivideAssignmentExpression => SyntaxKind.DivideExpression,
            SyntaxKind.ExclusiveOrAssignmentExpression => SyntaxKind.ExclusiveOrExpression,
            SyntaxKind.LeftShiftAssignmentExpression => SyntaxKind.LeftShiftExpression,
            SyntaxKind.ModuloAssignmentExpression => SyntaxKind.ModuloExpression,
            SyntaxKind.MultiplyAssignmentExpression => SyntaxKind.MultiplyExpression,
            SyntaxKind.OrAssignmentExpression => SyntaxKind.BitwiseOrExpression,
            SyntaxKind.RightShiftAssignmentExpression => SyntaxKind.RightShiftExpression,
            SyntaxKind.SubtractAssignmentExpression => SyntaxKind.SubtractExpression,
            SyntaxKind.UnsignedRightShiftAssignmentExpression => SyntaxKind.UnsignedRightShiftExpression,
            _ => SyntaxKind.None,
        };

        if (operatorKind is SyntaxKind.None)
            return parent;

        return BinaryExpression(operatorKind, readExpression, parent.Right.Parenthesize());
    }
}
