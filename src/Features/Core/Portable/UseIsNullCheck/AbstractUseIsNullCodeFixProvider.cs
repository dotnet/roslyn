// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseIsNullCheck
{
    internal abstract class AbstractUseIsNullCheckCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public const string Negated = nameof(Negated);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseIsNullCheckDiagnosticId);

        protected abstract string GetIsNullTitle();
        protected abstract string GetIsNotNullTitle();
        protected abstract SyntaxNode CreateIsNullCheck(SyntaxNode argument);
        protected abstract SyntaxNode CreateIsNotNullCheck(SyntaxNode notExpression, SyntaxNode argument);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var negated = diagnostic.Properties.ContainsKey(Negated);
            var title = negated ? GetIsNotNullTitle() : GetIsNullTitle();

            context.RegisterCodeFix(
                new MyCodeAction(title, c => this.FixAsync(context.Document, diagnostic, c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            foreach (var diagnostic in diagnostics)
            {
                var invocation = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken: cancellationToken);
                var negate = diagnostic.Properties.ContainsKey(Negated);

                var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
                var argument = syntaxFacts.IsNullLiteralExpression(syntaxFacts.GetExpressionOfArgument(arguments[0]))
                    ? syntaxFacts.GetExpressionOfArgument(arguments[1])
                    : syntaxFacts.GetExpressionOfArgument(arguments[0]);

                var toReplace = negate ? invocation.Parent : invocation;
                var replacement = negate ? CreateIsNotNullCheck(invocation.Parent, argument) : CreateIsNullCheck(argument);

                editor.ReplaceNode(
                    toReplace,
                    replacement.WithTriviaFrom(toReplace));
            }

            return SpecializedTasks.EmptyTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}
