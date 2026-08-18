// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET
using System;
#endif
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class DirectiveAttributeTransitionCompletionItemProvider(IClientCapabilitiesService clientCapabilitiesService) : DirectiveAttributeCompletionItemProviderBase
{
    private const string DisplayText = "@...";
    private static readonly DirectiveCompletionDescription s_descriptionInfo = new(SR.Blazor_directive_attributes);

    private RazorCompletionItem? _transitionCompletionItem;

    public RazorCompletionItem TransitionCompletionItem
        => _transitionCompletionItem ??= RazorCompletionItem.CreateDirective(
            displayText: DisplayText,
            insertText: "@",
            sortText: null,
            descriptionInfo: s_descriptionInfo,

            // We specify these three commit characters to work around a Visual Studio interaction where
            // completion items that get "soft selected" will cause completion to re-trigger if a user
            // types one of the soft-selected completion item's commit characters.
            // In practice this happens in the `<button |` scenario where the "space" results in completions
            // where this directive attribute transition character ("@...") gets provided and then typing
            // `@` should re-trigger OR typing `/` should re-trigger.
            // However, in VS Code explicit commit characters like these cause issues, e.g. "@..." gets committed when trying to type "/" in a
            // self-closing tag. So in VS Code we have SupportSoftSelectionInCompletion set to false and we will
            // use empty commit character set in that case.
            commitCharacters: _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions ? RazorCommitCharacter.CreateArray(["@", "/", ">"]) : [],
            isSnippet: false);

    public static bool IsTransitionCompletionItem(RazorCompletionItem completionItem)
    {
        return completionItem.Kind == RazorCompletionItemKind.Directive && completionItem.DescriptionInfo == s_descriptionInfo && completionItem.DisplayText == DisplayText;
    }

    private ImmutableArray<RazorCompletionItem>? _completions;

    private ImmutableArray<RazorCompletionItem> Completions => _completions ??= [TransitionCompletionItem];

    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;

    public override ImmutableArray<RazorCompletionItem> GetCompletionItems(RazorCompletionContext context)
    {
        if (!context.SyntaxTree.Options.FileKind.IsComponent())
        {
            // Directive attributes are only supported in components
            return [];
        }

        var owner = context.Owner;
        if (owner is null)
        {
            return [];
        }

        var attribute = owner.Parent;
        if (attribute is MarkupMiscAttributeContentSyntax && attribute.ContainsOnlyWhitespace())
        {
            // This represents a tag when there's no attribute content <InputText | />.
            return Completions;
        }

        if (!TryGetAttributeInfo(owner, out var prefixLocation, out var attributeName, out var attributeNameLocation, out _, out _))
        {
            return [];
        }

        if (attributeNameLocation.IntersectsWith(context.AbsoluteIndex) && attributeName.StartsWith('@'))
        {
            // The transition is already provided for the attribute name
            return [];
        }

        if (!IsValidCompletionPoint(context.AbsoluteIndex, prefixLocation, attributeNameLocation))
        {
            // Not operating in the attribute name area
            return [];
        }

        // This represents a tag when there's no attribute content <InputText | />.
        return Completions;
    }

    // Internal for testing
    internal static bool IsValidCompletionPoint(int absoluteIndex, TextSpan? prefixLocation, TextSpan attributeNameLocation)
    {
        if (absoluteIndex == (prefixLocation?.Start ?? -1))
        {
            // <input| class="test" />
            // Starts of prefix locations belong to the previous SyntaxNode. It could be the end of an attribute value, the tag name, C# etc.
            return false;
        }

        if (attributeNameLocation.Start == absoluteIndex)
        {
            // <input |class="test" />
            return false;
        }

        if (prefixLocation?.IntersectsWith(absoluteIndex) ?? false)
        {
            // <input   |  class="test" />
            return true;
        }

        if (attributeNameLocation.IntersectsWith(absoluteIndex))
        {
            // <input cla|ss="test" />
            return false;
        }

        return false;
    }
}
