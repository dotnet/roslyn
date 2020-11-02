// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        protected abstract string GetText(Accessibility accessibility);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var diagnosticNode = root.FindNode(context.Span);
            if (!syntaxFacts.IsDeclaration(diagnosticNode))
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declaredSymbol = semanticModel.GetDeclaredSymbol(diagnosticNode, cancellationToken);
            if (declaredSymbol is not (IPropertySymbol or IMethodSymbol or IEventSymbol))
            {
                return;
            }

            var diagnostic = context.Diagnostics[0];

            if (declaredSymbol.IsOverride)
            {
                // only show accessibility of base definition for override
                var original = declaredSymbol.GetOverriddenMember();
                if (original is null)
                {
                    return;
                }

                var accessibility = original.ComputeResultantAccessibility(declaredSymbol.ContainingType);

                if (accessibility == Accessibility.Internal
                    && !original.ContainingAssembly.GivesAccessTo(declaredSymbol.ContainingAssembly))
                {
                    // not able to override inaccessible member - return
                    return;
                }

                if (accessibility == Accessibility.Private)
                {
                    // should be unreachable - return for robustness
                    return;
                }

                context.RegisterCodeFix(
                    new MyCodeAction(
                        string.Format(FeaturesResources.Change_accessibility_to_0, GetText(accessibility)),
                        ct => ChangeAccessibilityAsync(accessibility, document, diagnosticNode, ct)),
                    diagnostic);
                return;
            }

            context.RegisterCodeFix(
                new MyNestedAction(
                    FeaturesResources.Change_accessibility_to,
                    ImmutableArray.Create(
                        CreateAction(Accessibility.Public),
                        CreateAction(Accessibility.Protected),
                        CreateAction(Accessibility.Internal),
                        CreateAction(Accessibility.ProtectedOrInternal),
                        CreateAction(Accessibility.ProtectedAndInternal)),
                    isInlinable: false),
                diagnostic);

            return;

            CodeAction CreateAction(Accessibility accessibility)
                => new MyCodeAction(
                    GetText(accessibility),
                    ct => ChangeAccessibilityAsync(accessibility, document, diagnosticNode, ct));
        }

        private static async Task<Document> ChangeAccessibilityAsync(
            Accessibility accessibility,
            Document document,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.SetAccessibility(declaration, accessibility);
            return editor.GetChangedDocument();
        }

        private class MyNestedAction : CodeAction.CodeActionWithNestedActions
        {
            public MyNestedAction(string title, ImmutableArray<CodeAction> nestedActions, bool isInlinable)
                : base(title, nestedActions, isInlinable)
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
