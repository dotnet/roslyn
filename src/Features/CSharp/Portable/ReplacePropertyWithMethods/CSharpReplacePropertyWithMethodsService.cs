﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ReplacePropertyWithMethods;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ReplacePropertyWithMethods
{
    [ExportLanguageService(typeof(IReplacePropertyWithMethodsService), LanguageNames.CSharp), Shared]
    internal class CSharpReplacePropertyWithMethodsService : 
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

        public override IList<SyntaxNode> GetReplacementMembers(
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

            return ConvertPropertyToMembers(
                SyntaxGenerator.GetGenerator(document), property,
                propertyDeclaration, propertyBackingField,
                desiredGetMethodName, desiredSetMethodName,
                cancellationToken);
        }

        private List<SyntaxNode> ConvertPropertyToMembers(
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
                    generator, propertyDeclaration, propertyBackingField,
                    getMethod, desiredGetMethodName, cancellationToken));
            }

            var setMethod = property.SetMethod;
            if (setMethod != null)
            {
                result.Add(GetSetMethod(
                    generator, propertyDeclaration, propertyBackingField, 
                    setMethod, desiredSetMethodName, cancellationToken));
            }

            return result;
        }

        private static SyntaxNode GetSetMethod(
            SyntaxGenerator generator, 
            PropertyDeclarationSyntax propertyDeclaration, 
            IFieldSymbol propertyBackingField,
            IMethodSymbol setMethod,
            string desiredSetMethodName,
            CancellationToken cancellationToken)
        {
            var setAccessorDeclaration = (AccessorDeclarationSyntax)setMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);

            var statements = new List<SyntaxNode>();
            if (setAccessorDeclaration?.Body != null)
            {
                statements.AddRange(setAccessorDeclaration.Body.Statements.Select(WithFormattingAnnotation));
            }
            else if (propertyBackingField != null)
            {
                statements.Add(generator.ExpressionStatement(
                    generator.AssignmentStatement(
                        GetFieldReference(generator, propertyBackingField),
                        generator.IdentifierName("value"))));
            }

            return generator.MethodDeclaration(setMethod, desiredSetMethodName, statements);
        }

        private static StatementSyntax WithFormattingAnnotation(StatementSyntax statement)
            => statement.WithAdditionalAnnotations(Formatter.Annotation);

        private static SyntaxNode GetGetMethod(
            SyntaxGenerator generator,
            PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol propertyBackingField,
            IMethodSymbol getMethod,
            string desiredGetMethodName,
            CancellationToken cancellationToken)
        {
            var statements = new List<SyntaxNode>();

            if (propertyDeclaration.ExpressionBody != null)
            {
                var returnKeyword = SyntaxFactory.Token(SyntaxKind.ReturnKeyword)
                                                 .WithTrailingTrivia(propertyDeclaration.ExpressionBody.ArrowToken.TrailingTrivia);

                var returnStatement = SyntaxFactory.ReturnStatement(
                    returnKeyword, 
                    propertyDeclaration.ExpressionBody.Expression,
                    propertyDeclaration.SemicolonToken);
                statements.Add(returnStatement);
            }
            else
            {
                var getAccessorDeclaration = (AccessorDeclarationSyntax)getMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                if (getAccessorDeclaration?.Body != null)
                {
                    statements.AddRange(getAccessorDeclaration.Body.Statements.Select(WithFormattingAnnotation));
                }
                else if (propertyBackingField != null)
                {
                    var fieldReference = GetFieldReference(generator, propertyBackingField);
                    statements.Add(generator.ReturnStatement(fieldReference));
                }
            }

            return generator.MethodDeclaration(getMethod, desiredGetMethodName, statements);
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