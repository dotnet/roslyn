// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseNamedArguments
{
    internal abstract class AbstractUseNamedArgumentsCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected interface IAnalyzer
        {
            Task ComputeRefactoringsAsync(CodeRefactoringContext context, SyntaxNode root);
        }

        protected abstract class Analyzer<TBaseArgumentSyntax, TSimpleArgumentSyntax, TArgumentListSyntax> : IAnalyzer
            where TBaseArgumentSyntax : SyntaxNode
            where TSimpleArgumentSyntax : TBaseArgumentSyntax
            where TArgumentListSyntax : SyntaxNode
        {
            public async Task ComputeRefactoringsAsync(
                CodeRefactoringContext context, SyntaxNode root)
            {
                var (document, textSpan, cancellationToken) = context;

                // We allow empty nodes here to find VB implicit arguments.
                var potentialArguments = await document.GetRelevantNodesAsync<TBaseArgumentSyntax>(textSpan, allowEmptyNodes: true, cancellationToken).ConfigureAwait(false);
                var argument = potentialArguments.FirstOrDefault(n => n.Parent is TArgumentListSyntax) as TSimpleArgumentSyntax;
                if (argument == null)
                {
                    return;
                }

                if (!IsPositionalArgument(argument))
                {
                    return;
                }

                var receiver = GetReceiver(argument);
                if (receiver == null)
                {
                    return;
                }

                if (receiver.ContainsDiagnostics)
                {
                    return;
                }

                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var symbol = semanticModel.GetSymbolInfo(receiver, cancellationToken).Symbol;
                if (symbol == null)
                {
                    return;
                }

                var parameters = symbol.GetParameters();
                if (parameters.IsDefaultOrEmpty)
                {
                    return;
                }

                if (argument.Parent is not TArgumentListSyntax argumentList)
                {
                    return;
                }

                var arguments = GetArguments(argumentList);
                var argumentCount = arguments.Count;
                var argumentIndex = arguments.IndexOf(argument);
                if (argumentIndex >= parameters.Length)
                {
                    return;
                }

                if (!IsLegalToAddNamedArguments(parameters, argumentCount))
                {
                    return;
                }

                if (IsImplicitIndexOrRangeIndexer(parameters, argument, semanticModel))
                {
                    return;
                }

                var potentialArgumentsToName = 0;
                for (var i = argumentIndex; i < argumentCount; i++)
                {
                    if (arguments[i] is not TSimpleArgumentSyntax simpleArgumet)
                    {
                        return;
                    }
                    else if (IsPositionalArgument(simpleArgumet))
                    {
                        potentialArgumentsToName++;
                    }
                }

                var argumentName = parameters[argumentIndex].Name;

                if (SupportsNonTrailingNamedArguments(root.SyntaxTree.Options) &&
                    potentialArgumentsToName > 1)
                {
                    context.RegisterRefactoring(
                        CodeAction.Create(
                            string.Format(FeaturesResources.Add_argument_name_0, argumentName),
                            c => AddNamedArgumentsAsync(root, document, argument, parameters, argumentIndex, includingTrailingArguments: false),
                            nameof(FeaturesResources.Add_argument_name_0) + "_" + argumentName),
                        argument.Span);

                    context.RegisterRefactoring(
                        CodeAction.Create(
                            string.Format(FeaturesResources.Add_argument_name_0_including_trailing_arguments, argumentName),
                            c => AddNamedArgumentsAsync(root, document, argument, parameters, argumentIndex, includingTrailingArguments: true),
                            nameof(FeaturesResources.Add_argument_name_0_including_trailing_arguments) + "_" + argumentName),
                        argument.Span);
                }
                else
                {
                    context.RegisterRefactoring(
                        CodeAction.Create(
                            string.Format(FeaturesResources.Add_argument_name_0, argumentName),
                            c => AddNamedArgumentsAsync(root, document, argument, parameters, argumentIndex, includingTrailingArguments: true),
                            nameof(FeaturesResources.Add_argument_name_0) + "_" + argumentName),
                        argument.Span);
                }
            }

            private Task<Document> AddNamedArgumentsAsync(
                SyntaxNode root,
                Document document,
                TSimpleArgumentSyntax firstArgument,
                ImmutableArray<IParameterSymbol> parameters,
                int index,
                bool includingTrailingArguments)
            {
                var argumentList = (TArgumentListSyntax)firstArgument.Parent!;
                var newArgumentList = GetOrSynthesizeNamedArguments(parameters, argumentList, index, includingTrailingArguments);
                var newRoot = root.ReplaceNode(argumentList, newArgumentList);
                return Task.FromResult(document.WithSyntaxRoot(newRoot));
            }

            private TArgumentListSyntax GetOrSynthesizeNamedArguments(
                ImmutableArray<IParameterSymbol> parameters, TArgumentListSyntax argumentList,
                int index, bool includingTrailingArguments)
            {
                var arguments = GetArguments(argumentList);
                var namedArguments = arguments
                    .Select((argument, i) => ShouldAddName(argument, i)
                        ? WithName((TSimpleArgumentSyntax)argument.WithoutTrivia(), parameters[i].Name).WithTriviaFrom(argument)
                        : argument);

                return WithArguments(argumentList, namedArguments, arguments.GetSeparators());

                // local functions

                bool ShouldAddName(TBaseArgumentSyntax argument, int currentIndex)
                {
                    if (currentIndex > index && !includingTrailingArguments)
                    {
                        return false;
                    }

                    return currentIndex >= index && argument is TSimpleArgumentSyntax s && IsPositionalArgument(s);
                }
            }

            protected abstract TArgumentListSyntax WithArguments(
                TArgumentListSyntax argumentList, IEnumerable<TBaseArgumentSyntax> namedArguments, IEnumerable<SyntaxToken> separators);

            protected abstract bool IsLegalToAddNamedArguments(ImmutableArray<IParameterSymbol> parameters, int argumentCount);
            protected abstract TSimpleArgumentSyntax WithName(TSimpleArgumentSyntax argument, string name);
            protected abstract bool IsPositionalArgument(TSimpleArgumentSyntax argument);
            protected abstract SeparatedSyntaxList<TBaseArgumentSyntax> GetArguments(TArgumentListSyntax argumentList);
            protected abstract SyntaxNode? GetReceiver(SyntaxNode argument);
            protected abstract bool SupportsNonTrailingNamedArguments(ParseOptions options);
            protected abstract bool IsImplicitIndexOrRangeIndexer(ImmutableArray<IParameterSymbol> parameters, TBaseArgumentSyntax argument, SemanticModel semanticModel);
        }

        private readonly IAnalyzer _argumentAnalyzer;
        private readonly IAnalyzer _attributeArgumentAnalyzer;

        protected AbstractUseNamedArgumentsCodeRefactoringProvider(
            IAnalyzer argumentAnalyzer,
            IAnalyzer attributeArgumentAnalyzer)
        {
            _argumentAnalyzer = argumentAnalyzer;
            _attributeArgumentAnalyzer = attributeArgumentAnalyzer;
        }

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            if (document.Project.Solution.WorkspaceKind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            await _argumentAnalyzer.ComputeRefactoringsAsync(context, root).ConfigureAwait(false);

            if (_attributeArgumentAnalyzer != null)
            {
                await _attributeArgumentAnalyzer.ComputeRefactoringsAsync(context, root).ConfigureAwait(false);
            }
        }
    }
}
