// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ChangeAccessibilityModifier
{
    internal abstract class AbstractChangeAccessibilityModifierCodeFixProvider : CodeFixProvider
    {
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Innermost: We are looking for an IdentifierName. IdentifierName is sometimes at the same span as its parent (e.g. SimpleBaseTypeSyntax).
            var diagnosticNode = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (!syntaxFacts.IsDeclaration(diagnosticNode))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declaredSymbol = semanticModel.GetDeclaredSymbol(diagnosticNode, cancellationToken);
            if (declaredSymbol is { IsOverride: true } or not (IPropertySymbol or IMethodSymbol or IEventSymbol))
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];
            context.RegisterCodeFix(
                new MyNestedAction(
                    "Change accessibility",
                    ImmutableArray.Create<CodeAction>(
                        new MyCodeAction(
                            "Use 'public' accessibility",
                            ct => ChangeAccessibilityAsync(document, diagnosticNode, Accessibility.Public, ct)),
                        new MyCodeAction(
                            "Use 'protected' accessibility",
                            ct => ChangeAccessibilityAsync(document, diagnosticNode, Accessibility.Protected, ct)),
                        new MyCodeAction(
                            "Use 'internal' accessibility",
                            ct => ChangeAccessibilityAsync(document, diagnosticNode, Accessibility.Internal, ct)),
                        new MyCodeAction(
                            "Use 'protected internal' accessibility",
                            ct => ChangeAccessibilityAsync(document, diagnosticNode, Accessibility.ProtectedOrInternal, ct)),
                        new MyCodeAction(
                            "Use 'private protected' accessibility",
                            ct => ChangeAccessibilityAsync(document, diagnosticNode, Accessibility.ProtectedAndInternal, ct))),
                    isInlinable: true),
                diagnostic);
        }

        private static async Task<Document> ChangeAccessibilityAsync(
            Document document,
            SyntaxNode declaration,
            Accessibility accessibility,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.SetAccessibility(declaration, accessibility);
            return editor.GetChangedDocument();
        }

        private class MyNestedAction : CodeAction.CodeActionWithNestedActions
        {
            public MyNestedAction(string title, ImmutableArray<CodeAction> nestedActions, bool isInlinable)
                : base(title, nestedActions, isInlinable, CodeActionPriority.High)
            {
            }
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
