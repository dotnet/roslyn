// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

/// <summary>
/// Provides completion for CSX attribute names (props) inside a CSX element's attribute list.
/// Triggers when the cursor is inside a CSX opening element or self-closing element,
/// offering the properties of the component's props type that haven't yet been specified.
/// </summary>
[ExportCompletionProvider(nameof(CsxAttributeCompletionProvider), LanguageNames.CSharp), Shared]
[ExtensionOrder(After = nameof(KeywordCompletionProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CsxAttributeCompletionProvider() : LSPCompletionProvider
{
    internal override string Language => LanguageNames.CSharp;

    // Trigger after space or tab (between attributes)
    public override ImmutableHashSet<char> TriggerCharacters => ImmutableHashSet.Create(' ', '\t');

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        var ch = text[characterPosition];
        // Fire on space/tab (between attributes)
        if (ch is ' ' or '\t')
            return true;
        // Fire on the first letter of a new word (mirrors SymbolCompletionProvider)
        if (options.TriggerOnTypingLetters && CompletionUtilities.IsStartingNewWord(text, characterPosition))
            return true;
        return false;
    }

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var document = context.Document;
        var position = context.Position;
        var cancellationToken = context.CancellationToken;

        // Only active when CsxFactory is set.
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxTree?.Options is not CSharpParseOptions { CsxFactory: not null } parseOptions)
            return;

        // Find the enclosing CSX opening element or self-closing element.
        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(position);

        var openingElement = token.Parent?.AncestorsAndSelf()
            .OfType<CsxOpeningElementSyntax>()
            .FirstOrDefault()
            as CsxNodeSyntax
            ?? token.Parent?.AncestorsAndSelf()
            .OfType<CsxSelfClosingElementSyntax>()
            .FirstOrDefault();

        if (openingElement is null)
            return;

        // Extract tag name and already-specified attributes.
        NameSyntax tagName;
        SyntaxList<CsxAttributeSyntax> existingAttrs;

        if (openingElement is CsxOpeningElementSyntax opening)
        {
            tagName = opening.Name;
            existingAttrs = opening.Attributes;
        }
        else if (openingElement is CsxSelfClosingElementSyntax selfClosing)
        {
            tagName = selfClosing.Name;
            existingAttrs = selfClosing.Attributes;
        }
        else
        {
            return;
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Resolve the component type by name, then find its props from the first parameter
        // of the public static method returning CSX.Element.
        INamedTypeSymbol? propsType = null;

        var tagNameStr = tagName.ToString();
        var lookupPos = tagName.SpanStart;

        var typeSymbol = semanticModel.LookupSymbols(lookupPos, name: tagNameStr)
            .OfType<INamedTypeSymbol>()
            .FirstOrDefault();

        if (typeSymbol is not null)
        {
            var renderMethod = typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.IsStatic
                    && m.DeclaredAccessibility == Accessibility.Public
                    && m.Parameters.Length >= 1);

            propsType = renderMethod?.Parameters[0].Type as INamedTypeSymbol;
        }

        if (propsType is null)
            return;

        // Find the constructor (record primary constructor or any public constructor).
        var constructor = propsType.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();

        if (constructor is null)
            return;

        // Collect already-specified attribute names.
        var specified = new HashSet<string>(
            existingAttrs.Select(a => a.Name.Identifier.Text),
            StringComparer.OrdinalIgnoreCase);

        // Offer each unspecified parameter as a completion item.
        foreach (var param in constructor.Parameters)
        {
            if (specified.Contains(param.Name))
                continue;

            var displayText = param.Name;
            var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var isOptional = param.IsOptional;

            var item = CommonCompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: $"={{{typeName}}}",
                rules: isOptional
                    ? CompletionItemRules.Default
                    : CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect),
                glyph: Glyph.PropertyPublic,
                description: $"{param.Name}: {typeName}".ToSymbolDisplayParts(),
                sortText: isOptional ? $"~{displayText}" : displayText);

            context.AddItem(item);
        }
    }
}
