// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    internal partial class UseExpressionBodyForLambdaCodeStyleProvider
        : AbstractCodeStyleProvider<ExpressionBodyPreference, UseExpressionBodyForLambdaCodeStyleProvider>
    {
        private static readonly LocalizableString UseExpressionBodyTitle = new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_lambda_expressions), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString UseBlockBodyTitle = new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_lambda_expressions), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        public UseExpressionBodyForLambdaCodeStyleProvider()
            : base(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas,
                   LanguageNames.CSharp,
                   IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId,
                   EnforceOnBuildValues.UseExpressionBodyForLambdaExpressions,
                   UseExpressionBodyTitle,
                   UseExpressionBodyTitle)
        {
        }

        // Shared code needed by all parts of the style provider for this feature.

        private static ExpressionSyntax GetBodyAsExpression(LambdaExpressionSyntax declaration)
            => declaration.Body as ExpressionSyntax;

        private static bool CanOfferUseExpressionBody(
            ExpressionBodyPreference preference, LambdaExpressionSyntax declaration, LanguageVersion languageVersion)
        {
            var userPrefersExpressionBodies = preference != ExpressionBodyPreference.Never;
            if (!userPrefersExpressionBodies)
            {
                // If the user doesn't even want expression bodies, then certainly do not offer.
                return false;
            }

            var expressionBody = GetBodyAsExpression(declaration);
            if (expressionBody != null)
            {
                // they already have an expression body.  so nothing to do here.
                return false;
            }

            // They don't have an expression body.  See if we could convert the block they 
            // have into one.
            return TryConvertToExpressionBody(declaration, languageVersion, preference, out _, out _);
        }

        private static bool TryConvertToExpressionBody(
            LambdaExpressionSyntax declaration,
            LanguageVersion languageVersion,
            ExpressionBodyPreference conversionPreference,
            out ExpressionSyntax expression,
            out SyntaxToken semicolon)
        {
            var body = declaration.Body as BlockSyntax;

            return body.TryConvertToExpressionBody(languageVersion, conversionPreference, out expression, out semicolon);
        }

        private static bool CanOfferUseBlockBody(
            SemanticModel semanticModel, ExpressionBodyPreference preference,
            LambdaExpressionSyntax declaration, CancellationToken cancellationToken)
        {
            var userPrefersBlockBodies = preference == ExpressionBodyPreference.Never;
            if (!userPrefersBlockBodies)
            {
                // If the user doesn't even want block bodies, then certainly do not offer.
                return false;
            }

            var expressionBodyOpt = GetBodyAsExpression(declaration);
            if (expressionBodyOpt == null)
            {
                // they already have a block body.
                return false;
            }

            // We need to know what sort of lambda this is (void returning or not) in order to be
            // able to create the right sort of block body (i.e. with a return-statement or
            // expr-statement).  So, if we can't figure out what lambda type this is, we should not
            // proceed.
            if (semanticModel.GetTypeInfo(declaration, cancellationToken).ConvertedType is not INamedTypeSymbol lambdaType || lambdaType.DelegateInvokeMethod == null)
            {
                return false;
            }

            var canOffer = expressionBodyOpt.TryConvertToStatement(
                semicolonTokenOpt: null, createReturnStatementForExpression: false, out _);
            if (!canOffer)
            {
                // Couldn't even convert the expression into statement form.
                return false;
            }

            var languageVersion = declaration.SyntaxTree.Options.LanguageVersion();
            if (expressionBodyOpt.IsKind(SyntaxKind.ThrowExpression) &&
                languageVersion < LanguageVersion.CSharp7)
            {
                // Can't convert this prior to C# 7 because ```a => throw ...``` isn't allowed.
                return false;
            }

            return true;
        }

        private static LambdaExpressionSyntax Update(SemanticModel semanticModel, LambdaExpressionSyntax originalDeclaration, LambdaExpressionSyntax currentDeclaration)
            => UpdateWorker(semanticModel, originalDeclaration, currentDeclaration).WithAdditionalAnnotations(Formatter.Annotation);

        private static LambdaExpressionSyntax UpdateWorker(
            SemanticModel semanticModel, LambdaExpressionSyntax originalDeclaration, LambdaExpressionSyntax currentDeclaration)
        {
            var expressionBody = GetBodyAsExpression(currentDeclaration);
            return expressionBody == null
                ? WithExpressionBody(currentDeclaration, originalDeclaration.GetLanguageVersion())
                : WithBlockBody(semanticModel, originalDeclaration, currentDeclaration);
        }

        private static LambdaExpressionSyntax WithExpressionBody(LambdaExpressionSyntax declaration, LanguageVersion languageVersion)
        {
            if (!TryConvertToExpressionBody(declaration, languageVersion, ExpressionBodyPreference.WhenPossible, out var expressionBody, out _))
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
            var expressionBody = GetBodyAsExpression(currentDeclaration);
            var createReturnStatementForExpression = CreateReturnStatementForExpression(
                semanticModel, originalDeclaration);

            if (!expressionBody.TryConvertToStatement(
                    semicolonTokenOpt: null,
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

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, title)
            {
            }
        }
    }

    // Stub classes needed only for exporting purposes.

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExpressionBodyForLambda), Shared]
    internal sealed class UseExpressionBodyForLambdaCodeFixProvider : UseExpressionBodyForLambdaCodeStyleProvider.CodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseExpressionBodyForLambdaCodeFixProvider()
        {
        }
    }

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseExpressionBodyForLambda), Shared]
    internal sealed class UseExpressionBodyForLambdaCodeRefactoringProvider : UseExpressionBodyForLambdaCodeStyleProvider.CodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseExpressionBodyForLambdaCodeRefactoringProvider()
        {
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class UseExpressionBodyForLambdaDiagnosticAnalyzer : UseExpressionBodyForLambdaCodeStyleProvider.DiagnosticAnalyzer
    {
    }
}
