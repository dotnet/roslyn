// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

using static DocumentationCommentXmlNames;

internal abstract class AbstractDocCommentCompletionProvider<TSyntax> : LSPCompletionProvider
    where TSyntax : SyntaxNode
{
    // Tag names
    private static readonly ImmutableArray<string> s_listTagNames = [ListHeaderElementName, TermElementName, ItemElementName, DescriptionElementName];
    private static readonly ImmutableArray<string> s_listHeaderTagNames = [TermElementName, DescriptionElementName];
    private static readonly ImmutableArray<string> s_nestedTagNames = [CElementName, CodeElementName, ParaElementName, ListElementName];
    private static readonly ImmutableArray<string> s_topLevelRepeatableTagNames = [ExceptionElementName, IncludeElementName, PermissionElementName];
    private static readonly ImmutableArray<string> s_topLevelSingleUseTagNames = [SummaryElementName, RemarksElementName, ExampleElementName, CompletionListElementName];

    private static readonly Dictionary<string, (string tagOpen, string textBeforeCaret, string textAfterCaret, string? tagClose)> s_tagMap =
        new Dictionary<string, (string tagOpen, string textBeforeCaret, string textAfterCaret, string? tagClose)>()
        {
            //                                        tagOpen                                  textBeforeCaret       $$  textAfterCaret                            tagClose
            { ExceptionElementName,              ($"<{ExceptionElementName}",              $" {CrefAttributeName}=\"",  "\"",                                      null) },
            { IncludeElementName,                ($"<{IncludeElementName}",                $" {FileAttributeName}=\'", $"\' {PathAttributeName}=\'[@name=\"\"]\'", "/>") },
            { InheritdocElementName,             ($"<{InheritdocElementName}",             $"",                         "",                                        "/>") },
            { PermissionElementName,             ($"<{PermissionElementName}",             $" {CrefAttributeName}=\"",  "\"",                                      null) },
            { SeeElementName,                    ($"<{SeeElementName}",                    $" {CrefAttributeName}=\"",  "\"",                                      "/>") },
            { SeeAlsoElementName,                ($"<{SeeAlsoElementName}",                $" {CrefAttributeName}=\"",  "\"",                                      "/>") },
            { ListElementName,                   ($"<{ListElementName}",                   $" {TypeAttributeName}=\"",  "\"",                                      null) },
            { ParameterReferenceElementName,     ($"<{ParameterReferenceElementName}",     $" {NameAttributeName}=\"",  "\"",                                      "/>") },
            { TypeParameterReferenceElementName, ($"<{TypeParameterReferenceElementName}", $" {NameAttributeName}=\"",  "\"",                                      "/>") },
            { CompletionListElementName,         ($"<{CompletionListElementName}",         $" {CrefAttributeName}=\"",  "\"",                                      "/>") },
        };

    private static readonly ImmutableArray<(string elementName, string attributeName, string text)> s_attributeMap =
        [
            (ExceptionElementName, CrefAttributeName, $"{CrefAttributeName}=\""),
            (PermissionElementName, CrefAttributeName, $"{CrefAttributeName}=\""),
            (SeeElementName, CrefAttributeName, $"{CrefAttributeName}=\""),
            (SeeElementName, LangwordAttributeName, $"{LangwordAttributeName}=\""),
            (SeeElementName, HrefAttributeName, $"{HrefAttributeName}=\""),
            (SeeAlsoElementName, CrefAttributeName, $"{CrefAttributeName}=\""),
            (SeeAlsoElementName, HrefAttributeName, $"{HrefAttributeName}=\""),
            (ListElementName, TypeAttributeName, $"{TypeAttributeName}=\""),
            (ParameterElementName, NameAttributeName, $"{NameAttributeName}=\""),
            (ParameterReferenceElementName, NameAttributeName, $"{NameAttributeName}=\""),
            (TypeParameterElementName, NameAttributeName, $"{NameAttributeName}=\""),
            (TypeParameterReferenceElementName, NameAttributeName, $"{NameAttributeName}=\""),
            (IncludeElementName, FileAttributeName, $"{FileAttributeName}=\""),
            (IncludeElementName, PathAttributeName, $"{PathAttributeName}=\""),
            (InheritdocElementName, CrefAttributeName, $"{CrefAttributeName}=\""),
            (InheritdocElementName, PathAttributeName, $"{PathAttributeName}=\""),
        ];

    private static readonly ImmutableArray<string> s_listTypeValues = ["bullet", "number", "table"];

    private readonly CompletionItemRules defaultRules;

    protected AbstractDocCommentCompletionProvider(CompletionItemRules defaultRules)
    {
        this.defaultRules = defaultRules ?? throw new ArgumentNullException(nameof(defaultRules));
    }

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        if (!context.CompletionOptions.ShowXmlDocCommentCompletion)
        {
            return;
        }

        var items = await GetItemsWorkerAsync(
            context.Document, context.Position, context.Trigger, context.CancellationToken).ConfigureAwait(false);

        if (items != null)
        {
            context.AddItems(items);
        }
    }

    protected abstract Task<IEnumerable<CompletionItem>?> GetItemsWorkerAsync(Document document, int position, CompletionTrigger trigger, CancellationToken cancellationToken);

    protected abstract IEnumerable<string> GetExistingTopLevelElementNames(TSyntax syntax);

    protected abstract IEnumerable<string?> GetExistingTopLevelAttributeValues(TSyntax syntax, string tagName, string attributeName);

    protected abstract ImmutableArray<string> GetKeywordNames();

    /// <summary>
    /// A temporarily hack that should be removed once/if https://github.com/dotnet/roslyn/issues/53092 is fixed.
    /// </summary>
    protected abstract ImmutableArray<IParameterSymbol> GetParameters(ISymbol symbol);

    private CompletionItem GetItem(string name)
    {
        if (s_tagMap.TryGetValue(name, out var values))
        {
            return CreateCompletionItem(name,
                beforeCaretText: values.tagOpen + values.textBeforeCaret,
                afterCaretText: values.textAfterCaret + values.tagClose);
        }

        return CreateCompletionItem(name);
    }

    protected IEnumerable<CompletionItem> GetAttributeItems(string tagName, ISet<string> existingAttributes, bool addEqualsAndQuotes)
    {
        return s_attributeMap
            .Where(x => x.elementName == tagName && !existingAttributes.Contains(x.attributeName))
            .Select(x => CreateCompletionItem(
                x.attributeName,
                beforeCaretText: addEqualsAndQuotes ? x.text : x.text[..^2],
                afterCaretText: addEqualsAndQuotes ? "\"" : ""));
    }

    protected IEnumerable<CompletionItem> GetAlwaysVisibleItems()
        => [GetCDataItem(), GetCommentItem(), GetItem(InheritdocElementName), GetItem(SeeElementName), GetItem(SeeAlsoElementName)];

    private CompletionItem GetCommentItem()
    {
        const string prefix = "!--";
        const string suffix = "-->";
        return CreateCompletionItem(prefix, beforeCaretText: "<" + prefix, afterCaretText: suffix);
    }

    private CompletionItem GetCDataItem()
    {
        const string prefix = "![CDATA[";
        const string suffix = "]]>";
        return CreateCompletionItem(prefix, beforeCaretText: "<" + prefix, afterCaretText: suffix);
    }

    protected IEnumerable<CompletionItem> GetNestedItems(ISymbol? symbol, bool includeKeywords)
    {
        var items = s_nestedTagNames.Select(GetItem);

        if (symbol != null)
        {
            items = items.Concat(GetParamRefItems(symbol))
                         .Concat(GetTypeParamRefItems(symbol));
        }

        if (includeKeywords)
        {
            items = items.Concat(GetKeywordNames().Select(CreateLangwordCompletionItem));
        }

        return items;
    }

    private IEnumerable<CompletionItem> GetParamRefItems(ISymbol symbol)
    {
        var names = GetParameters(symbol).Select(p => p.Name);

        return names.Select(p => CreateCompletionItem(
            displayText: FormatParameter(ParameterReferenceElementName, p),
            beforeCaretText: FormatParameterRefTag(ParameterReferenceElementName, p),
            afterCaretText: string.Empty));
    }

    private IEnumerable<CompletionItem> GetTypeParamRefItems(ISymbol symbol)
    {
        var names = symbol.GetAllTypeParameters().Select(t => t.Name);

        return names.Select(t => CreateCompletionItem(
            displayText: FormatParameter(TypeParameterReferenceElementName, t),
            beforeCaretText: FormatParameterRefTag(TypeParameterReferenceElementName, t),
            afterCaretText: string.Empty));
    }

    protected IEnumerable<CompletionItem> GetAttributeValueItems(ISymbol? symbol, string tagName, string attributeName)
    {
        if (attributeName == NameAttributeName && symbol != null)
        {
            if (tagName is ParameterElementName or ParameterReferenceElementName)
            {
                return GetParameters(symbol)
                             .Select(parameter => CreateCompletionItem(parameter.Name));
            }
            else if (tagName == TypeParameterElementName)
            {
                return symbol.GetTypeParameters()
                             .Select(typeParameter => CreateCompletionItem(typeParameter.Name));
            }
            else if (tagName == TypeParameterReferenceElementName)
            {
                return symbol.GetAllTypeParameters()
                             .Select(typeParameter => CreateCompletionItem(typeParameter.Name));
            }
        }
        else if (attributeName == LangwordAttributeName && tagName == SeeElementName)
        {
            return GetKeywordNames().Select(CreateCompletionItem);
        }
        else if (attributeName == TypeAttributeName && tagName == ListElementName)
        {
            return s_listTypeValues.Select(CreateCompletionItem);
        }

        return SpecializedCollections.EmptyEnumerable<CompletionItem>();
    }

    protected ImmutableArray<CompletionItem> GetTopLevelItems(ISymbol? symbol, TSyntax syntax)
    {
        using var _1 = ArrayBuilder<CompletionItem>.GetInstance(out var items);
        using var _2 = PooledHashSet<string>.GetInstance(out var existingTopLevelTags);

        existingTopLevelTags.AddAll(GetExistingTopLevelElementNames(syntax));

        items.AddRange(s_topLevelSingleUseTagNames.Except(existingTopLevelTags).Select(GetItem));
        items.AddRange(s_topLevelRepeatableTagNames.Select(GetItem));

        if (symbol != null)
        {
            items.AddRange(GetParameterItems(GetParameters(symbol), syntax, ParameterElementName));
            items.AddRange(GetParameterItems(symbol.GetTypeParameters(), syntax, TypeParameterElementName));

            if (symbol is IPropertySymbol && !existingTopLevelTags.Contains(ValueElementName))
            {
                items.Add(GetItem(ValueElementName));
            }

            var returns = symbol is IMethodSymbol method && !method.ReturnsVoid;
            if (returns && !existingTopLevelTags.Contains(ReturnsElementName))
            {
                items.Add(GetItem(ReturnsElementName));
            }

            if (symbol is INamedTypeSymbol namedType && namedType.IsDelegateType())
            {
                var delegateInvokeMethod = namedType.DelegateInvokeMethod;
                if (delegateInvokeMethod != null)
                {
                    items.AddRange(GetParameterItems(delegateInvokeMethod.GetParameters(), syntax, ParameterElementName));
                }
            }
        }

        return items.ToImmutable();
    }

    protected IEnumerable<CompletionItem> GetItemTagItems()
        => new[] { TermElementName, DescriptionElementName }.Select(GetItem);

    protected IEnumerable<CompletionItem> GetListItems()
        => s_listTagNames.Select(GetItem);

    protected IEnumerable<CompletionItem> GetListHeaderItems()
        => s_listHeaderTagNames.Select(GetItem);

    private IEnumerable<CompletionItem> GetParameterItems<TSymbol>(ImmutableArray<TSymbol> symbols, TSyntax syntax, string tagName) where TSymbol : ISymbol
    {
        var names = symbols.Select(p => p.Name).ToSet();
        names.RemoveAll(GetExistingTopLevelAttributeValues(syntax, tagName, NameAttributeName).WhereNotNull());
        return names.Select(name => CreateCompletionItem(FormatParameter(tagName, name)));
    }

    private static string FormatParameter(string kind, string name)
        => $"{kind} {NameAttributeName}=\"{name}\"";

    private static string FormatParameterRefTag(string kind, string name)
        => $"<{kind} {NameAttributeName}=\"{name}\"/>";

    public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitChar = null, CancellationToken cancellationToken = default)
    {
        var beforeCaretText = XmlDocCommentCompletionItem.GetBeforeCaretText(item);
        var afterCaretText = XmlDocCommentCompletionItem.GetAfterCaretText(item);
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        var itemSpan = item.Span;
        var replacementSpan = TextSpan.FromBounds(text[itemSpan.Start - 1] == '<' && beforeCaretText[0] == '<' ? itemSpan.Start - 1 : itemSpan.Start, itemSpan.End);

        var replacementText = beforeCaretText;
        var newPosition = replacementSpan.Start + beforeCaretText.Length;

        if (text.Length > replacementSpan.End + 1
            && text[replacementSpan.End] == '='
            && text[replacementSpan.End + 1] == '"')
        {
            newPosition += 2;
        }

        if (commitChar.HasValue && !char.IsWhiteSpace(commitChar.Value) && commitChar.Value != replacementText[^1])
        {
            // include the commit character
            replacementText += commitChar.Value;

            // The caret goes after whatever commit character we spit.
            newPosition++;
        }

        replacementText += afterCaretText;

        return CompletionChange.Create(
            new TextChange(replacementSpan, replacementText),
            newPosition, includesCommitCharacter: true);
    }

    private CompletionItem CreateCompletionItem(string displayText)
    {
        return CreateCompletionItem(
            displayText: displayText,
            beforeCaretText: displayText,
            afterCaretText: string.Empty);
    }

    private CompletionItem CreateLangwordCompletionItem(string displayText)
    {
        return CreateCompletionItem(
            displayText: displayText,
            beforeCaretText: "<see langword=\"" + displayText + "\"/>",
            afterCaretText: string.Empty);
    }

    protected CompletionItem CreateCompletionItem(string displayText, string beforeCaretText, string afterCaretText)
        => XmlDocCommentCompletionItem.Create(displayText, beforeCaretText, afterCaretText, rules: GetCompletionItemRules(displayText));

    private static readonly CharacterSetModificationRule WithoutQuoteRule = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '"');
    private static readonly CharacterSetModificationRule WithoutSpaceRule = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ' ');

    protected static readonly ImmutableArray<CharacterSetModificationRule> FilterRules = [CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, '!', '-', '[')];

    private CompletionItemRules GetCompletionItemRules(string displayText)
    {
        var commitRules = defaultRules.CommitCharacterRules;

        if (displayText.Contains("\""))
        {
            commitRules = commitRules.Add(WithoutQuoteRule);
        }

        if (displayText.Contains(" "))
        {
            commitRules = commitRules.Add(WithoutSpaceRule);
        }

        return defaultRules.WithCommitCharacterRules(commitRules);
    }
}
