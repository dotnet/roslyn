// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
            if (stringType == null)
            {
                return;
            }

            var formatMethods = stringType
                .GetMembers(nameof(string.Format))
                .OfType<IMethodSymbol>()
                .Where(ShouldIncludeFormatMethod)
                .ToImmutableArray();

            if (formatMethods.Length == 0)
            {
                return;
            }

            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFactsService == null)
            {
                return;
            }

            var (invocation, invocationSymbol) = await TryFindInvocationAsync(textSpan, document, semanticModel, formatMethods, syntaxFactsService, context.CancellationToken).ConfigureAwait(false);
            if (invocation != null && invocationSymbol != null &&
                IsArgumentListCorrect(syntaxFactsService.GetArgumentsOfInvocationExpression(invocation), invocationSymbol, formatMethods, semanticModel, syntaxFactsService, cancellationToken))
            {
                context.RegisterRefactoring(
                    new ConvertToInterpolatedStringCodeAction(
                        FeaturesResources.Convert_to_interpolated_string,
                        c => CreateInterpolatedString(invocation, document, syntaxFactsService, c)),
                    invocation.Span);
            }
        }

        private async Task<(TInvocationExpressionSyntax, ISymbol)> TryFindInvocationAsync(
            TextSpan span,
            Document document,
            SemanticModel semanticModel,
            ImmutableArray<IMethodSymbol> formatMethods,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            var possibleInvocations = await document.GetRelevantNodesAsync<TInvocationExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
            var invocation = possibleInvocations.FirstOrDefault(invocation => IsValidPlaceholderToInterpolatedString(invocation, syntaxFactsService, semanticModel, formatMethods, this, cancellationToken));

            if (invocation != null)
            {
                return (invocation, semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol);
            }

            // User might have selected single argument of the expression instead of the whole invocation.
            var argument = await document.TryGetRelevantNodeAsync<TArgumentSyntax>(span, cancellationToken).ConfigureAwait(false);
            invocation = argument?.Parent?.Parent as TInvocationExpressionSyntax;
            if (invocation != null && IsValidPlaceholderToInterpolatedString(invocation, syntaxFactsService, semanticModel, formatMethods, this, cancellationToken))
            {
                return (invocation, semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol);
            }


            // user might have selected the whole argument list: string format with placeholders plus all expressions
            var argumentList = await document.TryGetRelevantNodeAsync<TArgumentListExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
            invocation = argumentList?.Parent as TInvocationExpressionSyntax;
            if (invocation != null && IsValidPlaceholderToInterpolatedString(invocation, syntaxFactsService, semanticModel, formatMethods, this, cancellationToken))
            {
                return (invocation, semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol);
            }

            return (null, null);

            static bool IsValidPlaceholderToInterpolatedString(TInvocationExpressionSyntax invocation,
                                                               ISyntaxFactsService syntaxFactsService,
                                                               SemanticModel semanticModel,
                                                               ImmutableArray<IMethodSymbol> formatMethods,
                                                               AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider<
                                                                   TInvocationExpressionSyntax, TExpressionSyntax,
                                                                   TArgumentSyntax, TLiteralExpressionSyntax,
                                                                   TArgumentListExpressionSyntax> thisInstance,
                                                               CancellationToken cancellationToken)
            {
                var arguments = syntaxFactsService.GetArgumentsOfInvocationExpression(invocation);
                if (arguments.Count >= 2)
                {
                    if (syntaxFactsService.GetExpressionOfArgument(thisInstance.GetFormatArgument(arguments, syntaxFactsService)) is TLiteralExpressionSyntax firstArgumentExpression &&
                        syntaxFactsService.IsStringLiteral(firstArgumentExpression.GetFirstToken()))
                    {
                        var invocationSymbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
                        if (formatMethods.Contains(invocationSymbol))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
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

        private async Task<Document> CreateInterpolatedString(
            TInvocationExpressionSyntax invocation,
            Document document,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var arguments = syntaxFactsService.GetArgumentsOfInvocationExpression(invocation);
            var literalExpression = syntaxFactsService.GetExpressionOfArgument(GetFormatArgument(arguments, syntaxFactsService)) as TLiteralExpressionSyntax;
            var text = literalExpression.GetFirstToken().ToString();
            var syntaxGenerator = document.GetLanguageService<SyntaxGenerator>();
            var expandedArguments = GetExpandedArguments(semanticModel, arguments, syntaxGenerator, syntaxFactsService);
            var interpolatedString = GetInterpolatedString(text);
            var newInterpolatedString = VisitArguments(expandedArguments, interpolatedString, syntaxFactsService);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(invocation, newInterpolatedString.WithTriviaFrom(invocation));
            return document.WithSyntaxRoot(newRoot);
        }

        private string GetArgumentName(TArgumentSyntax argument, ISyntaxFactsService syntaxFactsService)
            => syntaxFactsService.GetNameForArgument(argument);

        private SyntaxNode GetParamsArgument(SeparatedSyntaxList<TArgumentSyntax> arguments, ISyntaxFactsService syntaxFactsService)
        => arguments.FirstOrDefault(argument => string.Equals(GetArgumentName(argument, syntaxFactsService), StringFormatArguments.FormatArgumentName, StringComparison.OrdinalIgnoreCase)) ?? arguments[1];

        private TArgumentSyntax GetFormatArgument(SeparatedSyntaxList<TArgumentSyntax> arguments, ISyntaxFactsService syntaxFactsService)
            => arguments.FirstOrDefault(argument => string.Equals(GetArgumentName(argument, syntaxFactsService), StringFormatArguments.FormatArgumentName, StringComparison.OrdinalIgnoreCase)) ?? arguments[0];

        private TArgumentSyntax GetArgument(SeparatedSyntaxList<TArgumentSyntax> arguments, int index, ISyntaxFactsService syntaxFactsService)
        {
            if (arguments.Count > 4)
            {
                return arguments[index];
            }

            return arguments.FirstOrDefault(
                argument => string.Equals(GetArgumentName(argument, syntaxFactsService), StringFormatArguments.ParamsArgumentNames[index], StringComparison.OrdinalIgnoreCase))
                ?? arguments[index];
        }

        private ImmutableArray<TExpressionSyntax> GetExpandedArguments(
            SemanticModel semanticModel,
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            SyntaxGenerator syntaxGenerator,
            ISyntaxFactsService syntaxFactsService)
        {
            var builder = ArrayBuilder<TExpressionSyntax>.GetInstance();
            for (var i = 1; i < arguments.Count; i++)
            {
                var argumentExpression = syntaxFactsService.GetExpressionOfArgument(GetArgument(arguments, i, syntaxFactsService));
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
                    if (syntaxFactsService.GetExpressionOfInterpolation(interpolationSyntaxNode) is TLiteralExpressionSyntax literalExpression && syntaxFactsService.IsNumericLiteralExpression(literalExpression))
                    {
                        if (int.TryParse(literalExpression.GetFirstToken().ValueText, out var index))
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
            if (firstParameter?.Name != StringFormatArguments.FormatArgumentName)
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
            public ConvertToInterpolatedStringCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
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
