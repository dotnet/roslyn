using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal abstract class AbstractConvertToInterpolatedStringRefactoringProvider<TInterpolatedStringExpressionSyntax, TInvocationExpressionSyntax, TExpressionSyntax, TArgumentSyntax, TLiteralExpressionSyntax> : CodeRefactoringProvider
        where TInterpolatedStringExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TArgumentSyntax : SyntaxNode
        where TLiteralExpressionSyntax : SyntaxNode
    {
        protected static SyntaxAnnotation SpecializedFormattingAnnotation = new SyntaxAnnotation();
        protected abstract SeparatedSyntaxList<TArgumentSyntax>? GetArguments(TInvocationExpressionSyntax invocation);
        protected abstract ImmutableArray<TExpressionSyntax> GetExpandedArguments(SemanticModel semanticModel, SeparatedSyntaxList<TArgumentSyntax> arguments);
        protected abstract TLiteralExpressionSyntax GetFirstArgument(SeparatedSyntaxList<TArgumentSyntax> arguments);
        protected abstract IEnumerable<IFormattingRule> GetFormattingRules(Document document);
        protected abstract TInterpolatedStringExpressionSyntax GetInterpolatedString(string text);
        protected abstract string GetText(SeparatedSyntaxList<TArgumentSyntax> arguments);
        protected abstract bool IsArgumentListCorrect(TInvocationExpressionSyntax invocation, ISymbol invocationSymbol, ImmutableArray<ISymbol> formatMethods, SemanticModel semanticModel, CancellationToken cancellationToken);
        protected abstract bool IsStringLiteral(TLiteralExpressionSyntax firstArgument);
        protected abstract TInterpolatedStringExpressionSyntax VisitArguments(ImmutableArray<TExpressionSyntax> expandedArguments, TInterpolatedStringExpressionSyntax interpolatedString);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            var stringType = semanticModel.Compilation.GetTypeByMetadataName("System.String");
            if (stringType == null)
            {
                return;
            }

            var formatMethods = stringType
                .GetMembers("Format")
                .RemoveAll(ShouldRemoveStringFormatMethod);

            if (formatMethods.Length == 0)
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            TInvocationExpressionSyntax invocation;
            ISymbol invocationSymbol;
            if (TryFindInvocation(context.Span, root, semanticModel, formatMethods, context.CancellationToken, out invocation, out invocationSymbol) &&
                IsArgumentListCorrect(invocation, invocationSymbol, formatMethods, semanticModel, context.CancellationToken))
            {
                context.RegisterRefactoring(
                    new ConvertToInterpolatedStringCodeAction(FeaturesResources.ConvertToInterpolatedString, c => CreateInterpolatedString(invocation, context.Document, c)));
            }
        }

        private bool TryFindInvocation(
            TextSpan span,
            SyntaxNode root,
            SemanticModel semanticModel,
            ImmutableArray<ISymbol> formatMethods,
            CancellationToken cancellationToken,
            out TInvocationExpressionSyntax invocation,
            out ISymbol invocationSymbol)
        {
            invocationSymbol = null;
            invocation = root.FindNode(span, getInnermostNodeForTie: true)?.FirstAncestorOrSelf<TInvocationExpressionSyntax>();
            while (invocation != null)
            {
                var nullableArguments = GetArguments(invocation);
                if (nullableArguments != null)
                {
                    var arguments = nullableArguments.Value;
                    if (arguments.Count >= 2)
                    {
                        var firstArgument = GetFirstArgument(arguments);
                        if (IsStringLiteral(firstArgument))
                        {
                            invocationSymbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
                            if (formatMethods.Contains(invocationSymbol))
                            {
                                break;
                            }
                        }
                    }
                }

                invocation = invocation.Parent?.FirstAncestorOrSelf<TInvocationExpressionSyntax>();
            }

            return invocation != null;
        }


        private async Task<Document> CreateInterpolatedString(TInvocationExpressionSyntax invocation, Document document, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var arguments = GetArguments(invocation).Value;
            string text = GetText(arguments);
            var expandedArguments = GetExpandedArguments(semanticModel, arguments);
            var interpolatedString = GetInterpolatedString(text);
            var newInterpolatedString = VisitArguments(expandedArguments, interpolatedString);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(invocation, newInterpolatedString.WithTriviaFrom(invocation));
            newRoot = await FormatAsync(newRoot, document, cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<SyntaxNode> FormatAsync(SyntaxNode newRoot, Document document, CancellationToken cancellationToken)
        {
            var formattingRules = GetFormattingRules(document);
            if (formattingRules == null)
            {
                return newRoot;
            }

            return await Formatter.FormatAsync(
                newRoot,
                SpecializedFormattingAnnotation,
                document.Project.Solution.Workspace,
                options: null,
                rules: formattingRules,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static bool ShouldRemoveStringFormatMethod(ISymbol symbol)
        {
            if (symbol.Kind != SymbolKind.Method || !symbol.IsStatic)
            {
                return true;
            }

            var methodSymbol = (IMethodSymbol)symbol;
            if (methodSymbol.Parameters.Length == 0)
            {
                return true;
            }

            var firstParameter = methodSymbol.Parameters[0];
            if (firstParameter?.Name != "format")
            {
                return true;
            }

            return false;
        }

        protected bool IsArgumentListNotPassingArrayToParams(
            SyntaxNode expression,
            ISymbol invocationSymbol,
            ImmutableArray<ISymbol> formatMethods,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var formatMethodsAcceptingParamsArray = formatMethods
                    .OfType<IMethodSymbol>()
                    .Where(x =>
                        x.Parameters.Length > 1
                            ? x.Parameters[1].Type.Kind == SymbolKind.ArrayType
                            : false);
            if (formatMethodsAcceptingParamsArray.Contains(invocationSymbol))
            {
                return semanticModel.GetTypeInfo(expression, cancellationToken).Type?.Kind != SymbolKind.ArrayType;
            }

            return true;
        }

        private class ConvertToInterpolatedStringCodeAction : CodeAction.DocumentChangeAction
        {
            public ConvertToInterpolatedStringCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
