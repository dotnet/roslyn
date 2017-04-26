// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.UseNamedArguments
{
    internal abstract class AbstractUseNamedArgumentsCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected interface IAnalyzer
        {
            Task ComputeRefactoringsAsync(
                CodeRefactoringContext context, SyntaxNode root, CancellationToken cancellationToken);
        }

        protected abstract class Analyzer<TBaseArgumentSyntax, TArgumentSyntax, TArgumentListSyntax> : IAnalyzer
            where TBaseArgumentSyntax : SyntaxNode
            where TArgumentSyntax : TBaseArgumentSyntax
            where TArgumentListSyntax : SyntaxNode
        {
            public async Task ComputeRefactoringsAsync(
                CodeRefactoringContext context, SyntaxNode root, CancellationToken cancellationToken)
            {
                var document = context.Document;

                if (context.Span.Length > 0)
                {
                    return;
                }

                var argument = root.FindNode(context.Span).FirstAncestorOrSelf<TBaseArgumentSyntax>() as TArgumentSyntax;
                if (argument == null)
                {
                    return;
                }

                if (!IsPositionalArgument(argument))
                {
                    return;
                }

                // Arguments can be arbitrarily large.  Only offer this feature if the caret is on hte
                // line that the argument starts on.

                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                var argumentStartLine = sourceText.Lines.GetLineFromPosition(argument.Span.Start).LineNumber;
                var caretLine = sourceText.Lines.GetLineFromPosition(context.Span.Start).LineNumber;

                if (argumentStartLine != caretLine)
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

                var argumentList = (TArgumentListSyntax)argument.Parent;
                var (index, count) = GetArgumentListIndexAndCount(argument, argumentList);

                var arguments = GetArguments(argumentList);
                for (var i = index; i < count; i++)
                {
                    if (!(arguments[i] is TArgumentSyntax))
                    {
                        return;
                    }
                }

                if (!IsLegalToAddNamedArguments(parameters, count))
                {
                    return;
                }

                var argumentName = parameters[index].Name;
                context.RegisterRefactoring(
                    new MyCodeAction(
                        string.Format(FeaturesResources.Add_argument_name_0, argumentName),
                        c => AddNamedArgumentsAsync(root, document, argument, parameters, index)));
            }

            private Task<Document> AddNamedArgumentsAsync(
                SyntaxNode root,
                Document document,
                TArgumentSyntax firstArgument,
                ImmutableArray<IParameterSymbol> parameters,
                int index)
            {
                var argumentList = (TArgumentListSyntax)firstArgument.Parent;
                var newArgumentList = GetOrSynthesizeNamedArguments(parameters, argumentList, index);
                var newRoot = root.ReplaceNode(argumentList, newArgumentList);
                return Task.FromResult(document.WithSyntaxRoot(newRoot));
            }

            private (int, int) GetArgumentListIndexAndCount(TArgumentSyntax argument, TArgumentListSyntax argumentList)
            {
                var arguments = GetArguments(argumentList);
                return (arguments.IndexOf(argument), arguments.Count);
            }

            private TArgumentListSyntax GetOrSynthesizeNamedArguments(
                ImmutableArray<IParameterSymbol> parameters, TArgumentListSyntax argumentList, int index)
            {
                var arguments = GetArguments(argumentList);
                var namedArguments = arguments
                    .Select((argument, i) => i >= index && argument is TArgumentSyntax s && IsPositionalArgument(s)
                        ? WithName(s, parameters[i].Name).WithTriviaFrom(argument)
                        : argument);

                return WithArguments(argumentList, namedArguments, arguments.GetSeparators());
            }

            protected abstract TArgumentListSyntax WithArguments(
                TArgumentListSyntax argumentList, IEnumerable<TBaseArgumentSyntax> namedArguments, IEnumerable<SyntaxToken> separators);

            protected abstract bool IsLegalToAddNamedArguments(ImmutableArray<IParameterSymbol> parameters, int argumentCount);
            protected abstract TArgumentSyntax WithName(TArgumentSyntax argument, string name);
            protected abstract bool IsPositionalArgument(TArgumentSyntax argument);
            protected abstract SeparatedSyntaxList<TBaseArgumentSyntax> GetArguments(TArgumentListSyntax argumentList);
            protected abstract SyntaxNode GetReceiver(SyntaxNode argument);
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
            var document = context.Document;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            await _argumentAnalyzer.ComputeRefactoringsAsync(
                context, root, cancellationToken).ConfigureAwait(false);

            if (_attributeArgumentAnalyzer != null)
            {
                await _attributeArgumentAnalyzer.ComputeRefactoringsAsync(
                    context, root, cancellationToken).ConfigureAwait(false);
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