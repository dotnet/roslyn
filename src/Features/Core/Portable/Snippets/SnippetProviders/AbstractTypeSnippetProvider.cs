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

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractTypeSnippetProvider : AbstractSnippetProvider
{
    protected abstract void GetTypeDeclarationIdentifier(SyntaxNode node, out SyntaxToken identifier);
    protected abstract Task<SyntaxNode> GenerateTypeDeclarationAsync(Document document, int position, CancellationToken cancellationToken);
    protected abstract Task<TextChange?> GetAccessibilityModifiersChangeAsync(Document document, int position, CancellationToken cancellationToken);

    protected override async Task<ImmutableArray<TextChange>> GenerateSnippetTextChangesAsync(Document document, int position, CancellationToken cancellationToken)
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

    protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
        GetTypeDeclarationIdentifier(node, out var identifier);
        arrayBuilder.Add(new SnippetPlaceholder(identifier.ValueText, identifier.SpanStart));

        return arrayBuilder.ToImmutableArray();
    }

    protected static async Task<bool> AreAccessibilityModifiersRequiredAsync(Document document, CancellationToken cancellationToken)
    {
        var options = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        var accessibilityModifiersRequired = options.GetEditorConfigOptionValue(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Never);
        return accessibilityModifiersRequired is AccessibilityModifiersRequired.ForNonInterfaceMembers or AccessibilityModifiersRequired.Always;
    }
}
