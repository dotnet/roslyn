// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

                var argument = await context.TryGetSelectedNodeAsync<TSimpleArgumentSyntax>().ConfigureAwait(false);
                if (argument == null && textSpan.IsEmpty)
                {
                    // For arguments we want to enable cursor anywhere in the expressions (even deep within) as long as
                    // it is on the first line of said expression. Since the `TryGetSelectedNodeAsync` doesn't do such
                    // traversing up & checking line numbers -> need to do it manually.
                    // The rationale for only first line is that arg. expressions can be arbitrarily large. 
                    // see: https://github.com/dotnet/roslyn/issues/18848

                    var position = textSpan.Start;
                    var token = root.FindToken(position);

                    argument = root.FindNode(token.Span).FirstAncestorOrSelfUntil<TBaseArgumentSyntax>(node => node is TArgumentListSyntax) as TSimpleArgumentSyntax;
                    if (argument == null)
                    {
                        return;
                    }

                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var argumentStartLine = sourceText.Lines.GetLineFromPosition(argument.Span.Start).LineNumber;
                    var caretLine = sourceText.Lines.GetLineFromPosition(position).LineNumber;

                    if (argumentStartLine != caretLine)
                    {
                        return;
                    }
                }

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

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

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

                var argumentList = argument.Parent as TArgumentListSyntax;
                if (argumentList == null)
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

                for (var i = argumentIndex; i < argumentCount; i++)
                {
                    if (!(arguments[i] is TSimpleArgumentSyntax))
                    {
                        return;
                    }
                }

                var argumentName = parameters[argumentIndex].Name;

                if (SupportsNonTrailingNamedArguments(root.SyntaxTree.Options) &&
                    argumentIndex < argumentCount - 1)
                {
                    context.RegisterRefactoring(
                        new MyCodeAction(
                            string.Format(FeaturesResources.Add_argument_name_0, argumentName),
                            c => AddNamedArgumentsAsync(root, document, argument, parameters, argumentIndex, includingTrailingArguments: false)));

                    context.RegisterRefactoring(
                        new MyCodeAction(
                            string.Format(FeaturesResources.Add_argument_name_0_including_trailing_arguments, argumentName),
                            c => AddNamedArgumentsAsync(root, document, argument, parameters, argumentIndex, includingTrailingArguments: true)));
                }
                else
                {
                    context.RegisterRefactoring(
                        new MyCodeAction(
                            string.Format(FeaturesResources.Add_argument_name_0, argumentName),
                            c => AddNamedArgumentsAsync(root, document, argument, parameters, argumentIndex, includingTrailingArguments: true)));
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
                var argumentList = (TArgumentListSyntax)firstArgument.Parent;
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
                        ? WithName((TSimpleArgumentSyntax)argument, parameters[i].Name).WithTriviaFrom(argument)
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

            protected abstract bool IsCloseParenOrComma(SyntaxToken token);
            protected abstract bool IsLegalToAddNamedArguments(ImmutableArray<IParameterSymbol> parameters, int argumentCount);
            protected abstract TSimpleArgumentSyntax WithName(TSimpleArgumentSyntax argument, string name);
            protected abstract bool IsPositionalArgument(TSimpleArgumentSyntax argument);
            protected abstract SeparatedSyntaxList<TBaseArgumentSyntax> GetArguments(TArgumentListSyntax argumentList);
            protected abstract SyntaxNode GetReceiver(SyntaxNode argument);
            protected abstract bool SupportsNonTrailingNamedArguments(ParseOptions options);
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
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            await _argumentAnalyzer.ComputeRefactoringsAsync(context, root).ConfigureAwait(false);

            if (_attributeArgumentAnalyzer != null)
            {
                await _attributeArgumentAnalyzer.ComputeRefactoringsAsync(context, root).ConfigureAwait(false);
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
