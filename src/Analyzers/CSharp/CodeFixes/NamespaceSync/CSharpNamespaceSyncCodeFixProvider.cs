// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.NamespaceSync
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddBraces), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddAwait)]
    internal class CSharpNamespaceSyncCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpNamespaceSyncCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.NamespaceSyncAnalyzerDiagnosticId);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var namespaceDecls = new List<(NamespaceDeclarationSyntax, string)>();
            foreach (var diagnostic in diagnostics)
            {
                var targetNamespace = diagnostic.Properties["TargetNamespace"];
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                namespaceDecls.Add((
                    root.FindNode(diagnosticSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<NamespaceDeclarationSyntax>(),
                    targetNamespace));
            }

            ChangeNamespaceDeclarations(editor, namespaceDecls);
        }

        private static void ChangeNamespaceDeclarations(
            SyntaxEditor editor,
            List<(NamespaceDeclarationSyntax namespaceDecl, string targetNamespace)> namespaceDeclsToNamespaceMap)
        {
            var generator = editor.Generator;
            foreach (var (namespaceDecl, targetNamespace) in namespaceDeclsToNamespaceMap)
            {
                editor.ReplaceNode(
                    namespaceDecl,
                    (oldNode, generator) => generator.WithName(oldNode, targetNamespace));
            }
        }

        private sealed class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Namespace_must_match_folder_structure, createChangedDocument, CSharpAnalyzersResources.Namespace_must_match_folder_structure)
            {
            }
        }
    }
}
