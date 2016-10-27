// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.UseNamedArguments
{
    internal abstract class AbstractUseNamedArgumentsCodeRefactoringProvider<TArgumentSyntax> : CodeRefactoringProvider
        where TArgumentSyntax : SyntaxNode
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

            SyntaxNode targetNode;
            var arguments = GetArguments(node, out targetNode);
            if (arguments.Count == 0)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetSymbolInfo(targetNode, cancellationToken).Symbol;
            if (symbol == null)
            {
                return;
            }

            var parameters = symbol.GetParameters();
            if (parameters.Length == 0)
            {
                return;
            }

            bool hasLiteral;
            TArgumentSyntax[] namedArguments;
            if (!TryGetOrSynthesizeNamedArguments(arguments, parameters, out namedArguments, out hasLiteral))
            {
                return;
            }

            if (hasLiteral)
            {
                context.RegisterRefactoring(new MyCodeAction(FeaturesResources.Use_named_arguments_for_literals,
                    c => UseNamedArgumentsAsync(node, root, document, arguments, namedArguments, literalsOnly: true)));
            }

            context.RegisterRefactoring(new MyCodeAction(FeaturesResources.Use_named_arguments,
                c => UseNamedArgumentsAsync(node, root, document, arguments, namedArguments, literalsOnly: false)));
        }

        private Task<Document> UseNamedArgumentsAsync(
            SyntaxNode node,
            SyntaxNode root,
            Document document,
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            TArgumentSyntax[] namedArguments,
            bool literalsOnly)
        {
            var newArguments = ReplaceArguments(arguments, namedArguments, literalsOnly);
            var newNode = ReplaceArgumentList(node, newArguments);
            var newRoot = root.ReplaceNode(node, newNode);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private SeparatedSyntaxList<TArgumentSyntax> ReplaceArguments(
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            TArgumentSyntax[] namedArguments,
            bool literalsOnly)
        {
            int index = 0;
            if (literalsOnly)
            {
                for (; index < namedArguments.Length; ++index)
                {
                    if (IsLiteral(namedArguments[index]))
                    {
                        break;
                    }
                }
            }

            for (; index < namedArguments.Length; ++index)
            {
                arguments = arguments.Replace(arguments[index], namedArguments[index]);
            }

            // Truncate arguments
            var remainingArguments = arguments.Count - index;
            while (remainingArguments-- > 0)
            {
                arguments = arguments.RemoveAt(index);
            }

            return arguments;
        }

        protected abstract bool IsCandidate(SyntaxNode node);
        protected abstract bool IsLiteral(TArgumentSyntax argument);
        protected abstract SeparatedSyntaxList<TArgumentSyntax> GetArguments(SyntaxNode node, out SyntaxNode targetNode);
        protected abstract SyntaxNode ReplaceArgumentList(SyntaxNode node, SeparatedSyntaxList<TArgumentSyntax> arguments);
        protected abstract bool TryGetOrSynthesizeNamedArguments(
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            ImmutableArray<IParameterSymbol> parameters,
            out TArgumentSyntax[] namedArguments,
            out bool hasLiteral);

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
