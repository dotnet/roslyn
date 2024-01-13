// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddObsoleteAttribute
{
    internal abstract class AbstractAddObsoleteAttributeCodeFixProvider
        : SyntaxEditorBasedCodeFixProvider
    {
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly string _title;

        protected AbstractAddObsoleteAttributeCodeFixProvider(
            ISyntaxFacts syntaxFacts,
            string title)
        {
            _syntaxFacts = syntaxFacts;
            _title = title;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;

            var attribute = await GetObsoleteAttributeAsync(document, cancellationToken).ConfigureAwait(false);
            if (attribute == null)
            {
                return;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var node = context.Diagnostics[0].Location.FindNode(cancellationToken);

            var container = GetContainer(root, node);

            if (container == null)
            {
                return;
            }

            RegisterCodeFix(context, _title, _title);
        }

        private static async Task<INamedTypeSymbol?> GetObsoleteAttributeAsync(Document document, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var attribute = compilation.GetTypeByMetadataName(typeof(ObsoleteAttribute).FullName!);
            return attribute;
        }

        private SyntaxNode? GetContainer(SyntaxNode root, SyntaxNode node)
        {
            return _syntaxFacts.GetContainingMemberDeclaration(root, node.SpanStart) ??
                   _syntaxFacts.GetContainingTypeDeclaration(root, node.SpanStart);
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var obsoleteAttribute = await GetObsoleteAttributeAsync(document, cancellationToken).ConfigureAwait(false);

            // RegisterCodeFixesAsync checked for null
            Contract.ThrowIfNull(obsoleteAttribute);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

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
    }
}
