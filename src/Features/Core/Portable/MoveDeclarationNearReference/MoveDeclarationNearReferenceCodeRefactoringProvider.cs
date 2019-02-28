// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MoveDeclarationNearReference
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeRefactoringProviderNames.MoveDeclarationNearReference), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.InlineTemporary)]
    internal sealed class MoveDeclarationNearReferenceCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (!textSpan.IsEmpty)
            {
                return;
            }

            var statement = await GetLocalDeclarationStatementAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (statement == null)
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(statement);
            if (variables.Count != 1)
            {
                return;
            }

            // Don't offer the refactoring inside the initializer for the variable.
            var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(variables[0]);
            var applicableSpan = initializer == null
                ? statement.Span
                : TextSpan.FromBounds(statement.SpanStart, initializer.SpanStart);

            if (!applicableSpan.IntersectsWith(textSpan.Start))
            {
                return;
            }

            var service = document.GetLanguageService<IMoveDeclarationNearReferenceService>();
            if (!await service.CanMoveDeclarationNearReferenceAsync(document, statement, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(c => MoveDeclarationNearReferenceAsync(document, textSpan, c)));
        }

        private async Task<SyntaxNode> GetLocalDeclarationStatementAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var position = textSpan.Start;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var statement = root.FindToken(position).Parent.Ancestors().FirstOrDefault(n => syntaxFacts.IsLocalDeclarationStatement(n));
            return statement;
        }

        private async Task<Document> MoveDeclarationNearReferenceAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var statement = await GetLocalDeclarationStatementAsync(document, span, cancellationToken).ConfigureAwait(false);
            var service = document.GetLanguageService<IMoveDeclarationNearReferenceService>();

            return await service.MoveDeclarationNearReferenceAsync(document, statement, cancellationToken).ConfigureAwait(false);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Move_declaration_near_reference, createChangedDocument)
            {
            }

            internal override CodeActionPriority Priority => CodeActionPriority.Low;
        }
    }
}
