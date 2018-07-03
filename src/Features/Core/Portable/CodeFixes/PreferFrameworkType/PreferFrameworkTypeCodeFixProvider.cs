// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.PreferFrameworkType
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeFixProviderNames.PreferFrameworkType), Shared]
    internal class PreferFrameworkTypeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public const string EquivalenceKey = nameof(EquivalenceKey);
        public const string DeclarationsEquivalenceKey = nameof(DeclarationsEquivalenceKey);
        public const string MemberAccessEquivalenceKey = nameof(MemberAccessEquivalenceKey);

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
                IDEDiagnosticIds.PreferFrameworkTypeInDeclarationsDiagnosticId,
                IDEDiagnosticIds.PreferFrameworkTypeInMemberAccessDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var equivalenceKey = diagnostic.Properties[EquivalenceKey];
            context.RegisterCodeFix(
                new PreferFrameworkTypeCodeAction(
                    c => this.FixAsync(context.Document, context.Diagnostics[0], c),
                    equivalenceKey),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var generator = document.GetLanguageService<SyntaxGenerator>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                var node = diagnostic.Location.FindNode(
                    findInsideTrivia: true, getInnermostNodeForTie: true, cancellationToken);

                var typeSymbol = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol as ITypeSymbol;
                if (typeSymbol != null)
                {
                    var replacementNode = generator.TypeExpression(typeSymbol).WithTriviaFrom(node);
                    editor.ReplaceNode(node, replacementNode);
                }
            }
        }

        protected override bool IncludeDiagnosticDuringFixAll(FixAllState state, Diagnostic diagnostic)
            => diagnostic.Properties[EquivalenceKey] == state.CodeActionEquivalenceKey;

        private class PreferFrameworkTypeCodeAction : CodeAction.DocumentChangeAction
        {
            public PreferFrameworkTypeCodeAction(
                Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(FeaturesResources.Use_framework_type, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}
