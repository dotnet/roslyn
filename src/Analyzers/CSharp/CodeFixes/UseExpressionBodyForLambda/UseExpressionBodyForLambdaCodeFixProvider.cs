// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExpressionBodyForLambda), Shared]
    internal sealed class UseExpressionBodyForLambdaCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseExpressionBodyForLambdaCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics[0];

            var title = diagnostic.GetMessage();
            var codeAction = CodeAction.Create(
                title,
                c => FixWithSyntaxEditorAsync(document, diagnostic, c),
                title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
            => FixAllAsync(document, diagnostics, editor, cancellationToken);

        private static async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(editor, semanticModel, diagnostic, cancellationToken);
            }
        }

        private static Task<Document> FixWithSyntaxEditorAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
            => FixAllWithEditorAsync(
                document, editor => FixAllAsync(document, ImmutableArray.Create(diagnostic), editor, cancellationToken), cancellationToken);

        private static void AddEdits(
            SyntaxEditor editor, SemanticModel semanticModel,
            Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var declarationLocation = diagnostic.AdditionalLocations[0];
            var originalDeclaration = (LambdaExpressionSyntax)declarationLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);

            editor.ReplaceNode(
                originalDeclaration,
                (current, _) => Update(semanticModel, originalDeclaration, (LambdaExpressionSyntax)current));
        }

        private static LambdaExpressionSyntax Update(SemanticModel semanticModel, LambdaExpressionSyntax originalDeclaration, LambdaExpressionSyntax currentDeclaration)
            => UpdateWorker(semanticModel, originalDeclaration, currentDeclaration).WithAdditionalAnnotations(Formatter.Annotation);

        private static LambdaExpressionSyntax UpdateWorker(
            SemanticModel semanticModel, LambdaExpressionSyntax originalDeclaration, LambdaExpressionSyntax currentDeclaration)
        {
            var expressionBody = UseExpressionBodyForLambdaDiagnosticAnalyzer.GetBodyAsExpression(currentDeclaration);
            return expressionBody == null
                ? WithExpressionBody(currentDeclaration, originalDeclaration.GetLanguageVersion())
                : WithBlockBody(semanticModel, originalDeclaration, currentDeclaration, expressionBody);
        }

        private static LambdaExpressionSyntax WithExpressionBody(LambdaExpressionSyntax declaration, LanguageVersion languageVersion)
        {
            if (!UseExpressionBodyForLambdaDiagnosticAnalyzer.TryConvertToExpressionBody(declaration, languageVersion, ExpressionBodyPreference.WhenPossible, out var expressionBody))
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
            SemanticModel semanticModel, LambdaExpressionSyntax originalDeclaration, LambdaExpressionSyntax currentDeclaration, ExpressionSyntax expressionBody)
        {
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
            var lambdaType = (INamedTypeSymbol)semanticModel.GetTypeInfo(declaration).ConvertedType!;
            if (lambdaType.DelegateInvokeMethod!.ReturnsVoid)
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

    // PROTOTYPE: TODO
    //[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseExpressionBodyForLambda), Shared]
    //internal sealed class UseExpressionBodyForLambdaCodeRefactoringProvider : UseExpressionBodyForLambdaCodeStyleProvider.CodeRefactoringProvider
    //{
    //    [ImportingConstructor]
    //    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    //    public UseExpressionBodyForLambdaCodeRefactoringProvider()
    //    {
    //    }
    //}
}
