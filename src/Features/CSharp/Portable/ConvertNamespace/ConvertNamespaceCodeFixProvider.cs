// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertNamespaceCodeFixProvider)), Shared]
    internal class ConvertNamespaceCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertNamespaceCodeFixProvider()
        {
        }

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseRegularNamespaceDiagnosticId, IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            var title = diagnostic.Id == IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId
                ? CSharpFeaturesResources.Convert_to_file_scoped_namespace
                : CSharpFeaturesResources.Convert_to_regular_namespace;

            context.RegisterCodeFix(
                new MyCodeAction(title, c => FixAsync(context.Document, diagnostic, c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var diagnostic = diagnostics.First();

            var namespaceDecl = (BaseNamespaceDeclarationSyntax)diagnostic.Location.FindNode(cancellationToken);
            var converted = ConvertNamespaceHelper.Convert(namespaceDecl);

            editor.ReplaceNode(namespaceDecl, converted);
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
