// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractTypeSnippetProvider<TTypeDeclarationSyntax>(
    TypeKind typeKind, string defaultPrefix)
    : AbstractSnippetProvider<TTypeDeclarationSyntax>
    where TTypeDeclarationSyntax : SyntaxNode
{
    private readonly TypeKind _typeKind = typeKind;
    private readonly string _defaultPrefix = defaultPrefix;

    protected abstract TTypeDeclarationSyntax TypeDeclaration(string name);
    protected abstract SyntaxToken GetTypeDeclarationIdentifier(TTypeDeclarationSyntax node);
    protected abstract Task<TextChange?> GetAccessibilityModifiersChangeAsync(Document document, int position, CancellationToken cancellationToken);

    protected sealed override async Task<ImmutableArray<TextChange>> GenerateSnippetTextChangesAsync(
        Document document, int position, CancellationToken cancellationToken)
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

    protected sealed override async ValueTask<ImmutableArray<SnippetPlaceholder>> GetPlaceHolderLocationsListAsync(
        Document document, TTypeDeclarationSyntax node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
    {
        var identifier = GetTypeDeclarationIdentifier(node);
        var (prefix, _, _) = await GetNamePartsAsync(document, cancellationToken).ConfigureAwait(false);

        // If the name was changed somehow, place the cursor at the main part of the name.
        return !identifier.Text.StartsWith(prefix)
            ? [new SnippetPlaceholder(identifier.Text, identifier.SpanStart)]
            : [new SnippetPlaceholder(identifier.Text[prefix.Length..], identifier.SpanStart + prefix.Length)];
    }

    protected static async Task<bool> AreAccessibilityModifiersRequiredAsync(Document document, CancellationToken cancellationToken)
    {
        var options = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        var accessibilityModifiersRequired = options.GetEditorConfigOptionValue(CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Never);
        return accessibilityModifiersRequired is AccessibilityModifiersRequired.ForNonInterfaceMembers or AccessibilityModifiersRequired.Always;
    }

    private async Task<TTypeDeclarationSyntax> GenerateTypeDeclarationAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var generator = SyntaxGenerator.GetGenerator(document);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var (prefix, main, suffix) = await GetNamePartsAsync(document, cancellationToken).ConfigureAwait(false);

        var name = NameGenerator.GenerateUniqueName(
            prefix + main + suffix, name => semanticModel.LookupSymbols(position, name: name).IsEmpty);
        return TypeDeclaration(name);
    }

    private async ValueTask<(string prefix, string main, string suffix)> GetNamePartsAsync(
        Document document, CancellationToken cancellationToken)
    {
        var namingStylePreferences = await document.GetNamingStylePreferencesAsync(cancellationToken).ConfigureAwait(false);

        var mainName = "My" + this.Identifier.ToPascalCase();
        foreach (var rule in namingStylePreferences.Rules.NamingRules)
        {
            if (rule.SymbolSpecification.AppliesTo(_typeKind, Modifiers.None, accessibility: null))
                return (rule.NamingStyle.Prefix, mainName, rule.NamingStyle.Suffix);
        }

        return (_defaultPrefix, mainName, "");
    }
}
