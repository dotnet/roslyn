﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NameTupleElement
{
    internal abstract class AbstractNameTupleElementCodeRefactoringProvider<TArgumentSyntax, TTupleExpressionSyntax> : CodeRefactoringProvider
        where TArgumentSyntax : SyntaxNode
        where TTupleExpressionSyntax : SyntaxNode
    {
        protected abstract TArgumentSyntax WithName(TArgumentSyntax argument, string argumentName);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            var (_, argument, elementName) = await TryGetArgumentInfoAsync(document, span, cancellationToken).ConfigureAwait(false);

            if (elementName == null)
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    string.Format(FeaturesResources.Add_tuple_element_name_0, elementName),
                    c => AddNamedElementAsync(document, span, cancellationToken)),
                argument.Span);
        }

        private static async Task<(SyntaxNode root, TArgumentSyntax argument, string argumentName)> TryGetArgumentInfoAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return default;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var potentialArguments = await document.GetRelevantNodesAsync<TArgumentSyntax>(span, cancellationToken).ConfigureAwait(false);
            var argument = potentialArguments.FirstOrDefault(n => n?.Parent is TTupleExpressionSyntax);
            if (argument == null || !syntaxFacts.IsSimpleArgument(argument))
            {
                return default;
            }

            var tuple = (TTupleExpressionSyntax)argument.Parent;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (!(semanticModel.GetTypeInfo(tuple, cancellationToken).ConvertedType is INamedTypeSymbol tupleType))
            {
                return default;
            }

            syntaxFacts.GetPartsOfTupleExpression<TArgumentSyntax>(tuple, out _, out var arguments, out _);
            var argumentIndex = arguments.IndexOf(argument);
            var elements = tupleType.TupleElements;
            if (elements.IsDefaultOrEmpty || argumentIndex >= elements.Length)
            {
                return default;
            }

            var element = elements[argumentIndex];
            if (element.Equals(element.CorrespondingTupleField))
            {
                return default;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return (root, argument, element.Name);
        }

        private async Task<Document> AddNamedElementAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var (root, argument, elementName) = await TryGetArgumentInfoAsync(document, span, cancellationToken).ConfigureAwait(false);

            var newArgument = WithName(argument, elementName).WithTriviaFrom(argument);
            var newRoot = root.ReplaceNode(argument, newArgument);
            return document.WithSyntaxRoot(newRoot);
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
