﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CodeRefactorings;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ConvertToInterpolatedString
{
    internal abstract class AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider<TInvocationExpressionSyntax, TExpressionSyntax, TArgumentSyntax, TLiteralExpressionSyntax> : CodeRefactoringProvider
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
                .GetMembers(nameof(string.Format))
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
            if (TryFindInvocation(context.Span, root, semanticModel, formatMethods, syntaxFactsService, context.CancellationToken, out var invocation, out var invocationSymbol) &&
                IsArgumentListCorrect(syntaxFactsService.GetArgumentsOfInvocationExpression(invocation), invocationSymbol, formatMethods, semanticModel, syntaxFactsService, context.CancellationToken))
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
                var arguments = syntaxFactsService.GetArgumentsOfInvocationExpression(invocation);
                if (arguments.Count >= 2)
                {
                    if (syntaxFactsService.GetExpressionOfArgument(GetFormatArgument(arguments, syntaxFactsService)) is TLiteralExpressionSyntax firstArgumentExpression && syntaxFactsService.IsStringLiteral(firstArgumentExpression.GetFirstToken()))
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
            var firstExpression = syntaxFactsService.GetExpressionOfArgument(GetFormatArgument(arguments, syntaxFactsService)) as TLiteralExpressionSyntax;
            if (arguments.Count >= 2 &&
                firstExpression != null &&
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
            for (int i = 1; i < arguments.Count; i++)
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
            public ConvertToInterpolatedStringCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
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
