// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.MakeRefStruct
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpMakeRefStructCodeFixProvider : CodeFixProvider
    {
        // Error CS8345: Field or auto-implemented property cannot be of certain type unless it is an instance member of a ref struct.
        private const string CS8345 = nameof(CS8345);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS8345);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var span = context.Span;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var structDeclaration = FindContainingStruct(root, span);

            // CS8345 could be triggered when struct is already marked with `ref` but a property is static
            if (structDeclaration != null && !structDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.RefKeyword)))
            {
                context.RegisterCodeFix(
                    new MyCodeAction(
                        CSharpFeaturesResources.Make_ref_struct,
                        c => FixCodeAsync(context.Document, context.Span, c)),
                    context.Diagnostics);
            }
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        private async Task<Document> FixCodeAsync(Document document, TextSpan span, CancellationToken c)
        {
            var root = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);

            var structDeclaration = FindContainingStruct(root, span);
            var newStruct = UpdateStructDeclaration(document, structDeclaration);
            var newRoot = root.ReplaceNode(structDeclaration, newStruct);

            return document.WithSyntaxRoot(newRoot);
        }

        private StructDeclarationSyntax FindContainingStruct(SyntaxNode root, TextSpan span)
        {
            var member = root.FindNode(span);
            // Could be declared in a class or even in a nested class inside a struct,
            // so find only the first parent declaration
            return member.GetAncestor<TypeDeclarationSyntax>() as StructDeclarationSyntax;
        }

        private SyntaxNode UpdateStructDeclaration(Document document, StructDeclarationSyntax decl)
        {
            var gen = SyntaxGenerator.GetGenerator(document);
            return gen.WithModifiers(decl, gen.GetModifiers(decl).WithIsRef(true));
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
