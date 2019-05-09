// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    using static DocumentationCommentXmlNames;

    internal abstract class AbstractDocCommentCompletionProvider<TSyntax> : CommonCompletionProvider
        where TSyntax : SyntaxNode
    {
        // Tag names
        private static readonly ImmutableArray<string> s_listTagNames = ImmutableArray.Create(ListHeaderElementName, TermElementName, ItemElementName, DescriptionElementName);
        private static readonly ImmutableArray<string> s_listHeaderTagNames = ImmutableArray.Create(TermElementName, DescriptionElementName);
        private static readonly ImmutableArray<string> s_nestedTagNames = ImmutableArray.Create(CElementName, CodeElementName, ParaElementName, ListElementName);
        private static readonly ImmutableArray<string> s_topLevelRepeatableTagNames = ImmutableArray.Create(ExceptionElementName, IncludeElementName, PermissionElementName);
        private static readonly ImmutableArray<string> s_topLevelSingleUseTagNames = ImmutableArray.Create(SummaryElementName, RemarksElementName, ExampleElementName, CompletionListElementName);

        private static readonly Dictionary<string, (string tagOpen, string textBeforeCaret, string textAfterCaret, string tagClose)> s_tagMap =
            new Dictionary<string, (string tagOpen, string textBeforeCaret, string textAfterCaret, string tagClose)>
            {
                //                                        tagOpen                                  textBeforeCaret       $$  textAfterCaret                            tagClose
                { ExceptionElementName,              ($"<{ExceptionElementName}",              $" {CrefAttributeName}=\"",  "\"",                                      null) },
                { IncludeElementName,                ($"<{IncludeElementName}",                $" {FileAttributeName}=\'", $"\' {PathAttributeName}=\'[@name=\"\"]\'", "/>") },
                { PermissionElementName,             ($"<{PermissionElementName}",             $" {CrefAttributeName}=\"",  "\"",                                      null) },
                { SeeElementName,                    ($"<{SeeElementName}",                    $" {CrefAttributeName}=\"",  "\"",                                      "/>") },
                { SeeAlsoElementName,                ($"<{SeeAlsoElementName}",                $" {CrefAttributeName}=\"",  "\"",                                      "/>") },
                { ListElementName,                   ($"<{ListElementName}",                   $" {TypeAttributeName}=\"",  "\"",                                      null) },
                { ParameterReferenceElementName,     ($"<{ParameterReferenceElementName}",     $" {NameAttributeName}=\"",  "\"",                                      "/>") },
                { TypeParameterReferenceElementName, ($"<{TypeParameterReferenceElementName}", $" {NameAttributeName}=\"",  "\"",                                      "/>") },
                { CompletionListElementName,         ($"<{CompletionListElementName}",         $" {CrefAttributeName}=\"",  "\"",                                      "/>") },
            };

        private static readonly string[][] s_attributeMap =
            new[]
            {
                new[] { ExceptionElementName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { PermissionElementName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { SeeElementName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { SeeElementName, LangwordAttributeName, $"{LangwordAttributeName}=\"", "\"" },
                new[] { SeeAlsoElementName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { ListElementName, TypeAttributeName, $"{TypeAttributeName}=\"", "\"" },
                new[] { ParameterElementName, NameAttributeName, $"{NameAttributeName}=\"", "\"" },
                new[] { ParameterReferenceElementName, NameAttributeName, $"{NameAttributeName}=\"", "\"" },
                new[] { TypeParameterElementName, NameAttributeName, $"{NameAttributeName}=\"", "\"" },
                new[] { TypeParameterReferenceElementName, NameAttributeName, $"{NameAttributeName}=\"", "\"" },
                new[] { IncludeElementName, FileAttributeName, $"{FileAttributeName}=\"", "\"" },
                new[] { IncludeElementName, PathAttributeName, $"{PathAttributeName}=\"", "\"" }
            };

        private static readonly ImmutableArray<string> s_listTypeValues = ImmutableArray.Create("bullet", "number", "table");

        private readonly CompletionItemRules defaultRules;

        protected AbstractDocCommentCompletionProvider(CompletionItemRules defaultRules)
        {
            this.defaultRules = defaultRules ?? throw new ArgumentNullException(nameof(defaultRules)); ;
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            if (!context.Options.GetOption(CompletionControllerOptions.ShowXmlDocCommentCompletion))
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

        protected abstract Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTrigger trigger, CancellationToken cancellationToken);

        protected abstract IEnumerable<string> GetExistingTopLevelElementNames(TSyntax syntax);

        protected abstract IEnumerable<string> GetExistingTopLevelAttributeValues(TSyntax syntax, string tagName, string attributeName);

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

        protected IEnumerable<CompletionItem> GetAttributeItems(string tagName, ISet<string> existingAttributes)
        {
            return s_attributeMap.Where(x => x[0] == tagName && !existingAttributes.Contains(x[1]))
                                 .Select(x => CreateCompletionItem(x[1], x[2], x[3]));
        }

        protected IEnumerable<CompletionItem> GetAlwaysVisibleItems()
        {
            return new[] { GetCDataItem(), GetCommentItem(), GetItem(SeeElementName), GetItem(SeeAlsoElementName) };
        }

        private CompletionItem GetCommentItem()
        {
            const string prefix = "!--";
            const string suffix = "-->";
            return CreateCompletionItem(prefix, "<" + prefix, suffix);
        }

        private CompletionItem GetCDataItem()
        {
            const string prefix = "![CDATA[";
            const string suffix = "]]>";
            return CreateCompletionItem(prefix, "<" + prefix, suffix);
        }

        protected IEnumerable<CompletionItem> GetNestedItems(ISymbol symbol, bool includeKeywords)
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
            var names = symbol.GetParameters().Select(p => p.Name);

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

        protected IEnumerable<CompletionItem> GetAttributeValueItems(ISymbol symbol, string tagName, string attributeName)
        {
            if (attributeName == NameAttributeName && symbol != null)
            {
                if (tagName == ParameterElementName || tagName == ParameterReferenceElementName)
                {
                    return symbol.GetParameters()
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

        protected abstract IEnumerable<string> GetKeywordNames();

        protected IEnumerable<CompletionItem> GetTopLevelItems(ISymbol symbol, TSyntax syntax)
        {
            var items = new List<CompletionItem>();

            var existingTopLevelTags = new HashSet<string>(GetExistingTopLevelElementNames(syntax));

            items.AddRange(s_topLevelSingleUseTagNames.Except(existingTopLevelTags).Select(GetItem));
            items.AddRange(s_topLevelRepeatableTagNames.Select(GetItem));

            if (symbol != null)
            {
                items.AddRange(GetParameterItems(symbol.GetParameters(), syntax, ParameterElementName));
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

            return items;
        }

        protected IEnumerable<CompletionItem> GetItemTagItems()
        {
            return new[] { TermElementName, DescriptionElementName }.Select(GetItem);
        }

        protected IEnumerable<CompletionItem> GetListItems()
        {
            return s_listTagNames.Select(GetItem);
        }

        protected IEnumerable<CompletionItem> GetListHeaderItems()
        {
            return s_listHeaderTagNames.Select(GetItem);
        }

        private IEnumerable<CompletionItem> GetParameterItems<TSymbol>(ImmutableArray<TSymbol> symbols, TSyntax syntax, string tagName) where TSymbol : ISymbol
        {
            var names = symbols.Select(p => p.Name).ToSet();
            names.RemoveAll(GetExistingTopLevelAttributeValues(syntax, tagName, NameAttributeName).WhereNotNull());
            return names.Select(name => CreateCompletionItem(FormatParameter(tagName, name)));
        }

        private string FormatParameter(string kind, string name)
        {
            return $"{kind} {NameAttributeName}=\"{name}\"";
        }

        private string FormatParameterRefTag(string kind, string name)
        {
            return $"<{kind} {NameAttributeName}=\"{name}\"/>";
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitChar = default, CancellationToken cancellationToken = default)
        {
            var includesCommitCharacter = true;

            if (commitChar == ' ' &&
                XmlDocCommentCompletionItem.TryGetInsertionTextOnSpace(item, out var beforeCaretText, out var afterCaretText))
            {
                includesCommitCharacter = false;
            }
            else
            {
                beforeCaretText = XmlDocCommentCompletionItem.GetBeforeCaretText(item);
                afterCaretText = XmlDocCommentCompletionItem.GetAfterCaretText(item);
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var itemSpan = item.Span;
            var replacementSpan = TextSpan.FromBounds(text[itemSpan.Start - 1] == '<' && beforeCaretText[0] == '<' ? itemSpan.Start - 1 : itemSpan.Start, itemSpan.End);

            var replacementText = beforeCaretText;
            var newPosition = replacementSpan.Start + beforeCaretText.Length;

            if (commitChar.HasValue && !char.IsWhiteSpace(commitChar.Value) && commitChar.Value != replacementText[replacementText.Length - 1])
            {
                // include the commit character
                replacementText += commitChar.Value;

                // The caret goes after whatever commit character we spit.
                newPosition++;
            }

            replacementText += afterCaretText;

            return CompletionChange.Create(
                new TextChange(replacementSpan, replacementText),
                newPosition, includesCommitCharacter);
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

        protected CompletionItem CreateCompletionItem(string displayText,
            string beforeCaretText, string afterCaretText,
            string beforeCaretTextOnSpace = null, string afterCaretTextOnSpace = null)
        {
            return XmlDocCommentCompletionItem.Create(
                displayText, beforeCaretText, afterCaretText,
                beforeCaretTextOnSpace, afterCaretTextOnSpace,
                rules: GetCompletionItemRules(displayText));
        }

        private static readonly CharacterSetModificationRule WithoutQuoteRule = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '"');
        private static readonly CharacterSetModificationRule WithoutSpaceRule = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ' ');

        internal static readonly ImmutableArray<CharacterSetModificationRule> FilterRules = ImmutableArray.Create(
            CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, '!', '-', '['));

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
}
