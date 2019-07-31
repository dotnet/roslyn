// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var currentOptionValue = optionSet.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas);
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
            var expressionBodyOpt = GetExpressionBody(declaration);
            if (expressionBodyOpt == null)
            {
                return (canOffer: false, fixesError: false);
            }

            // We need to know what sort of lambda this is (void returning or not) in order to be
            // able to create the right sort of block body (i.e. with a return-statement or
            // expr-statement).  So, if we can't figure out what lambda type this is, we should not
            // proceed.
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

            var currentOptionValue = optionSet.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas);
            var preference = currentOptionValue.Value;
            var userPrefersBlockBodies = preference == ExpressionBodyPreference.Never;
            var analyzerDisabled = currentOptionValue.Notification.Severity == ReportDiagnostic.Suppress;

            // If the user likes block bodies, then we offer block bodies from the diagnostic analyzer.
            // If the user does not like block bodies then we offer block bodies from the refactoring provider.
            // If the analyzer is disabled completely, the refactoring is enabled in both directions.
            canOffer = userPrefersBlockBodies == forAnalyzer || (!forAnalyzer && analyzerDisabled);
            return (canOffer, fixesError: false);
        }

        public static LambdaExpressionSyntax Update(
            SemanticModel semanticModel, bool useExpressionBody,
            LambdaExpressionSyntax originalDeclaration, LambdaExpressionSyntax currentDeclaration)
        {
            return UpdateWorker(semanticModel, useExpressionBody, originalDeclaration, currentDeclaration)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static LambdaExpressionSyntax UpdateWorker(
            SemanticModel semanticModel, bool useExpressionBody,
            LambdaExpressionSyntax originalDeclaration, LambdaExpressionSyntax currentDeclaration)
        {
            return useExpressionBody
                ? WithExpressionBody(currentDeclaration)
                : WithBlockBody(semanticModel, originalDeclaration, currentDeclaration);
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
            SemanticModel semanticModel, LambdaExpressionSyntax originalDeclaration, LambdaExpressionSyntax currentDeclaration)
        {
            var expressionBody = GetExpressionBody(currentDeclaration);
            var createReturnStatementForExpression = CreateReturnStatementForExpression(semanticModel, originalDeclaration);

            if (!expressionBody.TryConvertToStatement(
                    semicolonTokenOpt: default,
                    createReturnStatementForExpression,
                    out var statement))
            {

                return currentDeclaration;
            }

            // If the user is converting to a block, it's likely they intend to add multiple
            // statements to it.  So make a multi-line block so that things are formatted properly
            // for them to do so.
            return currentDeclaration.WithBody(SyntaxFactory.Block(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithAppendedTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed),
                SyntaxFactory.SingletonList(statement),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)));
        }

        private static bool CreateReturnStatementForExpression(
            SemanticModel semanticModel, LambdaExpressionSyntax declaration)
        {
            var lambdaType = (INamedTypeSymbol)semanticModel.GetTypeInfo(declaration).ConvertedType;
            if (lambdaType.DelegateInvokeMethod.ReturnsVoid)
            {
                return false;
            }

            // 'async Task' is effectively a void-returning lambda.  we do not want to create 
            // 'return statements' when converting.
            if (declaration.AsyncKeyword != default)
            {
                var returnType = lambdaType.DelegateInvokeMethod.ReturnType;
                if (returnType.IsErrorType())
                {
                    // "async Goo" where 'Goo' failed to bind.  If 'Goo' is 'Task' then it's
                    // reasonable to assume this is just a missing 'using' and that this is a true
                    // "async Task" lambda.  If the name isn't 'Task', then this looks like a
                    // real return type, and we should use return statements.
                    return returnType.Name != nameof(Task);
                }

                var taskType = semanticModel.Compilation.GetTypeByMetadataName(typeof(Task).FullName);
                if (returnType.Equals(taskType))
                {
                    // 'async Task'.  definitely do not create a 'return' statement;
                    return false;
                }
            }

            return true;
        }
    }
}
