// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractTypeSnippetProvider<TTypeDeclarationSyntax> : AbstractSnippetProvider<TTypeDeclarationSyntax>
    where TTypeDeclarationSyntax : SyntaxNode
{
    protected abstract SyntaxToken GetTypeDeclarationIdentifier(TTypeDeclarationSyntax node);
    protected abstract Task<TTypeDeclarationSyntax> GenerateTypeDeclarationAsync(Document document, int position, CancellationToken cancellationToken);
    protected abstract Task<TextChange?> GetAccessibilityModifiersChangeAsync(Document document, int position, CancellationToken cancellationToken);

    protected sealed override async Task<ImmutableArray<TextChange>> GenerateSnippetTextChangesAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var typeDeclaration = await GenerateTypeDeclarationAsync(document, position, cancellationToken).ConfigureAwait(false);

        var mainChange = new TextChange(TextSpan.FromBounds(position, position), typeDeclaration.NormalizeWhitespace().ToFullString());
        var accessibilityModifiersChange = await GetAccessibilityModifiersChangeAsync(document, position, cancellationToken).ConfigureAwait(false);

        if (accessibilityModifiersChange.HasValue)
        {
            return [accessibilityModifiersChange.Value, mainChange];
        }

        return [mainChange];
    }

    protected sealed override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(TTypeDeclarationSyntax node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
    {
        var identifier = GetTypeDeclarationIdentifier(node);
        return [new SnippetPlaceholder(identifier.ValueText, identifier.SpanStart)];
    }

    protected static async Task<bool> AreAccessibilityModifiersRequiredAsync(Document document, CancellationToken cancellationToken)
    {
        var options = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        var accessibilityModifiersRequired = options.GetEditorConfigOptionValue(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Never);
        return accessibilityModifiersRequired is AccessibilityModifiersRequired.ForNonInterfaceMembers or AccessibilityModifiersRequired.Always;
    }
}
