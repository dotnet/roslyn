// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertToInterpolatedString
{
    internal abstract class AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider<TInvocationExpressionSyntax, TExpressionSyntax, TArgumentSyntax, TLiteralExpressionSyntax, TArgumentListExpressionSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TArgumentSyntax : SyntaxNode
        where TLiteralExpressionSyntax : SyntaxNode
        where TArgumentListExpressionSyntax : SyntaxNode
    {
        protected abstract SyntaxNode GetInterpolatedString(string text);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);

            if (stringType is null)
            {
                return;
            }

            var consoleType = semanticModel.Compilation.GetTypeByMetadataName(typeof(Console).FullName);

            var stringMethods = CollectMethods(stringType, nameof(string.Format));
            var consoleMethods = CollectMethods(consoleType, nameof(Console.Write), nameof(Console.WriteLine));


            var replaceInvocationMethods = stringMethods;
            var keepInvocationMethods = consoleMethods;

            if (replaceInvocationMethods.Length == 0 && keepInvocationMethods.Length == 0)
            {
                return;
            }

            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFactsService == null)
            {
                return;
            }

            var addedRefactorings = await TryAddRefactoringsAsync(
                context,
                document,
                textSpan,
                semanticModel,
                replaceInvocationMethods,
                syntaxFactsService,
                true,
                cancellationToken).ConfigureAwait(false);

            if (!addedRefactorings)
            {
                await TryAddRefactoringsAsync(
                    context,
                    document,
                    textSpan,
                    semanticModel,
                    keepInvocationMethods,
                    syntaxFactsService,
                    false,
                    cancellationToken).ConfigureAwait(false);
            }

            // Local Functions

            static ImmutableArray<IMethodSymbol> CollectMethods(INamedTypeSymbol? typeSymbol, params string[] methodNames)
            {
                if (typeSymbol is null)
                {
                    return ImmutableArray<IMethodSymbol>.Empty;
                }

                return typeSymbol
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => methodNames.Contains(m.Name))
                    .Where(ShouldIncludeFormatMethod)
                    .ToImmutableArray();
            }
        }

        private async Task<bool> TryAddRefactoringsAsync(
            CodeRefactoringContext context,
            Document? document,
            TextSpan textSpan,
            SemanticModel? semanticModel,
            ImmutableArray<IMethodSymbol> keepInvocationMethods,
            ISyntaxFactsService? syntaxFactsService,
            bool shouldReplaceInvocation,
            CancellationToken cancellationToken)
        {
            var (keepInvocation, keepSymbol) = await TryFindInvocationAsync(textSpan, document, semanticModel, keepInvocationMethods, syntaxFactsService, context.CancellationToken).ConfigureAwait(false);
            if (keepInvocation != null && keepSymbol != null &&
                IsArgumentListCorrect(syntaxFactsService.GetArgumentsOfInvocationExpression(keepInvocation), keepSymbol, keepInvocationMethods, semanticModel, syntaxFactsService, cancellationToken))
            {
                context.RegisterRefactoring(
                    new ConvertToInterpolatedStringCodeAction(
                        c => CreateInterpolatedStringAsync(keepInvocation, document, syntaxFactsService, shouldReplaceInvocation, c)),
                    keepInvocation.Span);

                return true;
            }

            return false;
        }

        private async Task<(TInvocationExpressionSyntax?, ISymbol?)> TryFindInvocationAsync(
            TextSpan span,
            Document document,
            SemanticModel semanticModel,
            ImmutableArray<IMethodSymbol> applicableMethods,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            // If selection is empty there can be multiple matching invocations (we can be deep in), need to go through all of them
            var possibleInvocations = await document.GetRelevantNodesAsync<TInvocationExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
            var invocation = possibleInvocations.FirstOrDefault(invocation => IsValidPlaceholderToInterpolatedString(invocation, syntaxFactsService, semanticModel, applicableMethods, this, cancellationToken));

            // User selected the whole invocation of format.
            if (invocation != null)
            {
                return (invocation, semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol);
            }

            // User selected a single argument of the invocation (expression / format string) instead of the whole invocation.
            var argument = await document.TryGetRelevantNodeAsync<TArgumentSyntax>(span, cancellationToken).ConfigureAwait(false);
            invocation = argument?.Parent?.Parent as TInvocationExpressionSyntax;
            if (invocation != null && IsValidPlaceholderToInterpolatedString(invocation, syntaxFactsService, semanticModel, applicableMethods, this, cancellationToken))
            {
                return (invocation, semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol);
            }

            // User selected the whole argument list: string format with placeholders plus all expressions
            var argumentList = await document.TryGetRelevantNodeAsync<TArgumentListExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
            invocation = argumentList?.Parent as TInvocationExpressionSyntax;
            if (invocation != null && IsValidPlaceholderToInterpolatedString(invocation, syntaxFactsService, semanticModel, applicableMethods, this, cancellationToken))
            {
                return (invocation, semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol);
            }

            return (null, null);

            static bool IsValidPlaceholderToInterpolatedString(TInvocationExpressionSyntax invocation,
                                                               ISyntaxFactsService syntaxFactsService,
                                                               SemanticModel semanticModel,
                                                               ImmutableArray<IMethodSymbol> applicableMethods,
                                                               AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider<
                                                                   TInvocationExpressionSyntax, TExpressionSyntax,
                                                                   TArgumentSyntax, TLiteralExpressionSyntax,
                                                                   TArgumentListExpressionSyntax> thisInstance,
                                                               CancellationToken cancellationToken)
            {
                var arguments = syntaxFactsService.GetArgumentsOfInvocationExpression(invocation);
                if (arguments.Count >= 2)
                {
                    if (syntaxFactsService.GetExpressionOfArgument(GetFormatArgument(arguments, syntaxFactsService)) is TLiteralExpressionSyntax firstArgumentExpression &&
                        syntaxFactsService.IsStringLiteral(firstArgumentExpression.GetFirstToken()))
                    {
                        var invocationSymbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
                        if (applicableMethods.Contains(invocationSymbol))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private static bool IsArgumentListCorrect(
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            ISymbol invocationSymbol,
            ImmutableArray<IMethodSymbol> formatMethods,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            if (arguments.Count >= 2 &&
                syntaxFactsService.GetExpressionOfArgument(GetFormatArgument(arguments, syntaxFactsService)) is TLiteralExpressionSyntax firstExpression &&
                syntaxFactsService.IsStringLiteral(firstExpression.GetFirstToken()))
            {
                // We do not want to substitute the expression if it is being passed to params array argument
                // Example: 
                // string[] args;
                // String.Format("{0}{1}{2}", args);
                return IsArgumentListNotPassingArrayToParams(
                    syntaxFactsService.GetExpressionOfArgument(GetParamsArgument(arguments, syntaxFactsService)),
                    invocationSymbol,
                    formatMethods,
                    semanticModel,
                    cancellationToken);
            }

            return false;
        }

        private async Task<Document> CreateInterpolatedStringAsync(
            TInvocationExpressionSyntax invocation,
            Document document,
            ISyntaxFactsService syntaxFactsService,
            bool shouldReplaceInvocation,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var arguments = syntaxFactsService.GetArgumentsOfInvocationExpression(invocation);
            var literalExpression = (TLiteralExpressionSyntax?)syntaxFactsService.GetExpressionOfArgument(GetFormatArgument(arguments, syntaxFactsService));
            Contract.ThrowIfNull(literalExpression);
            var text = literalExpression.GetFirstToken().ToString();
            var syntaxGenerator = document.GetRequiredLanguageService<SyntaxGenerator>();
            var expandedArguments = GetExpandedArguments(semanticModel, arguments, syntaxGenerator, syntaxFactsService);
            var interpolatedString = GetInterpolatedString(text);
            var newInterpolatedString = VisitArguments(expandedArguments, interpolatedString, syntaxFactsService);

            SyntaxNode? replacementNode;
            if (shouldReplaceInvocation)
            {
                replacementNode = newInterpolatedString;
            }
            else
            {
                replacementNode = syntaxGenerator.InvocationExpression(
                    syntaxFactsService.GetExpressionOfInvocationExpression(invocation),
                    newInterpolatedString);
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(invocation, replacementNode.WithTriviaFrom(invocation));
            return document.WithSyntaxRoot(newRoot);
        }

        private static string GetArgumentName(TArgumentSyntax argument, ISyntaxFacts syntaxFacts)
            => syntaxFacts.GetNameForArgument(argument);

        private static SyntaxNode GetParamsArgument(SeparatedSyntaxList<TArgumentSyntax> arguments, ISyntaxFactsService syntaxFactsService)
            => arguments.FirstOrDefault(argument => string.Equals(GetArgumentName(argument, syntaxFactsService), StringFormatArguments.FormatArgumentName, StringComparison.OrdinalIgnoreCase)) ?? arguments[1];

        private static TArgumentSyntax GetFormatArgument(SeparatedSyntaxList<TArgumentSyntax> arguments, ISyntaxFactsService syntaxFactsService)
            => arguments.FirstOrDefault(argument => string.Equals(GetArgumentName(argument, syntaxFactsService), StringFormatArguments.FormatArgumentName, StringComparison.OrdinalIgnoreCase)) ?? arguments[0];

        private static TArgumentSyntax GetArgument(SeparatedSyntaxList<TArgumentSyntax> arguments, int index, ISyntaxFacts syntaxFacts)
        {
            if (arguments.Count > 4)
            {
                return arguments[index];
            }

            return arguments.FirstOrDefault(
                argument => string.Equals(GetArgumentName(argument, syntaxFacts), StringFormatArguments.ParamsArgumentNames[index], StringComparison.OrdinalIgnoreCase))
                ?? arguments[index];
        }

        private static ImmutableArray<TExpressionSyntax> GetExpandedArguments(
            SemanticModel semanticModel,
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            SyntaxGenerator syntaxGenerator,
            ISyntaxFacts syntaxFacts)
        {
            using var _ = ArrayBuilder<TExpressionSyntax>.GetInstance(out var builder);
            for (var i = 1; i < arguments.Count; i++)
            {
                var argumentExpression = syntaxFacts.GetExpressionOfArgument(GetArgument(arguments, i, syntaxFacts));
                var convertedType = argumentExpression == null ? null : semanticModel.GetTypeInfo(argumentExpression).ConvertedType;
                if (convertedType == null)
                {
                    builder.Add((TExpressionSyntax)syntaxGenerator.AddParentheses(argumentExpression));
                }
                else
                {
                    var castExpression = (TExpressionSyntax)syntaxGenerator.CastExpression(convertedType, syntaxGenerator.AddParentheses(argumentExpression)).WithAdditionalAnnotations(Simplifier.Annotation);
                    builder.Add(castExpression);
                }
            }

            return builder.ToImmutable();
        }

        private static SyntaxNode VisitArguments(
            ImmutableArray<TExpressionSyntax> expandedArguments,
            SyntaxNode interpolatedString,
            ISyntaxFactsService syntaxFactsService)
        {
            return interpolatedString.ReplaceNodes(syntaxFactsService.GetContentsOfInterpolatedString(interpolatedString), (oldNode, newNode) =>
            {
                var interpolationSyntaxNode = newNode;
                if (interpolationSyntaxNode != null)
                {
                    if (syntaxFactsService.GetExpressionOfInterpolation(interpolationSyntaxNode) is TLiteralExpressionSyntax literalExpression && syntaxFactsService.IsNumericLiteralExpression(literalExpression))
                    {
                        if (int.TryParse(literalExpression.GetFirstToken().ValueText, out var index))
                        {
                            if (index >= 0 && index < expandedArguments.Length)
                            {
                                return interpolationSyntaxNode.ReplaceNode(
                                    literalExpression,
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
            if (firstParameter?.Name != StringFormatArguments.FormatArgumentName)
            {
                return false;
            }

            return true;
        }

        private static bool IsArgumentListNotPassingArrayToParams(
            SyntaxNode? expression,
            ISymbol invocationSymbol,
            ImmutableArray<IMethodSymbol> formatMethods,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (expression != null)
            {
                var formatMethodsAcceptingParamsArray = formatMethods
                        .Where(x => x.Parameters.Length > 1 && x.Parameters[1].Type.Kind == SymbolKind.ArrayType);
                if (formatMethodsAcceptingParamsArray.Contains(invocationSymbol))
                {
                    return semanticModel.GetTypeInfo(expression, cancellationToken).Type?.Kind != SymbolKind.ArrayType;
                }
            }

            return true;
        }

        private class ConvertToInterpolatedStringCodeAction : CodeAction.DocumentChangeAction
        {
            public ConvertToInterpolatedStringCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Convert_to_interpolated_string, createChangedDocument, nameof(FeaturesResources.Convert_to_interpolated_string))
            {
            }
        }

        private static class StringFormatArguments
        {
            public const string FormatArgumentName = "format";

            public const string ArgsArgumentName = "args";

            public static readonly ImmutableArray<string> ParamsArgumentNames =
                ImmutableArray.Create("", "arg0", "arg1", "arg2");
        }
    }
}
