// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Microsoft.CodeAnalysis.CSharp.AddNonNullTypes
{
    /// <summary>
    /// When some nullable-related syntax is encountered outside of a NonNullTYpes context,
    /// offer to add a type-level NonNullTypes attribute.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddNonNullTypes), Shared]
    internal class CSharpAddNonNullTypesCodeFixProvider : CodeFixProvider
    {
        // warning CS8632: The annotation for nullable reference types should only be used in code within a '[NonNullTypes(true)]' context.
        private const string CS8632 = nameof(CS8632);

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(CS8632);

        public CSharpAddNonNullTypesCodeFixProvider()
        {
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnosticSpan = context.Diagnostics.First().Location.SourceSpan;
            var token = root.FindToken(diagnosticSpan.Start);
            var containingType = token.GetAncestor<TypeDeclarationSyntax>();
            if (containingType == null)
            {
                return;
            }

            context.RegisterCodeFix(new MyCodeAction(context.Document, containingType), context.Diagnostics);
        }

        private class MyCodeAction : CodeAction
        {
            private Document _document;
            private TypeDeclarationSyntax _containingType;

            public override string Title => CSharpFeaturesResources.Add_NonNullTypes_attribute;

            public MyCodeAction(Document document, TypeDeclarationSyntax node)
            {
                _document = document;
                _containingType = node;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var newNode = GetNewNode(_document, _containingType, cancellationToken);
                var newRoot = root.ReplaceNode(_containingType, newNode);

                return _document.WithSyntaxRoot(newRoot);
            }

            private SyntaxNode GetNewNode(Document document, TypeDeclarationSyntax node, CancellationToken cancellationToken)
            {
                var generator = SyntaxGenerator.GetGenerator(_document);
                return generator.AddAttributes(_containingType, generator.Attribute("System.Runtime.CompilerServices.NonNullTypes"));
            }
        }
    }
}
