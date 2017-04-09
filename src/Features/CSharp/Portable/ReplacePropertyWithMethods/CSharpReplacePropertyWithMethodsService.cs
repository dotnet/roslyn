﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ReplacePropertyWithMethods;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ReplacePropertyWithMethods
{
    [ExportLanguageService(typeof(IReplacePropertyWithMethodsService), LanguageNames.CSharp), Shared]
    internal partial class CSharpReplacePropertyWithMethodsService : 
        AbstractReplacePropertyWithMethodsService<IdentifierNameSyntax, ExpressionSyntax, StatementSyntax>
    {
        public override SyntaxNode GetPropertyDeclaration(SyntaxToken token)
        {
            var containingProperty = token.Parent.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (containingProperty == null)
            {
                return null;
            }

            var start = containingProperty.AttributeLists.Count > 0
                ? containingProperty.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
                : containingProperty.SpanStart;

            // Offer this refactoring anywhere in the signature of the property.
            var position = token.SpanStart;
            if (position < start || position > containingProperty.Identifier.Span.End)
            {
                return null;
            }

            return containingProperty;
        }

        public override async Task<IList<SyntaxNode>> GetReplacementMembersAsync(
            Document document,
            IPropertySymbol property,
            SyntaxNode propertyDeclarationNode,
            IFieldSymbol propertyBackingField,
            string desiredGetMethodName,
            string desiredSetMethodName,
            CancellationToken cancellationToken)
        {
            var propertyDeclaration = propertyDeclarationNode as PropertyDeclarationSyntax;
            if (propertyDeclaration == null)
            {
                return SpecializedCollections.EmptyList<SyntaxNode>();
            }

            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var parseOptions = syntaxTree.Options;

            return ConvertPropertyToMembers(
                documentOptions, parseOptions,
                SyntaxGenerator.GetGenerator(document), property,
                propertyDeclaration, propertyBackingField,
                desiredGetMethodName, desiredSetMethodName,
                cancellationToken);
        }

        private List<SyntaxNode> ConvertPropertyToMembers(
            DocumentOptionSet documentOptions,
            ParseOptions parseOptions,
            SyntaxGenerator generator, 
            IPropertySymbol property,
            PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol propertyBackingField,
            string desiredGetMethodName,
            string desiredSetMethodName,
            CancellationToken cancellationToken)
        {
            var result = new List<SyntaxNode>();

            if (propertyBackingField != null)
            {
                var initializer = propertyDeclaration.Initializer?.Value;
                result.Add(generator.FieldDeclaration(propertyBackingField, initializer));
            }

            var getMethod = property.GetMethod;
            if (getMethod != null)
            {
                result.Add(GetGetMethod(
                    documentOptions, parseOptions, 
                    generator, propertyDeclaration, propertyBackingField,
                    getMethod, desiredGetMethodName,
                    copyLeadingTrivia: true,
                    cancellationToken: cancellationToken));
            }

            var setMethod = property.SetMethod;
            if (setMethod != null)
            {
                // Set-method only gets the leading trivia of the property if we didn't copy
                // that trivia to the get-method.
                result.Add(GetSetMethod(
                    documentOptions, parseOptions,
                    generator, propertyDeclaration, propertyBackingField, 
                    setMethod, desiredSetMethodName,
                    copyLeadingTrivia: getMethod == null,
                    cancellationToken: cancellationToken));
            }

            return result;
        }

        private static SyntaxNode GetSetMethod(
            DocumentOptionSet documentOptions,
            ParseOptions parseOptions,
            SyntaxGenerator generator,
            PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol propertyBackingField,
            IMethodSymbol setMethod,
            string desiredSetMethodName,
            bool copyLeadingTrivia,
            CancellationToken cancellationToken)
        {
            var methodDeclaration = GetSetMethodWorker(
                generator, propertyDeclaration, propertyBackingField,
                setMethod, desiredSetMethodName, cancellationToken);

            methodDeclaration = CopyLeadingTrivia(propertyDeclaration, methodDeclaration, copyLeadingTrivia);

            return UseExpressionOrBlockBodyIfDesired(
                documentOptions, parseOptions, methodDeclaration,
                createReturnStatementForExpression: false);
        }

        private static MethodDeclarationSyntax GetSetMethodWorker(
            SyntaxGenerator generator, 
            PropertyDeclarationSyntax propertyDeclaration, 
            IFieldSymbol propertyBackingField,
            IMethodSymbol setMethod,
            string desiredSetMethodName,
            CancellationToken cancellationToken)
        {
            var setAccessorDeclaration = (AccessorDeclarationSyntax)setMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            var methodDeclaration = (MethodDeclarationSyntax)generator.MethodDeclaration(setMethod, desiredSetMethodName);

            if (setAccessorDeclaration.Body != null)
            {
                return methodDeclaration.WithBody(setAccessorDeclaration.Body)
                                        .WithAdditionalAnnotations(Formatter.Annotation);
            }
            else if (setAccessorDeclaration.ExpressionBody != null)
            {
                return methodDeclaration.WithBody(null)
                                        .WithExpressionBody(setAccessorDeclaration.ExpressionBody)
                                        .WithSemicolonToken(setAccessorDeclaration.SemicolonToken);
            }
            else if (propertyBackingField != null)
            {
                return methodDeclaration.WithBody(SyntaxFactory.Block(
                    (StatementSyntax)generator.ExpressionStatement(
                        generator.AssignmentStatement(
                            GetFieldReference(generator, propertyBackingField),
                            generator.IdentifierName("value")))));
            }

            return methodDeclaration;
        }

        private static SyntaxNode GetGetMethod(
            DocumentOptionSet documentOptions,
            ParseOptions parseOptions,
            SyntaxGenerator generator,
            PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol propertyBackingField,
            IMethodSymbol getMethod,
            string desiredGetMethodName,
            bool copyLeadingTrivia,
            CancellationToken cancellationToken)
        {
            var methodDeclaration = GetGetMethodWorker(
                generator, propertyDeclaration, propertyBackingField, getMethod,
                desiredGetMethodName, cancellationToken);

            methodDeclaration = CopyLeadingTrivia(propertyDeclaration, methodDeclaration, copyLeadingTrivia);

            return UseExpressionOrBlockBodyIfDesired(
                documentOptions, parseOptions, methodDeclaration,
                createReturnStatementForExpression: true);
        }

        private static MethodDeclarationSyntax CopyLeadingTrivia(
            PropertyDeclarationSyntax propertyDeclaration,
            MethodDeclarationSyntax methodDeclaration,
            bool copyLeadingTrivia)
        {
            if (copyLeadingTrivia)
            {
                var leadingTrivia = propertyDeclaration.GetLeadingTrivia();
                return methodDeclaration.WithLeadingTrivia(leadingTrivia.Select(ConvertTrivia));
            }

            return methodDeclaration;
        }

        private static SyntaxTrivia ConvertTrivia(SyntaxTrivia trivia)
        {
            if (trivia.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia ||
                trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia)
            {
                return ConvertDocumentationComment(trivia);
            }

            return trivia;
        }

        private static SyntaxTrivia ConvertDocumentationComment(SyntaxTrivia trivia)
        {
            var structure = trivia.GetStructure();
            var updatedStructure = (StructuredTriviaSyntax)ConvertValueToReturnsRewriter.Instance.Visit(structure);
            return SyntaxFactory.Trivia(updatedStructure);
        }

        private static SyntaxNode UseExpressionOrBlockBodyIfDesired(
            DocumentOptionSet documentOptions, ParseOptions parseOptions,
            MethodDeclarationSyntax methodDeclaration, bool createReturnStatementForExpression)
        {
            var expressionBodyPreference = documentOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods).Value;
            if (methodDeclaration?.Body != null && expressionBodyPreference != ExpressionBodyPreference.Never)
            {
                if (methodDeclaration.Body.TryConvertToExpressionBody(
                        parseOptions, expressionBodyPreference, out var arrowExpression, out var semicolonToken))
                {
                    return methodDeclaration.WithBody(null)
                                            .WithExpressionBody(arrowExpression)
                                            .WithSemicolonToken(semicolonToken)
                                            .WithAdditionalAnnotations(Formatter.Annotation);
                }
            }
            else if (methodDeclaration?.ExpressionBody != null && expressionBodyPreference == ExpressionBodyPreference.Never)
            {
                var block = methodDeclaration?.ExpressionBody.ConvertToBlock(
                    methodDeclaration.SemicolonToken, createReturnStatementForExpression);
                return methodDeclaration.WithExpressionBody(null)
                                        .WithSemicolonToken(default(SyntaxToken))
                                        .WithBody(block)
                                        .WithAdditionalAnnotations(Formatter.Annotation);
            }

            return methodDeclaration;
        }

        private static MethodDeclarationSyntax GetGetMethodWorker(
            SyntaxGenerator generator,
            PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol propertyBackingField,
            IMethodSymbol getMethod,
            string desiredGetMethodName,
            CancellationToken cancellationToken)
        {
            var methodDeclaration = (MethodDeclarationSyntax)generator.MethodDeclaration(getMethod, desiredGetMethodName);

            if (propertyDeclaration.ExpressionBody != null)
            {
                return methodDeclaration.WithBody(null)
                                        .WithExpressionBody(propertyDeclaration.ExpressionBody)
                                        .WithSemicolonToken(propertyDeclaration.SemicolonToken);
            }
            else
            {
                var getAccessorDeclaration = (AccessorDeclarationSyntax)getMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                if (getAccessorDeclaration?.ExpressionBody != null)
                {
                    return methodDeclaration.WithBody(null)
                                            .WithExpressionBody(getAccessorDeclaration.ExpressionBody)
                                            .WithSemicolonToken(getAccessorDeclaration.SemicolonToken);
                }
                if (getAccessorDeclaration?.Body != null)
                {
                    return methodDeclaration.WithBody(getAccessorDeclaration.Body)
                                            .WithAdditionalAnnotations(Formatter.Annotation);
                }
                else if (propertyBackingField != null)
                {
                    var fieldReference = GetFieldReference(generator, propertyBackingField);
                    return methodDeclaration.WithBody(
                        SyntaxFactory.Block(
                            (StatementSyntax)generator.ReturnStatement(fieldReference)));
                }
            }

            return methodDeclaration;
        }

        public override SyntaxNode GetPropertyNodeToReplace(SyntaxNode propertyDeclaration)
        {
            // For C# we'll have the property declaration that we want to replace.
            return propertyDeclaration;
        }

        protected override ExpressionSyntax UnwrapCompoundAssignment(
            SyntaxNode compoundAssignment, ExpressionSyntax readExpression)
        {
                var parent = (AssignmentExpressionSyntax)compoundAssignment;

                var operatorKind =
                    parent.IsKind(SyntaxKind.OrAssignmentExpression) ? SyntaxKind.BitwiseOrExpression :
                    parent.IsKind(SyntaxKind.AndAssignmentExpression) ? SyntaxKind.BitwiseAndExpression :
                    parent.IsKind(SyntaxKind.ExclusiveOrAssignmentExpression) ? SyntaxKind.ExclusiveOrExpression :
                    parent.IsKind(SyntaxKind.LeftShiftAssignmentExpression) ? SyntaxKind.LeftShiftExpression :
                    parent.IsKind(SyntaxKind.RightShiftAssignmentExpression) ? SyntaxKind.RightShiftExpression :
                    parent.IsKind(SyntaxKind.AddAssignmentExpression) ? SyntaxKind.AddExpression :
                    parent.IsKind(SyntaxKind.SubtractAssignmentExpression) ? SyntaxKind.SubtractExpression :
                    parent.IsKind(SyntaxKind.MultiplyAssignmentExpression) ? SyntaxKind.MultiplyExpression :
                    parent.IsKind(SyntaxKind.DivideAssignmentExpression) ? SyntaxKind.DivideExpression :
                    parent.IsKind(SyntaxKind.ModuloAssignmentExpression) ? SyntaxKind.ModuloExpression : SyntaxKind.None;

            return SyntaxFactory.BinaryExpression(operatorKind, readExpression, parent.Right.Parenthesize());
        }
    }
}