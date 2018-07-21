// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    /// <summary>
    /// Helper class that allows us to share lots of logic between the diagnostic analyzer and the
    /// code refactoring provider.  Those can't share a common base class due to their own inheritance
    /// requirements with <see cref="DiagnosticAnalyzer"/> and <see cref="CodeRefactoringProvider"/>.
    /// </summary>
    internal static class UseExpressionBodyForLambdaHelpers
    {
        public static readonly LocalizableString UseExpressionBodyTitle = new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_lambda_expressions), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        public static readonly LocalizableString UseBlockBodyTitle = new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_lambda_expressions), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        public static ExpressionSyntax GetExpressionBody(LambdaExpressionSyntax declaration)
            => declaration.Body as ExpressionSyntax;

        public static bool CanOfferUseExpressionBody(
            OptionSet optionSet, LambdaExpressionSyntax declaration, bool forAnalyzer)
        {
            var currentOptionValue = optionSet.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdaExpressions);
            var preference = currentOptionValue.Value;
            var userPrefersExpressionBodies = preference != ExpressionBodyPreference.Never;
            var analyzerDisabled = currentOptionValue.Notification.Severity == ReportDiagnostic.Suppress;

            // If the user likes expression bodies, then we offer expression bodies from the diagnostic analyzer.
            // If the user does not like expression bodies then we offer expression bodies from the refactoring provider.
            // If the analyzer is disabled completely, the refactoring is enabled in both directions.
            if (userPrefersExpressionBodies == forAnalyzer || (!forAnalyzer && analyzerDisabled))
            {
                var expressionBody = GetExpressionBody(declaration);
                if (expressionBody == null)
                {
                    // They don't have an expression body.  See if we could convert the block they 
                    // have into one.

                    var options = declaration.SyntaxTree.Options;
                    var conversionPreference = forAnalyzer ? preference : ExpressionBodyPreference.WhenPossible;

                    return TryConvertToExpressionBody(declaration, options, conversionPreference, out _, out _);
                }
            }

            return false;
        }

        private static bool TryConvertToExpressionBody(
            LambdaExpressionSyntax declaration,
            ParseOptions options, ExpressionBodyPreference conversionPreference, 
            out ExpressionSyntax expressionWhenOnSingleLine, 
            out SyntaxToken semicolonWhenOnSingleLine)
        {
            return TryConvertToExpressionBodyWorker(
                declaration, options, conversionPreference,
                out expressionWhenOnSingleLine, out semicolonWhenOnSingleLine);
        }

        private static bool TryConvertToExpressionBodyWorker(
            LambdaExpressionSyntax declaration, ParseOptions options, ExpressionBodyPreference conversionPreference,
            out ExpressionSyntax expressionWhenOnSingleLine, out SyntaxToken semicolonWhenOnSingleLine)
        {
            var body = declaration.Body as BlockSyntax;

            return body.TryConvertToExpressionBody(
                declaration.Kind(), options, conversionPreference,
                out expressionWhenOnSingleLine, out semicolonWhenOnSingleLine);
        }

        public static (bool canOffer, bool fixesError) CanOfferUseBlockBody(
            SemanticModel semanticModel, OptionSet optionSet,
            LambdaExpressionSyntax declaration, bool forAnalyzer,
            CancellationToken cancellationToken)
        {
            var currentOptionValue = optionSet.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdaExpressions);
            var preference = currentOptionValue.Value;
            var userPrefersBlockBodies = preference == ExpressionBodyPreference.Never;
            var analyzerDisabled = currentOptionValue.Notification.Severity == ReportDiagnostic.Suppress;

            var expressionBodyOpt = GetExpressionBody(declaration);
            if (expressionBodyOpt == null)
            {
                return (canOffer: false, fixesError: false);
            }

            var lambdaType = semanticModel.GetTypeInfo(declaration, cancellationToken).ConvertedType as INamedTypeSymbol;
            if (lambdaType == null || lambdaType.DelegateInvokeMethod == null)
            {
                return (canOffer: false, fixesError: false);
            }

            var canOffer = expressionBodyOpt.TryConvertToStatement(
                semicolonTokenOpt: default, createReturnStatementForExpression: false, out var statement) == true;
            if (!canOffer)
            {
                return (canOffer: false, fixesError: false);
            }

            var languageVersion = ((CSharpParseOptions)declaration.SyntaxTree.Options).LanguageVersion;
            if (expressionBodyOpt.IsKind(SyntaxKind.ThrowExpression) &&
                languageVersion < LanguageVersion.CSharp7)
            {
                // If they're using a throw expression in a declaration and it's prior to C# 7
                // then always mark this as something that can be fixed by the analyzer.  This way
                // we'll also get 'fix all' working to fix all these cases.
                return (canOffer, fixesError: true);
            }

            // If the user likes block bodies, then we offer block bodies from the diagnostic analyzer.
            // If the user does not like block bodies then we offer block bodies from the refactoring provider.
            // If the analyzer is disabled completely, the refactoring is enabled in both directions.
            canOffer = userPrefersBlockBodies == forAnalyzer || (!forAnalyzer && analyzerDisabled);
            return (canOffer, fixesError: false);
        }

        public static LambdaExpressionSyntax Update(
            SemanticModel semanticModel, LambdaExpressionSyntax declaration, bool useExpressionBody)
        {
            return UpdateWorker(semanticModel, declaration, useExpressionBody).WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static LambdaExpressionSyntax UpdateWorker(
            SemanticModel semanticModel, LambdaExpressionSyntax declaration, bool useExpressionBody)
        {
            return useExpressionBody 
                ? WithExpressionBody(declaration)
                : WithBlockBody(semanticModel, declaration);
        }

        private static LambdaExpressionSyntax WithExpressionBody(LambdaExpressionSyntax declaration)
        {
            if (!TryConvertToExpressionBody(
                    declaration, declaration.SyntaxTree.Options, ExpressionBodyPreference.WhenPossible,
                    out var expressionBody, out _))
            {
                return declaration;
            }

            var updatedDecl = declaration.WithBody(expressionBody);

            // If there will only be whitespace between the arrow and the body, then replace that
            // with a single space so that the lambda doesn't have superfluous newlines in it.
            if (declaration.ArrowToken.TrailingTrivia.All(t => t.IsWhitespaceOrEndOfLine()) &&
                expressionBody.GetLeadingTrivia().All(t => t.IsWhitespaceOrEndOfLine()))
            {
                updatedDecl = updatedDecl.WithArrowToken(updatedDecl.ArrowToken.WithTrailingTrivia(SyntaxFactory.ElasticSpace));
            }

            return updatedDecl;
        }

        private static LambdaExpressionSyntax WithBlockBody(
            SemanticModel semanticModel, LambdaExpressionSyntax declaration)
        {
            var expressionBody = GetExpressionBody(declaration);
            var lambdaType = (INamedTypeSymbol)semanticModel.GetTypeInfo(declaration).ConvertedType;
            var createReturnStatementForExpression = !lambdaType.DelegateInvokeMethod.ReturnsVoid;

            if (!expressionBody.TryConvertToStatement(
                    semicolonTokenOpt: default,
                    createReturnStatementForExpression,
                    out var statement))
            {

                return declaration;
            }

            // If the user is converting to a block, it's likely they intend to add multiple
            // statements to it.  So make a multi-line block so that things are formatted properly
            // for them to do so.
            return declaration.WithBody(SyntaxFactory.Block(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithAppendedTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed),
                SyntaxFactory.SingletonList(statement),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)));
        }
    }
}
