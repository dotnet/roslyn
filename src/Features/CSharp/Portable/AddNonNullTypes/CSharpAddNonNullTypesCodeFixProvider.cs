// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.AddNonNullTypes
{
    /// <summary>
    /// When some nullable-related syntax is encountered outside of a NonNullTypes context,
    /// offer to add a type-level NonNullTypes attribute.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddNonNullTypes), Shared]
    internal class CSharpAddNonNullTypesCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        // warning CS8632: The annotation for nullable reference types should only be used in code within a '[NonNullTypes(true)]' context.
        private const string CS8632 = nameof(CS8632);

        // warning CS8629: The suppression operator (!) should be used in code with a '[NonNullTypes(true/false)]' context.
        private const string CS8629 = nameof(CS8629);

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(CS8632, CS8629);

        public CSharpAddNonNullTypesCodeFixProvider()
            : base(supportsFixAll: false)
        {
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            Debug.Assert(diagnostics.Length == 1);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var generator = SyntaxGenerator.GetGenerator(document);

            var diagnosticSpan = diagnostics[0].Location.SourceSpan;
            var token = root.FindToken(diagnosticSpan.Start);
            var containingType = token.GetAncestor<TypeDeclarationSyntax>();
            if (containingType == null)
            {
                return;
            }

            var newNode = generator.AddAttributes(containingType, generator.Attribute("System.Runtime.CompilerServices.NonNullTypes"));
            editor.ReplaceNode(containingType, newNode);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Add_NonNullTypes_attribute, createChangedDocument)
            {
            }
        }
    }
}
