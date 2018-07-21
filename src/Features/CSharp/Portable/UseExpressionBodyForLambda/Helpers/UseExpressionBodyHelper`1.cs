// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    /// <summary>
    /// Helper class that allows us to share lots of logic between the diagnostic analyzer and the
    /// code refactoring provider.  Those can't share a common base class due to their own inheritance
    /// requirements with <see cref="DiagnosticAnalyzer"/> and <see cref="CodeRefactoringProvider"/>.
    /// </summary>
    internal class UseExpressionBodyHelper
    {
        public static readonly UseExpressionBodyHelper Instance = new UseExpressionBodyHelper();

        public Option<CodeStyleOption<ExpressionBodyPreference>> Option { get; }
        public LocalizableString UseExpressionBodyTitle { get; }
        public LocalizableString UseBlockBodyTitle { get; }
        public string DiagnosticId { get; }
        public ImmutableArray<SyntaxKind> SyntaxKinds { get; }

        public UseExpressionBodyHelper()
        {
            DiagnosticId = IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId;
            Option = CSharpCodeStyleOptions.PreferExpressionBodiedLambdaExpressions;
            UseExpressionBodyTitle = new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_lambda_expressions), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            UseBlockBodyTitle = new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_lambda_expressions), FeaturesResources.ResourceManager, typeof(FeaturesResources));
            SyntaxKinds = ImmutableArray.Create(SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
        }

        public Location GetDiagnosticLocation(LambdaExpressionSyntax declaration)
            => this.GetBody(declaration).Statements[0].GetLocation();

        public BlockSyntax GetBody(LambdaExpressionSyntax declaration)
            => declaration.Body as BlockSyntax;

        public ExpressionSyntax GetExpressionBody(LambdaExpressionSyntax declaration)
            => declaration.Body as ExpressionSyntax;

        public bool CanOfferUseExpressionBody(
            OptionSet optionSet, LambdaExpressionSyntax declaration, bool forAnalyzer)
        {
            var currentOptionValue = optionSet.GetOption(Option);
            var preference = currentOptionValue.Value;
            var userPrefersExpressionBodies = preference != ExpressionBodyPreference.Never;
            var analyzerDisabled = currentOptionValue.Notification.Severity == ReportDiagnostic.Suppress;

            // If the user likes expression bodies, then we offer expression bodies from the diagnostic analyzer.
            // If the user does not like expression bodies then we offer expression bodies from the refactoring provider.
            // If the analyzer is disabled completely, the refactoring is enabled in both directions.
            if (userPrefersExpressionBodies == forAnalyzer || (!forAnalyzer && analyzerDisabled))
            {
                var expressionBody = this.GetExpressionBody(declaration);
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

        protected virtual bool TryConvertToExpressionBody(
            LambdaExpressionSyntax declaration,
            ParseOptions options, ExpressionBodyPreference conversionPreference, 
            out ExpressionSyntax expressionWhenOnSingleLine, 
            out SyntaxToken semicolonWhenOnSingleLine)
        {
            return TryConvertToExpressionBodyWorker(
                declaration, options, conversionPreference,
                out expressionWhenOnSingleLine, out semicolonWhenOnSingleLine);
        }

        private bool TryConvertToExpressionBodyWorker(
            LambdaExpressionSyntax declaration, ParseOptions options, ExpressionBodyPreference conversionPreference,
            out ExpressionSyntax expressionWhenOnSingleLine, out SyntaxToken semicolonWhenOnSingleLine)
        {
            var body = this.GetBody(declaration);

            return body.TryConvertToExpressionBody(
                declaration.Kind(), options, conversionPreference,
                out expressionWhenOnSingleLine, out semicolonWhenOnSingleLine);
        }

        public (bool canOffer, bool fixesError) CanOfferUseBlockBody(
            SemanticModel semanticModel, OptionSet optionSet,
            LambdaExpressionSyntax declaration, bool forAnalyzer,
            CancellationToken cancellationToken)
        {
            var currentOptionValue = optionSet.GetOption(Option);
            var preference = currentOptionValue.Value;
            var userPrefersBlockBodies = preference == ExpressionBodyPreference.Never;
            var analyzerDisabled = currentOptionValue.Notification.Severity == ReportDiagnostic.Suppress;

            var expressionBodyOpt = this.GetExpressionBody(declaration);
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

        public LambdaExpressionSyntax Update(
            SemanticModel semanticModel, LambdaExpressionSyntax declaration, OptionSet options, 
            ParseOptions parseOptions, bool useExpressionBody)
        {
            if (useExpressionBody)
            {
                TryConvertToExpressionBody(
                    declaration, declaration.SyntaxTree.Options, ExpressionBodyPreference.WhenPossible,
                    out var expressionBody, out var semicolonToken);

                var trailingTrivia = semicolonToken.TrailingTrivia
                                                   .Where(t => t.Kind() != SyntaxKind.EndOfLineTrivia)
                                                   .Concat(declaration.GetTrailingTrivia());
                expressionBody = expressionBody.WithAppendedTrailingTrivia(trailingTrivia);

                return declaration.WithBody(expressionBody);
            }
            else
            {
                return WithGenerateBody(semanticModel, declaration, options, parseOptions);
            }
        }

        protected bool CreateReturnStatementForExpression(
            SemanticModel semanticModel, LambdaExpressionSyntax declaration)
        {
            var lambda = (INamedTypeSymbol)semanticModel.GetTypeInfo(declaration).ConvertedType;
            return !lambda.DelegateInvokeMethod.ReturnsVoid;
        }

        protected virtual LambdaExpressionSyntax WithGenerateBody(
            SemanticModel semanticModel, LambdaExpressionSyntax declaration,
            OptionSet options, ParseOptions parseOptions)
        {
            var expressionBody = GetExpressionBody(declaration);

            if (expressionBody.TryConvertToStatement(
                    semicolonTokenOpt: default,
                    CreateReturnStatementForExpression(semanticModel, declaration),
                    out var statement))
            {
                return declaration.WithBody(SyntaxFactory.Block(statement));
            }

            return declaration;
        }
    }
}
