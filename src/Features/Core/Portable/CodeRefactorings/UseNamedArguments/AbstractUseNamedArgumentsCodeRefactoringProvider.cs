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
    internal abstract class AbstractUseNamedArgumentsCodeRefactoringProvider<
        TBaseArgumentSyntax,
        TArgumentSyntax,
        TAttributeArgumentSyntax,
        TArgumentListSyntax,
        TAttributeArgumentListSyntax> : CodeRefactoringProvider
        where TBaseArgumentSyntax : SyntaxNode
        where TArgumentSyntax : TBaseArgumentSyntax
        where TAttributeArgumentSyntax : SyntaxNode
        where TArgumentListSyntax : SyntaxNode
        where TAttributeArgumentListSyntax : SyntaxNode
    {
        protected abstract class Analyzer<TBaseSyntax, TSyntax, TListSyntax>
            where TBaseSyntax : SyntaxNode
            where TSyntax : TBaseSyntax
            where TListSyntax : SyntaxNode
        {
            public async Task ComputeRefactoringsAsync(
                CodeRefactoringContext context, SyntaxNode root, CancellationToken cancellationToken)
            {
                var document = context.Document;

                var argument = root.FindNode(context.Span).FirstAncestorOrSelf<TBaseSyntax>() as TSyntax;
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

                var argumentList = (TListSyntax)argument.Parent;
                var (index, count) = GetArgumentListIndexAndCount(argument, argumentList);

                var arguments = GetArguments(argumentList);
                for (var i = index; i < count; i++)
                {
                    if (!(arguments[i] is TSyntax))
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
                TSyntax firstArgument,
                ImmutableArray<IParameterSymbol> parameters,
                int index)
            {
                var argumentList = (TListSyntax)firstArgument.Parent;
                var newArgumentList = GetOrSynthesizeNamedArguments(parameters, argumentList, index);
                var newRoot = root.ReplaceNode(argumentList, newArgumentList);
                return Task.FromResult(document.WithSyntaxRoot(newRoot));
            }

            private (int, int) GetArgumentListIndexAndCount(TSyntax argument, TListSyntax argumentList)
            {
                var arguments = GetArguments(argumentList);
                return (arguments.IndexOf(argument), arguments.Count);
            }

            private TListSyntax GetOrSynthesizeNamedArguments(
                ImmutableArray<IParameterSymbol> parameters, TListSyntax argumentList, int index)
            {
                var arguments = GetArguments(argumentList);
                var namedArguments = arguments
                    .Select((argument, i) => i >= index && argument is TSyntax s && IsPositionalArgument(s)
                        ? WithName(s, parameters[i].Name).WithTriviaFrom(argument)
                        : argument);

                return WithArguments(argumentList, namedArguments, arguments.GetSeparators());
            }

            protected abstract TListSyntax WithArguments(
                TListSyntax argumentList, IEnumerable<TBaseSyntax> namedArguments, IEnumerable<SyntaxToken> separators);

            protected abstract bool IsLegalToAddNamedArguments(ImmutableArray<IParameterSymbol> parameters, int argumentCount);
            protected abstract TSyntax WithName(TSyntax argument, string name);
            protected abstract bool IsPositionalArgument(TSyntax argument);
            protected abstract SeparatedSyntaxList<TBaseSyntax> GetArguments(TListSyntax argumentList);
            protected abstract SyntaxNode GetReceiver(SyntaxNode argument);
        }

        private readonly Analyzer<TBaseArgumentSyntax, TArgumentSyntax, TArgumentListSyntax> _argumentAnalyzer;
        private readonly Analyzer<TAttributeArgumentSyntax, TAttributeArgumentSyntax, TAttributeArgumentListSyntax> _attributeArgumentAnalyzer;

        protected AbstractUseNamedArgumentsCodeRefactoringProvider(
            Analyzer<TBaseArgumentSyntax, TArgumentSyntax, TArgumentListSyntax> argumentAnalyzer,
            Analyzer<TAttributeArgumentSyntax, TAttributeArgumentSyntax, TAttributeArgumentListSyntax> attributeArgumentAnalyzer)
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