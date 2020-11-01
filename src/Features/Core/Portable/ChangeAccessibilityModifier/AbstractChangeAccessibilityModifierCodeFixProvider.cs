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
        private readonly string _title = FeaturesResources.Change_accessibility;
        private readonly string _titleFormat = FeaturesResources.Change_accessibility_to_0;

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

                context.RegisterCodeFix(CreateAction(accessibility), diagnostic);
                return;
            }

            context.RegisterCodeFix(
                new MyNestedAction(
                    _title,
                    ImmutableArray.Create(
                        CreateAction(Accessibility.Public),
                        CreateAction(Accessibility.Protected),
                        CreateAction(Accessibility.Internal),
                        CreateAction(Accessibility.ProtectedOrInternal),
                        CreateAction(Accessibility.ProtectedAndInternal)),
                    isInlinable: true),
                diagnostic);

            CodeAction CreateAction(Accessibility accessibility)
                => new MyCodeAction(
                    string.Format(_titleFormat, GetText(accessibility)),
                    async ct =>
                    {
                        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                        editor.SetAccessibility(diagnosticNode, accessibility);
                        return editor.GetChangedDocument();
                    });
        }

        protected abstract string GetText(Accessibility accessibility);

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
