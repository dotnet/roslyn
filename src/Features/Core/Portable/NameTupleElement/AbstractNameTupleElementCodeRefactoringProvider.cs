// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    abstract class AbstractNameTupleElementCodeRefactoringProvider<TArgumentSyntax, TTupleExpressionSyntax> : CodeRefactoringProvider
        where TArgumentSyntax : SyntaxNode
        where TTupleExpressionSyntax : SyntaxNode
    {
        protected abstract bool IsCloseParenOrComma(SyntaxToken token);
        protected abstract TArgumentSyntax WithName(TArgumentSyntax argument, string argumentName);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            var (_, argument, elementName) = await TryGetArgumentInfo(document, span, cancellationToken).ConfigureAwait(false);

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

        private async Task<(SyntaxNode root, TArgumentSyntax argument, string argumentName)> TryGetArgumentInfo(
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
            var (root, argument, elementName) = await TryGetArgumentInfo(document, span, cancellationToken).ConfigureAwait(false);

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
