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
    internal abstract class AbstractUseIsNullCheckForReferenceEqualsCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public const string Negated = nameof(Negated);
        public const string UnconstrainedGeneric = nameof(UnconstrainedGeneric);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseIsNullCheckDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        protected abstract string GetIsNullTitle();
        protected abstract string GetIsNotNullTitle();
        protected abstract SyntaxNode CreateNullCheck(SyntaxNode argument, bool isUnconstrainedGeneric);
        protected abstract SyntaxNode CreateNotNullCheck(SyntaxNode argument);

        private static bool IsSupportedDiagnostic(Diagnostic diagnostic)
            => diagnostic.Properties[UseIsNullConstants.Kind] == UseIsNullConstants.ReferenceEqualsKey;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            if (IsSupportedDiagnostic(diagnostic))
            {
                var negated = diagnostic.Properties.ContainsKey(Negated);
                var title = negated ? GetIsNotNullTitle() : GetIsNullTitle();

                context.RegisterCodeFix(
                    new MyCodeAction(title, c => FixAsync(context.Document, diagnostic, c)),
                    context.Diagnostics);
            }

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            // Order in reverse so we process inner diagnostics before outer diagnostics.
            // Otherwise, we won't be able to find the nodes we want to replace if they're
            // not there once their parent has been replaced.
            foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
            {
                if (!IsSupportedDiagnostic(diagnostic))
                {
                    continue;
                }

                var invocation = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken: cancellationToken);
                var negate = diagnostic.Properties.ContainsKey(Negated);
                var isUnconstrainedGeneric = diagnostic.Properties.ContainsKey(UnconstrainedGeneric);

                var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
                var argument = syntaxFacts.IsNullLiteralExpression(syntaxFacts.GetExpressionOfArgument(arguments[0]))
                    ? syntaxFacts.GetExpressionOfArgument(arguments[1])
                    : syntaxFacts.GetExpressionOfArgument(arguments[0]);

                var toReplace = negate ? invocation.Parent : invocation;
                var replacement = negate
                    ? CreateNotNullCheck(argument)
                    : CreateNullCheck(argument, isUnconstrainedGeneric);

                editor.ReplaceNode(
                    toReplace,
                    replacement.WithTriviaFrom(toReplace));
            }

            return Task.CompletedTask;
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
