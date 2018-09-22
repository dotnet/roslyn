// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Threading;
using System;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddNew), Shared]
    internal partial class HideBaseCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        // 'SomeClass.SomeMember' hides inherited member 'SomeClass.SomeMember'. Use the new keyword if hiding was intended.
        internal const string CS0108 = nameof(CS0108);

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0108);

        public HideBaseCodeFixProvider()
            : base(supportsFixAll: false)
        {
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnostic = diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var token = root.FindToken(diagnosticSpan.Start);
            SyntaxNode originalNode = token.GetAncestor<PropertyDeclarationSyntax>();

            if (originalNode == null)
            {
                originalNode = token.GetAncestor<MethodDeclarationSyntax>();
            }

            if (originalNode == null)
            {
                originalNode = token.GetAncestor<FieldDeclarationSyntax>();
            }

            if (originalNode == null)
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(document);
            var newNode = generator.WithModifiers(originalNode, generator.GetModifiers(originalNode).WithIsNew(true));
            editor.ReplaceNode(originalNode, newNode);
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
             c => FixAsync(context.Document, context.Diagnostics[0], c)),
             context.Diagnostics);
            return Task.CompletedTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Hide_base_member, createChangedDocument)
            {
            }
        }
    }
}
