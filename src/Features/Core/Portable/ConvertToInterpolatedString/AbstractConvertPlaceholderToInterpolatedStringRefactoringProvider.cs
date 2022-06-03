// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
    internal abstract class AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider<
        TInvocationExpressionSyntax,
        TExpressionSyntax,
        TArgumentSyntax,
        TLiteralExpressionSyntax,
        TArgumentListExpressionSyntax,
        TInterpolationSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TArgumentSyntax : SyntaxNode
        where TLiteralExpressionSyntax : SyntaxNode
        where TArgumentListExpressionSyntax : SyntaxNode
        where TInterpolationSyntax : SyntaxNode
    {

        // Methods that are not string.Format but still should qualify to be replaced.
        // Ex: Console.WriteLine("{0}", a) => Console.WriteLine($"{a}");
        private static readonly ImmutableArray<(string typeName, ImmutableArray<string> methods)> s_compositeFormattedMethods = ImmutableArray.Create(
            (typeof(Console).FullName!, ImmutableArray.Create(nameof(Console.Write), nameof(Console.WriteLine))),
            (typeof(Debug).FullName!, ImmutableArray.Create(nameof(Debug.WriteLine), nameof(Debug.Print))),
            (typeof(Trace).FullName!, ImmutableArray.Create(nameof(Trace.TraceError), nameof(Trace.TraceWarning), nameof(Trace.TraceInformation))),
            (typeof(TraceSource).FullName!, ImmutableArray.Create(nameof(TraceSource.TraceInformation))));

        protected abstract SyntaxNode GetInterpolatedString(string text);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
            if (stringType.IsErrorType())
            {
                return;
            }

            var stringInvocationMethods = CollectMethods(stringType, ImmutableArray.Create(nameof(string.Format)));
            var compositeFormattedInvocationMethods = s_compositeFormattedMethods
                .SelectMany(pair => CollectMethods(semanticModel.Compilation.GetTypeByMetadataName(pair.typeName), pair.methods))
                .ToImmutableArray();

            var allInvocationMethods = stringInvocationMethods.AddRange(compositeFormattedInvocationMethods);

            if (allInvocationMethods.Length == 0)
            {
                return;
            }

            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFactsService == null)
            {
                return;
            }

            var (invocationSyntax, invocationSymbol) = await TryFindInvocationAsync(textSpan, document, semanticModel, allInvocationMethods, syntaxFactsService, context.CancellationToken).ConfigureAwait(false);
            if (invocationSyntax is null || invocationSymbol is null)
            {
                return;
            }

            if (!IsArgumentListCorrect(syntaxFactsService.GetArgumentsOfInvocationExpression(invocationSyntax), invocationSymbol, allInvocationMethods, semanticModel, syntaxFactsService, cancellationToken))
            {
                return;
            }

            var shouldReplaceInvocation = stringInvocationMethods.Contains(invocationSymbol);

            context.RegisterRefactoring(
                    CodeAction.Create(
                        FeaturesResources.Convert_to_interpolated_string,
                        c => CreateInterpolatedStringAsync(invocationSyntax, document, syntaxFactsService, shouldReplaceInvocation, c),
                        nameof(FeaturesResources.Convert_to_interpolated_string)),
                    invocationSyntax.Span);

            // Local Functions

            static ImmutableArray<IMethodSymbol> CollectMethods(INamedTypeSymbol? typeSymbol, ImmutableArray<string> methodNames)
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

            static bool IsValidPlaceholderToInterpolatedString(
                TInvocationExpressionSyntax invocation,
                ISyntaxFactsService syntaxFactsService,
                SemanticModel semanticModel,
                ImmutableArray<IMethodSymbol> applicableMethods,
                AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider<
                    TInvocationExpressionSyntax, TExpressionSyntax, TArgumentSyntax,
                    TLiteralExpressionSyntax, TArgumentListExpressionSyntax, TInterpolationSyntax> thisInstance,
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
                var convertedType = semanticModel.GetTypeInfo(argumentExpression).ConvertedType;
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

        private static class StringFormatArguments
        {
            public const string FormatArgumentName = "format";

            public const string ArgsArgumentName = "args";

            public static readonly ImmutableArray<string> ParamsArgumentNames =
                ImmutableArray.Create("", "arg0", "arg1", "arg2");
        }
    }
}
