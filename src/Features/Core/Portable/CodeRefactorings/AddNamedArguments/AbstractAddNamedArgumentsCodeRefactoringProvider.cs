// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.AddNamedArguments
{
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    internal abstract class AbstractAddNamedArgumentsCodeRefactoringProvider<TArgumentSyntax> : CodeRefactoringProvider where TArgumentSyntax : SyntaxNode
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

            var node = root.FindNode(context.Span).FirstAncestorOrSelf<SyntaxNode>(IsCandidate);
            if (node == null)
            {
                return;
            }

            if (node.ContainsDiagnostics)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var target = GetTargetNode(node);
            var symbol = semanticModel.GetSymbolInfo(target, cancellationToken).Symbol;
            if (symbol == null)
            {
                return;
            }

            var parameters = symbol.GetParameters();
            if (parameters.Length == 0)
            {
                return;
            }

            SeparatedSyntaxList<TArgumentSyntax> argumentList;
            if (!TryGetArguments(node, semanticModel, out argumentList))
            {
                return;
            }

            if (argumentList.Count == 0)
            {
                return;
            }

            bool hasLiteral;
            TArgumentSyntax[] namedArguments;
            if (!TryGetOrSynthesizeNamedArguments(argumentList, parameters, out namedArguments, out hasLiteral))
            {
                return;
            }

            if (hasLiteral)
            {
                context.RegisterRefactoring(new MyCodeAction(FeaturesResources.AddNamedArgumentsLiteralsOnly,
                    c => AddNamedArgumentsAsync(node, root, document, argumentList, namedArguments, literalsOnly: true)));
            }

            context.RegisterRefactoring(new MyCodeAction(FeaturesResources.AddNamedArguments,
                c => AddNamedArgumentsAsync(node, root, document, argumentList, namedArguments, literalsOnly: false)));
        }

        private Task<Document> AddNamedArgumentsAsync(
            SyntaxNode node,
            SyntaxNode root,
            Document document,
            SeparatedSyntaxList<TArgumentSyntax> argumentList,
            TArgumentSyntax[] namedArguments,
            bool literalsOnly)
        {
            var newArgumentList = ReplaceArguments(argumentList, namedArguments, literalsOnly);
            var newNode = ReplaceArgumentList(node, newArgumentList);
            var newRoot = root.ReplaceNode(node, newNode);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private SeparatedSyntaxList<TArgumentSyntax> ReplaceArguments(SeparatedSyntaxList<TArgumentSyntax> argumentList, TArgumentSyntax[] namedArguments, bool literalsOnly)
        {
            for (int index = 0; index < namedArguments.Length; ++index)
            {
                if (literalsOnly && !IsLiteral(namedArguments[index]))
                {
                    continue;
                }

                for (; index < namedArguments.Length; ++index)
                {
                    argumentList = argumentList.Replace(argumentList[index], namedArguments[index]);
                }

                // Truncate arguments
                var remainingArguments = argumentList.Count - index;
                while (remainingArguments-- > 0)
                {
                    argumentList = argumentList.RemoveAt(index);
                }

                break;
            }

            return argumentList;
        }

        protected static bool IsArray(SemanticModel semanticModel, SyntaxNode node)
        {
            System.Diagnostics.Debug.Assert(node != null);
            return semanticModel.GetTypeInfo(node).Type?.TypeKind == TypeKind.Array;
        }

        protected abstract bool IsCandidate(SyntaxNode node);
        protected abstract bool IsLiteral(TArgumentSyntax argument);
        protected abstract bool TryGetArguments(SyntaxNode node, SemanticModel semanticModel, out SeparatedSyntaxList<TArgumentSyntax> arguments);
        protected abstract bool TryGetOrSynthesizeNamedArguments(SeparatedSyntaxList<TArgumentSyntax> arguments, ImmutableArray<IParameterSymbol> parameters, out TArgumentSyntax[] namedArguments, out bool hasLiteral);
        protected abstract SyntaxNode ReplaceArgumentList(SyntaxNode node, SeparatedSyntaxList<TArgumentSyntax> argumentList);
        protected abstract SyntaxNode GetTargetNode(SyntaxNode node);

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
