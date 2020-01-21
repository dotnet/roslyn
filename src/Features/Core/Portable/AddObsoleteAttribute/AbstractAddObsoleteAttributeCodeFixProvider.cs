// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddObsoleteAttribute
{
    internal abstract class AbstractAddObsoleteAttributeCodeFixProvider
        : SyntaxEditorBasedCodeFixProvider
    {
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly string _title;

        protected AbstractAddObsoleteAttributeCodeFixProvider(
            ISyntaxFactsService syntaxFacts,
            string title)
        {
            _syntaxFacts = syntaxFacts;
            _title = title;
        }

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;

            var attribute = await GetObsoleteAttributeAsync(document, cancellationToken).ConfigureAwait(false);
            if (attribute == null)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var diagnotic = context.Diagnostics[0];
            var node = diagnotic.Location.FindNode(cancellationToken);

            var container = GetContainer(root, node);

            if (container == null)
            {
                return;
            }

            context.RegisterCodeFix(
                new MyCodeAction(
                    _title,
                    c => FixAsync(document, diagnotic, c)),
                context.Diagnostics);
        }

        private static async Task<INamedTypeSymbol> GetObsoleteAttributeAsync(Document document, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var attribute = compilation.GetTypeByMetadataName(typeof(ObsoleteAttribute).FullName);
            return attribute;
        }

        private SyntaxNode GetContainer(SyntaxNode root, SyntaxNode node)
        {
            return _syntaxFacts.GetContainingMemberDeclaration(root, node.SpanStart) ??
                   _syntaxFacts.GetContainingTypeDeclaration(root, node.SpanStart);
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var obsoleteAttribute = await GetObsoleteAttributeAsync(document, cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var containers = diagnostics.Select(d => GetContainer(root, d.Location.FindNode(cancellationToken)))
                                        .WhereNotNull()
                                        .ToSet();

            var generator = editor.Generator;
            foreach (var container in containers)
            {
                editor.AddAttribute(container,
                    generator.Attribute(editor.Generator.TypeExpression(obsoleteAttribute)));
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
