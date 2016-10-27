using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal abstract class AbstractConvertToInterpolatedStringRefactoringProvider<TInvocationExpressionSyntax, TExpressionSyntax, TArgumentSyntax, TLiteralExpressionSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TArgumentSyntax : SyntaxNode
        where TLiteralExpressionSyntax : SyntaxNode
    {
        protected abstract SyntaxNode GetInterpolatedString(string text);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            var stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
            if (stringType == null)
            {
                return;
            }

            var formatMethods = stringType
                .GetMembers("Format")
                .OfType<IMethodSymbol>()
                .Where(ShouldIncludeFormatMethod)
                .ToImmutableArray();

            if (formatMethods.Length == 0)
            {
                return;
            }

            var syntaxFactsService = context.Document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFactsService == null)
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            TInvocationExpressionSyntax invocation;
            ISymbol invocationSymbol;
            if (TryFindInvocation(context.Span, root, semanticModel, formatMethods, syntaxFactsService, context.CancellationToken, out invocation, out invocationSymbol) &&
                IsArgumentListCorrect(syntaxFactsService.GetArgumentsForInvocationExpression(invocation), invocationSymbol, formatMethods, semanticModel, syntaxFactsService, context.CancellationToken))
            {
                context.RegisterRefactoring(
                    new ConvertToInterpolatedStringCodeAction(
                        FeaturesResources.Convert_to_interpolated_string,
                        c => CreateInterpolatedString(invocation, context.Document, syntaxFactsService, c)));
            }
        }

        private bool TryFindInvocation(
            TextSpan span,
            SyntaxNode root,
            SemanticModel semanticModel,
            ImmutableArray<IMethodSymbol> formatMethods,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken,
            out TInvocationExpressionSyntax invocation,
            out ISymbol invocationSymbol)
        {
            invocationSymbol = null;
            invocation = root.FindNode(span, getInnermostNodeForTie: true)?.FirstAncestorOrSelf<TInvocationExpressionSyntax>();
            while (invocation != null)
            {
                var arguments = syntaxFactsService.GetArgumentsForInvocationExpression(invocation);
                if (arguments.Count >= 2)
                {
                    var firstArgumentExpression = syntaxFactsService.GetExpressionOfArgument(arguments[0]) as TLiteralExpressionSyntax;
                    if (firstArgumentExpression != null && syntaxFactsService.IsStringLiteral(firstArgumentExpression.GetFirstToken()))
                    {
                        invocationSymbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
                        if (formatMethods.Contains(invocationSymbol))
                        {
                            break;
                        }
                    }
                }

                invocation = invocation.Parent?.FirstAncestorOrSelf<TInvocationExpressionSyntax>();
            }

            return invocation != null;
        }

        private bool IsArgumentListCorrect(
            SeparatedSyntaxList<TArgumentSyntax>? nullableArguments,
            ISymbol invocationSymbol,
            ImmutableArray<IMethodSymbol> formatMethods,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            var arguments = nullableArguments.Value;
            var firstExpression = syntaxFactsService.GetExpressionOfArgument(arguments[0]) as TLiteralExpressionSyntax;
            if (arguments.Count >= 2 &&
                firstExpression != null &&
                syntaxFactsService.IsStringLiteral(firstExpression.GetFirstToken()))
            {
                // We do not want to substitute the expression if it is being passed to params array argument
                // Example: 
                // string[] args;
                // String.Format("{0}{1}{2}", args);
                return IsArgumentListNotPassingArrayToParams(
                    syntaxFactsService.GetExpressionOfArgument(arguments[1]),
                    invocationSymbol,
                    formatMethods,
                    semanticModel,
                    cancellationToken);
            }

            return false;
        }


        private async Task<Document> CreateInterpolatedString(
            TInvocationExpressionSyntax invocation,
            Document document,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var arguments = syntaxFactsService.GetArgumentsForInvocationExpression(invocation);
            var literalExpression = syntaxFactsService.GetExpressionOfArgument(arguments[0]) as TLiteralExpressionSyntax;
            var text = literalExpression.GetFirstToken().ToString();
            var syntaxGenerator = document.Project.LanguageServices.GetService<SyntaxGenerator>();
            var expandedArguments = GetExpandedArguments(semanticModel, arguments, syntaxGenerator, syntaxFactsService);
            var interpolatedString = GetInterpolatedString(text);
            var newInterpolatedString = VisitArguments(expandedArguments, interpolatedString, syntaxFactsService);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(invocation, newInterpolatedString.WithTriviaFrom(invocation));
            return document.WithSyntaxRoot(newRoot);
        }

        private ImmutableArray<TExpressionSyntax> GetExpandedArguments(
            SemanticModel semanticModel,
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            SyntaxGenerator syntaxGenerator,
            ISyntaxFactsService syntaxFactsService)
        {
            var builder = ArrayBuilder<TExpressionSyntax>.GetInstance();
            for (int i = 1; i < arguments.Count; i++)
            {
                var argumentExpression = syntaxFactsService.GetExpressionOfArgument(arguments[i]);
                var convertedType = semanticModel.GetTypeInfo(argumentExpression).ConvertedType;
                if (convertedType == null)
                {
                    builder.Add(syntaxFactsService.Parenthesize(argumentExpression) as TExpressionSyntax);
                }
                else
                {
                    var castExpression = syntaxGenerator.CastExpression(convertedType, syntaxFactsService.Parenthesize(argumentExpression)).WithAdditionalAnnotations(Simplifier.Annotation);
                    builder.Add(castExpression as TExpressionSyntax);
                }
            }

            var expandedArguments = builder.ToImmutableAndFree();
            return expandedArguments;
        }

        private SyntaxNode VisitArguments(
            ImmutableArray<TExpressionSyntax> expandedArguments,
            SyntaxNode interpolatedString,
            ISyntaxFactsService syntaxFactsService)
        {
            return interpolatedString.ReplaceNodes(syntaxFactsService.GetContentsOfInterpolatedString(interpolatedString), (oldNode, newNode) =>
            {
                var interpolationSyntaxNode = newNode;
                if (interpolationSyntaxNode != null)
                {
                    var literalExpression = syntaxFactsService.GetExpressionOfInterpolation(interpolationSyntaxNode) as TLiteralExpressionSyntax;
                    if (literalExpression != null && syntaxFactsService.IsNumericLiteralExpression(literalExpression))
                    {
                        int index;

                        if (int.TryParse(literalExpression.GetFirstToken().ValueText, out index))
                        {
                            if (index >= 0 && index < expandedArguments.Length)
                            {
                                return interpolationSyntaxNode.ReplaceNode(
                                    syntaxFactsService.GetExpressionOfInterpolation(interpolationSyntaxNode),
                                    syntaxFactsService.ConvertToSingleLine(expandedArguments[index], useElasticTrivia: true).WithAdditionalAnnotations(Formatter.Annotation));
                            }
                        }
                    }
                }

                return newNode;
            });
        }

        private static bool ShouldIncludeFormatMethod(IMethodSymbol methodSymbol)
        {
            if (!methodSymbol.IsStatic)
            {
                return false;
            }

            if (methodSymbol.Parameters.Length == 0)
            {
                return false;
            }

            var firstParameter = methodSymbol.Parameters[0];
            if (firstParameter?.Name != "format")
            {
                return false;
            }

            return true;
        }

        private static bool IsArgumentListNotPassingArrayToParams(
            SyntaxNode expression,
            ISymbol invocationSymbol,
            ImmutableArray<IMethodSymbol> formatMethods,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var formatMethodsAcceptingParamsArray = formatMethods
                    .Where(x => x.Parameters.Length > 1 && x.Parameters[1].Type.Kind == SymbolKind.ArrayType);
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
