// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertToInterpolatedString
{
    internal abstract class AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider<
        TExpressionSyntax,
        TLiteralExpressionSyntax,
        TInvocationExpressionSyntax,
        TArgumentSyntax,
        TArgumentListExpressionSyntax,
        TInterpolationSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TLiteralExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TArgumentSyntax : SyntaxNode
        where TArgumentListExpressionSyntax : SyntaxNode
        where TInterpolationSyntax : SyntaxNode
    {
        protected abstract SyntaxNode GetInterpolatedString(string text);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
            if (stringType.IsErrorType())
                return;

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var invocationSyntax = await TryFindInvocationAsync().ConfigureAwait(false);
            if (invocationSyntax is null)
                return;

            var invocationSymbol = semanticModel.GetSymbolInfo(invocationSyntax, cancellationToken).GetAnySymbol();
            if (invocationSymbol is null)
                return;

            // If the user is actually passing an array to a params argument, we can't change this to be an interpolated string.
            if (invocationSymbol.IsParams())
            {
                var lastArgument = syntaxFacts.GetArgumentsOfInvocationExpression(invocationSyntax).Last();
                var lastArgumentType = semanticModel.GetTypeInfo(syntaxFacts.GetExpressionOfArgument(lastArgument), cancellationToken).Type;
                if (lastArgument is IArrayTypeSymbol)
                    return;
            }

            //if (!await IsArgumentListCorrectAsync(invocationSyntax, invocationSymbol).ConfigureAwait(false))
            //    return;

            var shouldReplaceInvocation = invocationSymbol is { ContainingType.SpecialType: SpecialType.System_String, Name: nameof(string.Format) };

            context.RegisterRefactoring(
                CodeAction.Create(
                    FeaturesResources.Convert_to_interpolated_string,
                    cancellationToken => CreateInterpolatedStringAsync(document, invocationSyntax, shouldReplaceInvocation, cancellationToken),
                    nameof(FeaturesResources.Convert_to_interpolated_string)),
                invocationSyntax.Span);

            return;

            async Task<TInvocationExpressionSyntax?> TryFindInvocationAsync()
            {
                // If selection is empty there can be multiple matching invocations (we can be deep in), need to go through all of them
                var possibleInvocations = await document.GetRelevantNodesAsync<TInvocationExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
                var invocation = possibleInvocations.FirstOrDefault(IsValidPlaceholderToInterpolatedString);

                // User selected the whole invocation of format.
                if (invocation != null)
                    return invocation;

                // User selected a single argument of the invocation (expression / format string) instead of the whole invocation.
                var argument = await document.TryGetRelevantNodeAsync<TArgumentSyntax>(span, cancellationToken).ConfigureAwait(false);
                invocation = argument?.Parent?.Parent as TInvocationExpressionSyntax;
                if (IsValidPlaceholderToInterpolatedString(invocation))
                    return invocation;

                // User selected the whole argument list: string format with placeholders plus all expressions
                var argumentList = await document.TryGetRelevantNodeAsync<TArgumentListExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
                invocation = argumentList?.Parent as TInvocationExpressionSyntax;
                if (IsValidPlaceholderToInterpolatedString(invocation))
                    return invocation;

                return null;
            }

            bool IsValidPlaceholderToInterpolatedString([NotNullWhen(true)] TInvocationExpressionSyntax? invocation)
            {
                if (invocation != null)
                {
                    // look for a string argument containing `"...{0}..."`, followed by more arguments.
                    var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
                    for (int i = 0, n = arguments.Count - 2; i < n; i++)
                    {
                        var argument = arguments[i];
                        var expression = syntaxFacts.GetExpressionOfArgument(argument);
                        if (syntaxFacts.IsStringLiteralExpression(expression))
                        {
                            var remainingArgCount = arguments.Count - i - 1;
                            Debug.Assert(remainingArgCount > 0);
                            if (IsValidPlaceholderArgument(expression.GetFirstToken(), remainingArgCount))
                                return true;
                        }
                    }
                }

                return false;
            }

            bool IsValidPlaceholderArgument(SyntaxToken stringToken, int remainingArgCount)
            {
                // See how many arguments follow the `"...{0}..."`.  We have to have a {0}, {1}, ... {N} part in the
                // string for each of them.  Note, those could be in any order.
                for (var i = 0; i < remainingArgCount; i++)
                {
                    if (!stringToken.Text.Contains($"{{{i}}}"))
                        return false;
                }

                return true;
            }

            //Task<bool> IsArgumentListCorrectAsync(
            //    TInvocationExpressionSyntax invocation,
            //    ISymbol invocationSymbol)
            //{
            //    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            //    var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);

            //    if (arguments.Count >= 2 &&
            //        syntaxFacts.GetExpressionOfArgument(GetFormatArgument(arguments, syntaxFacts)) is TLiteralExpressionSyntax firstExpression &&
            //        syntaxFacts.IsStringLiteral(firstExpression.GetFirstToken()))
            //    {
            //        // We do not want to substitute the expression if it is being passed to params array argument
            //        // Example: 
            //        // string[] args;
            //        // String.Format("{0}{1}{2}", args);
            //        return await IsArgumentListNotPassingArrayToParams(
            //            document,
            //            syntaxFacts.GetExpressionOfArgument(GetParamsArgument(arguments, syntaxFacts)),
            //            invocationSymbol,
            //            cancellationToken);
            //    }

            //    return SpecializedTasks.False;
            //}
        }

        private async Task<Document> CreateInterpolatedStringAsync(
            Document document,
            TInvocationExpressionSyntax invocation,
            bool shouldReplaceInvocation,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
            var literalExpression = (TLiteralExpressionSyntax?)syntaxFacts.GetExpressionOfArgument(GetFormatArgument(arguments, syntaxFactsService));
            Contract.ThrowIfNull(literalExpression);
            var text = literalExpression.GetFirstToken().ToString();
            var syntaxGenerator = document.GetRequiredLanguageService<SyntaxGenerator>();
            var expandedArguments = GetExpandedArguments(semanticModel, arguments, syntaxGenerator, syntaxFactsService);
            var interpolatedString = GetInterpolatedString(text);
            var newInterpolatedString = VisitArguments(expandedArguments, interpolatedString, syntaxFactsService);

            var replacementNode = shouldReplaceInvocation
                ? newInterpolatedString
                : syntaxGenerator.InvocationExpression(
                    syntaxFactsService.GetExpressionOfInvocationExpression(invocation),
                    newInterpolatedString);

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
                var convertedType = semanticModel.GetTypeInfo(argumentExpression).ConvertedType;
                if (convertedType is null)
                {
                    builder.Add((TExpressionSyntax)syntaxGenerator.AddParentheses(argumentExpression));
                }
                else
                {
                    builder.Add((TExpressionSyntax)syntaxGenerator.CastExpression(convertedType, syntaxGenerator.AddParentheses(argumentExpression)));
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
                if (newNode is TInterpolationSyntax interpolation)
                {
                    if (syntaxFactsService.GetExpressionOfInterpolation(interpolation) is TLiteralExpressionSyntax literalExpression && syntaxFactsService.IsNumericLiteralExpression(literalExpression))
                    {
                        if (int.TryParse(literalExpression.GetFirstToken().ValueText, out var index))
                        {
                            if (index >= 0 && index < expandedArguments.Length)
                            {
                                return interpolation.ReplaceNode(
                                    literalExpression,
                                    syntaxFactsService.ConvertToSingleLine(expandedArguments[index], useElasticTrivia: true).WithAdditionalAnnotations(Formatter.Annotation));
                            }
                        }
                    }
                }

                return newNode;
            });
        }

        //private static bool ShouldIncludeFormatMethod(IMethodSymbol methodSymbol)
        //{
        //    if (!methodSymbol.IsStatic)
        //    {
        //        return false;
        //    }

        //    if (methodSymbol.Parameters.Length == 0)
        //    {
        //        return false;
        //    }

        //    var firstParameter = methodSymbol.Parameters[0];
        //    if (firstParameter?.Name != StringFormatArguments.FormatArgumentName)
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        //private static async Task<bool> IsArgumentListNotPassingArrayToParams(

        //    SyntaxNode? expression,
        //    ISymbol invocationSymbol,
        //    SemanticModel semanticModel,
        //    CancellationToken cancellationToken)
        //{
        //    if (expression != null)
        //    {
        //        var formatMethodsAcceptingParamsArray = formatMethods.Where(
        //            x => x.Parameters is [_, { Type.Kind: SymbolKind.ArrayType }, ..]);
        //        if (formatMethodsAcceptingParamsArray.Contains(invocationSymbol))
        //        {
        //            return semanticModel.GetTypeInfo(expression, cancellationToken).Type?.Kind != SymbolKind.ArrayType;
        //        }
        //    }

        //    return true;
        //}
    }
}
