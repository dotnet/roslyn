// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertToInterpolatedString
{
    internal abstract class AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider<
        TExpressionSyntax,
        TLiteralExpressionSyntax,
        TInvocationExpressionSyntax,
        TInterpolatedStringExpressionSyntax,
        TArgumentSyntax,
        TArgumentListExpressionSyntax,
        TInterpolationSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TLiteralExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TInterpolatedStringExpressionSyntax : TExpressionSyntax
        where TArgumentSyntax : SyntaxNode
        where TArgumentListExpressionSyntax : SyntaxNode
        where TInterpolationSyntax : SyntaxNode
    {
        protected abstract TExpressionSyntax ParseExpression(string text);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
            if (stringType.IsErrorType())
                return;

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var (invocationSyntax, placeholderArgument) = await TryFindInvocationAsync().ConfigureAwait(false);
            if (invocationSyntax is null || placeholderArgument is null)
                return;

            var placeholderExpression = syntaxFacts.GetExpressionOfArgument(placeholderArgument);
            var stringToken = placeholderExpression.GetFirstToken();

            // don't offer if the string argument has errors in it, or if converting it to an interpolated string creates errors.
            if (stringToken.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                return;

            var interpolatedString = ParseExpression("$" + stringToken.Text) as TInterpolatedStringExpressionSyntax;
            if (interpolatedString is null || interpolatedString.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                return;

            // Not supported if there are any omitted arguments following the placeholder.
            var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocationSyntax);
            var placeholderIndex = arguments.IndexOf(placeholderArgument);
            Contract.ThrowIfTrue(placeholderIndex < 0);
            for (var i = placeholderIndex + 1; i < arguments.Count; i++)
            {
                if (syntaxFacts.GetExpressionOfArgument(arguments[i]) is null)
                    return;
            }

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

            var shouldReplaceInvocation = invocationSymbol is { ContainingType.SpecialType: SpecialType.System_String, Name: nameof(string.Format) };

            context.RegisterRefactoring(
                CodeAction.Create(
                    FeaturesResources.Convert_to_interpolated_string,
                    cancellationToken => CreateInterpolatedStringAsync(document, invocationSyntax, placeholderArgument, interpolatedString, shouldReplaceInvocation, cancellationToken),
                    nameof(FeaturesResources.Convert_to_interpolated_string)),
                invocationSyntax.Span);

            return;

            async Task<(TInvocationExpressionSyntax? invocation, TArgumentSyntax? placeholderArgument)> TryFindInvocationAsync()
            {
                // If selection is empty there can be multiple matching invocations (we can be deep in), need to go through all of them
                var invocations = await document.GetRelevantNodesAsync<TInvocationExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
                foreach (var invocation in invocations)
                {
                    var argument = FindValidPlaceholderArgument(invocation);
                    if (argument != null)
                        return (invocation, argument);
                }

                {
                    // User selected a single argument of the invocation (expression / format string) instead of the whole invocation.
                    var argument = await document.TryGetRelevantNodeAsync<TArgumentSyntax>(span, cancellationToken).ConfigureAwait(false);
                    var invocation = argument?.Parent?.Parent as TInvocationExpressionSyntax;
                    var placeholderArgument = FindValidPlaceholderArgument(invocation);
                    if (placeholderArgument != null)
                        return (invocation, argument);
                }

                {
                    // User selected the whole argument list: string format with placeholders plus all expressions
                    var argumentList = await document.TryGetRelevantNodeAsync<TArgumentListExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
                    var invocation = argumentList?.Parent as TInvocationExpressionSyntax;
                    var argument = FindValidPlaceholderArgument(invocation);
                    if (argument != null)
                        return (invocation, argument);
                }

                return default;
            }

            TArgumentSyntax? FindValidPlaceholderArgument(TInvocationExpressionSyntax? invocation)
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
                                return (TArgumentSyntax)argument;
                        }
                    }
                }

                return null;
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
        }

        private static async Task<Document> CreateInterpolatedStringAsync(
            Document document,
            TInvocationExpressionSyntax invocation,
            TArgumentSyntax placeholderArgument,
            TInterpolatedStringExpressionSyntax interpolatedString,
            bool shouldReplaceInvocation,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
            var literalExpression = (TLiteralExpressionSyntax?)syntaxFacts.GetExpressionOfArgument(placeholderArgument);
            Contract.ThrowIfNull(literalExpression);

            var syntaxGenerator = document.GetRequiredLanguageService<SyntaxGenerator>();

            var expandedArguments = GetExpandedArguments();
            var newInterpolatedString = VisitArguments();

            var replacementNode = shouldReplaceInvocation
                ? newInterpolatedString
                : syntaxGenerator.InvocationExpression(
                    syntaxFacts.GetExpressionOfInvocationExpression(invocation),
                    newInterpolatedString);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(invocation, replacementNode.WithTriviaFrom(invocation));
            return document.WithSyntaxRoot(newRoot);

            ImmutableArray<TExpressionSyntax> GetExpandedArguments()
            {
                var placeholderIndex = arguments.IndexOf(placeholderArgument);
                Contract.ThrowIfTrue(placeholderIndex < 0);

                using var _ = ArrayBuilder<TExpressionSyntax>.GetInstance(out var builder);
                for (var i = placeholderIndex + 1; i < arguments.Count; i++)
                {
                    var argumentExpression = syntaxFacts.GetExpressionOfArgument(arguments[i]);
                    var convertedType = semanticModel.GetTypeInfo(argumentExpression, cancellationToken).ConvertedType;

                    builder.Add(convertedType is null
                        ? (TExpressionSyntax)syntaxGenerator.AddParentheses(argumentExpression)
                        : (TExpressionSyntax)syntaxGenerator.CastExpression(convertedType, syntaxGenerator.AddParentheses(argumentExpression)));
                }

                return builder.ToImmutableAndClear();
            }

            TExpressionSyntax VisitArguments()
            {
                return interpolatedString.ReplaceNodes(syntaxFacts.GetContentsOfInterpolatedString(interpolatedString), (oldNode, newNode) =>
                {
                    if (newNode is TInterpolationSyntax interpolation)
                    {
                        if (syntaxFacts.GetExpressionOfInterpolation(interpolation) is TLiteralExpressionSyntax literalExpression &&
                            syntaxFacts.IsNumericLiteralExpression(literalExpression) &&
                            int.TryParse(literalExpression.GetFirstToken().ValueText, out var index) &&
                            index >= 0 && index < expandedArguments.Length)
                        {
                            return interpolation.ReplaceNode(
                                literalExpression,
                                syntaxFacts.ConvertToSingleLine(expandedArguments[index], useElasticTrivia: true).WithAdditionalAnnotations(Formatter.Annotation));
                        }
                    }

                    return newNode;
                });
            }
        }
    }
}
