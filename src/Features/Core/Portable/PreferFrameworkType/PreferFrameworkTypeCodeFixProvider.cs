// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.PreferFrameworkType
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeFixProviderNames.PreferFrameworkType), Shared]
    internal class PreferFrameworkTypeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public PreferFrameworkTypeCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            if (diagnostic.Properties.ContainsKey(PreferFrameworkTypeConstants.PreferFrameworkType))
            {
                context.RegisterCodeFix(
                    new PreferFrameworkTypeCodeAction(
                        c => FixAsync(context.Document, diagnostic, c)),
                    context.Diagnostics);
            }

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

                if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is ITypeSymbol typeSymbol)
                {
                    var replacementNode = generator.TypeExpression(typeSymbol).WithTriviaFrom(node);
                    editor.ReplaceNode(node, replacementNode);
                }
            }
        }

        protected override bool IncludeDiagnosticDuringFixAll(FixAllState state, Diagnostic diagnostic, CancellationToken cancellationToken)
            => diagnostic.Properties.ContainsKey(PreferFrameworkTypeConstants.PreferFrameworkType);

        private class PreferFrameworkTypeCodeAction : CodeAction.DocumentChangeAction
        {
            public PreferFrameworkTypeCodeAction(
                Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_framework_type, createChangedDocument, FeaturesResources.Use_framework_type)
            {
            }
        }
    }
}
