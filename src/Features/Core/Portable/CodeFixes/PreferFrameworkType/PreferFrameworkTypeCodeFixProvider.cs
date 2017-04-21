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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.PreferFrameworkType
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeFixProviderNames.PreferFrameworkType), Shared]
    internal class PreferFrameworkTypeCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
                IDEDiagnosticIds.PreferFrameworkTypeInDeclarationsDiagnosticId,
                IDEDiagnosticIds.PreferFrameworkTypeInMemberAccessDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => BatchFixAllProvider.Instance;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new PreferFrameworkTypeCodeAction(
                    FeaturesResources.Use_framework_type,
                    c => CreateChangedDocumentAsync(context.Document, context.Span, c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        private async Task<Document> CreateChangedDocumentAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(span, findInsideTrivia: true, getInnermostNodeForTie: true);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = document.GetLanguageService<SyntaxGenerator>();
            var typeSymbol = (ITypeSymbol)semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;
            var replacementNode = generator.TypeExpression(typeSymbol).WithTriviaFrom(node);

            return document.WithSyntaxRoot(root.ReplaceNode(node, replacementNode));
        }

        private class PreferFrameworkTypeCodeAction : CodeAction.DocumentChangeAction
        {
            public PreferFrameworkTypeCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument, equivalenceKey: title)
            {
            }
        }
    }
}
