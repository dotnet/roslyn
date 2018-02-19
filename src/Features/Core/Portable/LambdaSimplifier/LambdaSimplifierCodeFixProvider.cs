using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LambdaSimplifier
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    internal class LambdaSimplifierCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.LambdaSimplifierDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => BatchFixAllProvider.Instance;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => SimplifyLambdaAsync(context, c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        private async Task<Document> SimplifyLambdaAsync(
            CodeFixContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var lambdaNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var lambdaExpression = (IAnonymousFunctionOperation)semanticModel.GetOperation(lambdaNode, cancellationToken);

            var annotation = new SyntaxAnnotation();
            var invocationExpression = LambdaSimplifierDiagnosticAnalyzer.TryGetInvocationExpression(lambdaExpression);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var expression = syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression.Syntax);
            expression = syntaxFacts.Parenthesize(expression.WithAdditionalAnnotations(annotation));

            var newDocument = document.WithSyntaxRoot(
                root.ReplaceNode(lambdaExpression.Syntax, expression));

            var preservesSemantics = await SimplificationPreservesSemanticsAsync(
                newDocument, invocationExpression, expression, annotation, cancellationToken).ConfigureAwait(false);

            if (preservesSemantics)
            {
                return newDocument;
            }

            // If semantic change, give the user an appropriate warning.
            expression = expression.WithAdditionalAnnotations(
                WarningAnnotation.Create(FeaturesResources.Warning_code_meaning_changes_after_simplifying_lambda));
            return document.WithSyntaxRoot(root.ReplaceNode(lambdaExpression.Syntax, expression));
        }

        private async Task<bool> SimplificationPreservesSemanticsAsync(
            Document newDocument,
            IInvocationOperation invocationExpression,
            SyntaxNode oldExpressionNode,
            SyntaxAnnotation annotation,
            CancellationToken cancellationToken)
        {
            var oldTargetMethod = invocationExpression.TargetMethod;

            // Try the rewrite.  If the new code binds to the same target method, then this 
            // change is safe to perform.

            var newSemanticModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var newSyntaxRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newExpressionNode = newSyntaxRoot.GetAnnotatedNodes(annotation).Single();

            var newTargetMethod = newSemanticModel.GetSymbolInfo(newExpressionNode).Symbol;

            return SymbolEquivalenceComparer.Instance.Equals(oldTargetMethod, newTargetMethod);
        }

        public static string FixAllEquivalenceKey = FeaturesResources.Simplify_lambda_expression;

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Simplify_lambda_expression,
                       createChangedDocument,
                       FixAllEquivalenceKey)
            {
            }
        }
    }
}
