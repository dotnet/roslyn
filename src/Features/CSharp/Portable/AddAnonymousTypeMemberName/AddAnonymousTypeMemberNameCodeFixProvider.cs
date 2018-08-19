// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.AddAnonymousTypeMemberName
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class AddAnonymousTypeMemberNameCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const string CS0746 = nameof(CS0746); // Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.

        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(CS0746);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var diagnostic = context.Diagnostics[0];
            var declarator = await GetMemberDeclaratorAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);
            if (declarator == null)
            {
                return;
            }

            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(document, diagnostic, c)),
                context.Diagnostics);
        }

        private async Task<AnonymousObjectMemberDeclaratorSyntax> GetMemberDeclaratorAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var span = diagnostic.Location.SourceSpan;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(span, getInnermostNodeForTie: true) as ExpressionSyntax;
            if (node?.Span != span)
            {
                return null;
            }

            if (!(node.Parent is AnonymousObjectMemberDeclaratorSyntax declarator))
            {
                return null;
            }

            // Can't add a name of the declarator already has a name.
            if (declarator.NameEquals != null)
            {
                return null;
            }

            return declarator;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                await FixOneAsync(
                    document, semanticModel, diagnostic, 
                    editor, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FixOneAsync(
            Document document, SemanticModel semanticModel, Diagnostic diagnostic, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var declarator = await GetMemberDeclaratorAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);
            if (declarator == null)
            {
                return;
            }

            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();
            var name = semanticFacts.GenerateNameForExpression(semanticModel, declarator.Expression, capitalize: true, cancellationToken);
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            editor.ReplaceNode(
                declarator,
                (current, _) =>
                {
                    var currentDeclarator = (AnonymousObjectMemberDeclaratorSyntax)current;
                    return currentDeclarator.WithNameEquals(SyntaxFactory.NameEquals(name));
                });
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Add_member_name, createChangedDocument)
            {
            }
        }
    }
}
