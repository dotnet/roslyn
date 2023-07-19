// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders
{
    internal abstract class AbstractTypeSnippetProvider : AbstractSingleChangeSnippetProvider
    {
        protected abstract Task<bool> HasPrecedingAccessibilityModifiersAsync(Document document, int position, CancellationToken cancellationToken);
        protected abstract void GetTypeDeclarationIdentifier(SyntaxNode node, out SyntaxToken identifier);
        protected abstract Task<SyntaxNode> GenerateTypeDeclarationAsync(Document document, int position, bool useAccessibility, CancellationToken cancellationToken);

        protected override async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var hasAccessibilityModifiers = await HasPrecedingAccessibilityModifiersAsync(document, position, cancellationToken).ConfigureAwait(false);
            var useAccessibility = !hasAccessibilityModifiers && await AreAccessibilityModifiersRequiredAsync(document, cancellationToken).ConfigureAwait(false);

            var typeDeclaration = await GenerateTypeDeclarationAsync(document, position, useAccessibility, cancellationToken).ConfigureAwait(false);

            return new TextChange(TextSpan.FromBounds(position, position), typeDeclaration.NormalizeWhitespace().ToFullString());
        }

        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
            GetTypeDeclarationIdentifier(node, out var identifier);
            arrayBuilder.Add(new SnippetPlaceholder(identifier.ValueText, identifier.SpanStart));

            return arrayBuilder.ToImmutableArray();
        }

        private static async Task<bool> AreAccessibilityModifiersRequiredAsync(Document document, CancellationToken cancellationToken)
        {
            var options = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            var accessibilityModifiersRequired = options.GetEditorConfigOptionValue(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Never);
            return accessibilityModifiersRequired is AccessibilityModifiersRequired.ForNonInterfaceMembers or AccessibilityModifiersRequired.Always;
        }
    }
}
