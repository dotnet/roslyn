// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ReplacePropertyWithMethods;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ReplacePropertyWithMethods
{
    [ExportLanguageService(typeof(IReplacePropertyWithMethodsService), LanguageNames.CSharp), Shared]
    internal partial class CSharpReplacePropertyWithMethodsService :
        AbstractReplacePropertyWithMethodsService<IdentifierNameSyntax, ExpressionSyntax, NameMemberCrefSyntax, StatementSyntax, PropertyDeclarationSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpReplacePropertyWithMethodsService()
        {
        }

        public override async Task<ImmutableArray<SyntaxNode>> GetReplacementMembersAsync(
            Document document,
            IPropertySymbol property,
            SyntaxNode propertyDeclarationNode,
            IFieldSymbol propertyBackingField,
            string desiredGetMethodName,
            string desiredSetMethodName,
            CancellationToken cancellationToken)
        {
            if (propertyDeclarationNode is not PropertyDeclarationSyntax propertyDeclaration)
                return ImmutableArray<SyntaxNode>.Empty;

            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var languageVersion = syntaxTree.Options.LanguageVersion();

            return ConvertPropertyToMembers(
                documentOptions, languageVersion,
                SyntaxGenerator.GetGenerator(document), property,
                propertyDeclaration, propertyBackingField,
                desiredGetMethodName, desiredSetMethodName,
                cancellationToken);
        }

        private static ImmutableArray<SyntaxNode> ConvertPropertyToMembers(
            DocumentOptionSet documentOptions,
            LanguageVersion languageVersion,
            SyntaxGenerator generator,
            IPropertySymbol property,
            PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol? propertyBackingField,
            string desiredGetMethodName,
            string desiredSetMethodName,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var result);

            if (propertyBackingField != null)
            {
                var initializer = propertyDeclaration.Initializer?.Value;
                result.Add(generator.FieldDeclaration(propertyBackingField, initializer));
            }

            var getMethod = property.GetMethod;
            if (getMethod != null)
            {
                result.Add(GetGetMethod(
                    documentOptions, languageVersion,
                    generator, propertyDeclaration, propertyBackingField,
                    getMethod, desiredGetMethodName,
                    cancellationToken: cancellationToken));
            }

            var setMethod = property.SetMethod;
            if (setMethod != null)
            {
                result.Add(GetSetMethod(
                    documentOptions, languageVersion,
                    generator, propertyDeclaration, propertyBackingField,
                    setMethod, desiredSetMethodName,
                    cancellationToken: cancellationToken));
            }

            return result.ToImmutable();
        }

        private static SyntaxNode GetSetMethod(
            DocumentOptionSet documentOptions,
            LanguageVersion languageVersion,
            SyntaxGenerator generator,
            PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol? propertyBackingField,
            IMethodSymbol setMethod,
            string desiredSetMethodName,
            CancellationToken cancellationToken)
        {
            var methodDeclaration = GetSetMethodWorker(
                generator, propertyDeclaration, propertyBackingField,
                setMethod, desiredSetMethodName, cancellationToken);

            // The analyzer doesn't report diagnostics when the trivia contains preprocessor directives, so it's safe
            // to copy the complete leading trivia to both generated methods.
            methodDeclaration = CopyLeadingTrivia(propertyDeclaration, methodDeclaration, ConvertValueToParamRewriter.Instance);

            return UseExpressionOrBlockBodyIfDesired(
                documentOptions, languageVersion, methodDeclaration,
                createReturnStatementForExpression: false);
        }

        private static MethodDeclarationSyntax GetSetMethodWorker(
            SyntaxGenerator generator,
            PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol? propertyBackingField,
            IMethodSymbol setMethod,
            string desiredSetMethodName,
            CancellationToken cancellationToken)
        {
            var setAccessorDeclaration = (AccessorDeclarationSyntax)setMethod.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            var methodDeclaration = (MethodDeclarationSyntax)generator.MethodDeclaration(setMethod, desiredSetMethodName);

            // property has unsafe, but generator didn't add it to the method, so we have to add it here
            if (propertyDeclaration.Modifiers.Any(SyntaxKind.UnsafeKeyword)
                && !methodDeclaration.Modifiers.Any(SyntaxKind.UnsafeKeyword))
            {
                methodDeclaration = methodDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
            }

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
            LanguageVersion languageVersion,
            SyntaxGenerator generator,
            PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol? propertyBackingField,
            IMethodSymbol getMethod,
            string desiredGetMethodName,
            CancellationToken cancellationToken)
        {
            var methodDeclaration = GetGetMethodWorker(
                generator, propertyDeclaration, propertyBackingField, getMethod,
                desiredGetMethodName, cancellationToken);

            methodDeclaration = CopyLeadingTrivia(propertyDeclaration, methodDeclaration, ConvertValueToReturnsRewriter.Instance);

            return UseExpressionOrBlockBodyIfDesired(
                documentOptions, languageVersion, methodDeclaration,
                createReturnStatementForExpression: true);
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
            if (trivia.Kind() is SyntaxKind.MultiLineDocumentationCommentTrivia or
                SyntaxKind.SingleLineDocumentationCommentTrivia)
            {
                return ConvertDocumentationComment(trivia, rewriter);
            }

            return trivia;
        }

        private static SyntaxTrivia ConvertDocumentationComment(SyntaxTrivia trivia, CSharpSyntaxRewriter rewriter)
        {
            var structure = trivia.GetStructure();
            var rewritten = rewriter.Visit(structure);
            Contract.ThrowIfNull(rewritten);
            return SyntaxFactory.Trivia((StructuredTriviaSyntax)rewritten);
        }

        private static SyntaxNode UseExpressionOrBlockBodyIfDesired(
            DocumentOptionSet documentOptions, LanguageVersion languageVersion,
            MethodDeclarationSyntax methodDeclaration, bool createReturnStatementForExpression)
        {
            var expressionBodyPreference = documentOptions.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods).Value;
            if (methodDeclaration.Body != null && expressionBodyPreference != ExpressionBodyPreference.Never)
            {
                if (methodDeclaration.Body.TryConvertToArrowExpressionBody(
                        methodDeclaration.Kind(), languageVersion, expressionBodyPreference,
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

        private static MethodDeclarationSyntax GetGetMethodWorker(
            SyntaxGenerator generator,
            PropertyDeclarationSyntax propertyDeclaration,
            IFieldSymbol? propertyBackingField,
            IMethodSymbol getMethod,
            string desiredGetMethodName,
            CancellationToken cancellationToken)
        {
            var methodDeclaration = (MethodDeclarationSyntax)generator.MethodDeclaration(getMethod, desiredGetMethodName);

            // property has unsafe, but generator didn't add it to the method, so we have to add it here
            if (propertyDeclaration.Modifiers.Any(SyntaxKind.UnsafeKeyword)
                && !methodDeclaration.Modifiers.Any(SyntaxKind.UnsafeKeyword))
            {
                methodDeclaration = methodDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
            }

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
                var parameter = SyntaxFactory.CrefParameter(typeSyntax);
                parameterList = SyntaxFactory.CrefParameterList(SyntaxFactory.SingletonSeparatedList(parameter));
            }
            else
            {
                parameterList = SyntaxFactory.CrefParameterList();
            }

            // XmlCrefAttribute replaces <T> with {T}, which is required for C# documentation comments
            var crefAttribute = SyntaxFactory.XmlCrefAttribute(
                SyntaxFactory.NameMemberCref(SyntaxFactory.IdentifierName(identifierToken), parameterList));
            return (NameMemberCrefSyntax)crefAttribute.Cref;
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
