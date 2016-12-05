// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.UseNamedArguments
{
    internal abstract class AbstractUseNamedArgumentsCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var argument = root.FindNode(context.Span).FirstAncestorOrSelf<SyntaxNode>(IsCandidate);
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

            var t = GetArgumentListIndexAndCount(argument);
            if (t.Item1 < 0)
            {
                return;
            }

            if (!IsLegalToAddNamedArguments(parameters, t.Item2))
            {
                return;
            }

            var argumentName = parameters[t.Item1].Name;
            context.RegisterRefactoring(new MyCodeAction(string.Format(FeaturesResources.Add_argument_name_0, argumentName),
                c => AddNamedArgumentsAsync(root, document, argument, parameters, t.Item1)));
        }

        private Task<Document> AddNamedArgumentsAsync(
            SyntaxNode root,
            Document document,
            SyntaxNode firstArgument,
            ImmutableArray<IParameterSymbol> parameters,
            int index)
        {
            var argumentList = firstArgument.Parent;
            var newArgumentList = GetOrSynthesizeNamedArguments(parameters, argumentList, index);
            var newRoot = root.ReplaceNode(argumentList, newArgumentList);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        protected abstract bool IsCandidate(SyntaxNode node);
        protected abstract bool IsPositionalArgument(SyntaxNode argument);
        protected abstract bool IsLegalToAddNamedArguments(ImmutableArray<IParameterSymbol> parameters, int argumentCount);
        protected abstract ValueTuple<int, int> GetArgumentListIndexAndCount(SyntaxNode argument);
        protected abstract SyntaxNode GetReceiver(SyntaxNode argument);
        protected abstract SyntaxNode GetOrSynthesizeNamedArguments(ImmutableArray<IParameterSymbol> parameters, SyntaxNode argumentList, int index);

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
