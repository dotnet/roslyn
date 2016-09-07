﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractDocCommentCompletionProvider<TSyntax> : CommonCompletionProvider
        where TSyntax : SyntaxNode
    {
        // Tag names
        protected const string CTagName = "c";
        protected const string CodeTagName = "code";
        protected const string CompletionListTagName = "completionlist";
        protected const string DescriptionTagName = "description";
        protected const string ExampleTagName = "example";
        protected const string ExceptionTagName = "exception";
        protected const string IncludeTagName = "include";
        protected const string ItemTagName = "item";
        protected const string ListTagName = "list";
        protected const string ListHeaderTagName = "listheader";
        protected const string ParaTagName = "para";
        protected const string ParamTagName = "param";
        protected const string ParamRefTagName = "paramref";
        protected const string PermissionTagName = "permission";
        protected const string RemarksTagName = "remarks";
        protected const string ReturnsTagName = "returns";
        protected const string SeeTagName = "see";
        protected const string SeeAlsoTagName = "seealso";
        protected const string SummaryTagName = "summary";
        protected const string TermTagName = "term";
        protected const string TypeParamTagName = "typeparam";
        protected const string TypeParamRefTagName = "typeparamref";
        protected const string ValueTagName = "value";

        // Attribute names
        protected const string CrefAttributeName = "cref";
        protected const string FileAttributeName = "file";
        protected const string LangwordAttributeName = "langword";
        protected const string NameAttributeName = "name";
        protected const string PathAttributeName = "path";
        protected const string TypeAttributeName = "type";

        private readonly Dictionary<string, string[]> _tagMap =
            new Dictionary<string, string[]>
            {
                //                                  Open                         Before caret           $$  After caret                               Close
                { ExceptionTagName,      new[] { $"<{ExceptionTagName}",      $" {CrefAttributeName}=\"",  "\"",                                      null } },
                { IncludeTagName,        new[] { $"<{IncludeTagName}",        $" {FileAttributeName}=\'", $"\' {PathAttributeName}=\'[@name=\"\"]\'", "/>" } },
                { PermissionTagName,     new[] { $"<{PermissionTagName}",     $" {CrefAttributeName}=\"",  "\"",                                      null } },
                { SeeTagName,            new[] { $"<{SeeTagName}",            $" {CrefAttributeName}=\"",  "\"",                                      "/>" } },
                { SeeAlsoTagName,        new[] { $"<{SeeAlsoTagName}",        $" {CrefAttributeName}=\"",  "\"",                                      "/>" } },
                { ListTagName,           new[] { $"<{ListTagName}",           $" {TypeAttributeName}=\"",  "\"",                                      null } },
                { ParamRefTagName,       new[] { $"<{ParamRefTagName}",       $" {NameAttributeName}=\"",  "\"",                                      "/>" } },
                { TypeParamRefTagName,   new[] { $"<{TypeParamRefTagName}",   $" {NameAttributeName}=\"",  "\"",                                      "/>" } },
                { CompletionListTagName, new[] { $"<{CompletionListTagName}", $" {CrefAttributeName}=\"",  "\"",                                      "/>" } },
            };

        private readonly string[][] _attributeMap =
            new[]
            {
                new[] { ExceptionTagName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { PermissionTagName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { SeeTagName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { SeeTagName, LangwordAttributeName, $"{LangwordAttributeName}=\"", "\"" },
                new[] { SeeAlsoTagName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { ListTagName, TypeAttributeName, $"{TypeAttributeName}=\"", "\"" },
                new[] { ParamTagName, NameAttributeName, $"{NameAttributeName}=\"", "\"" },
                new[] { ParamRefTagName, NameAttributeName, $"{NameAttributeName}=\"", "\"" },
                new[] { TypeParamTagName, NameAttributeName, $"{NameAttributeName}=\"", "\"" },
                new[] { TypeParamRefTagName, NameAttributeName, $"{NameAttributeName}=\"", "\"" },
                new[] { IncludeTagName, FileAttributeName, $"{FileAttributeName}=\"", "\"" },
                new[] { IncludeTagName, PathAttributeName, $"{PathAttributeName}=\"", "\"" }
            };

        private readonly ImmutableArray<string> _listTypeValues = ImmutableArray.Create("bullet", "number", "table");

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

        private Boolean HasExistingTopLevelElement(TSyntax syntax, string name)
        {
            return GetExistingTopLevelElementNames(syntax).Contains(name);
        }

        private CompletionItem GetItem(string name)
        {
            string[] values;
            if (_tagMap.TryGetValue(name, out values))
            {
                return CreateCompletionItem(name,
                    beforeCaretText: values[0] + values[1],
                    afterCaretText: values[2] + values[3],
                    beforeCaretTextOnSpace: values[0],
                    afterCaretTextOnSpace: values[3]);
            }

            return CreateCompletionItem(name);
        }

        protected IEnumerable<CompletionItem> GetAttributeItems(string tagName, ISet<string> existingAttributes)
        {
            return _attributeMap.Where(x => x[0] == tagName && !existingAttributes.Contains(x[1]))
                                .Select(x => CreateCompletionItem(x[1], x[2], x[3]));
        }

        protected IEnumerable<CompletionItem> GetAlwaysVisibleItems()
        {
            const string CommentPrefix = "!--";
            const string CommentSuffix = "-->";
            const string CDataPrefix = "![CDATA[";
            const string CDataSuffix = "]]>";

            return new[] {
                GetItem(SeeTagName),
                GetItem(SeeAlsoTagName),
                CreateCompletionItem(CommentPrefix, "<" + CommentPrefix, CommentSuffix),
                CreateCompletionItem(CDataPrefix, "<" + CDataPrefix, CDataSuffix),
            };
        }

        protected IEnumerable<string> NestedTagNames
        {
            get { return new[] { CTagName, CodeTagName, ParaTagName, ListTagName }; }
        }

        protected IEnumerable<CompletionItem> GetNestedTags(ISymbol declaredSymbol)
        {
            return NestedTagNames.Select(GetItem)
                                 .Concat(GetParamRefItems(declaredSymbol))
                                 .Concat(GetTypeParamRefItems(declaredSymbol));
        }

        private IEnumerable<CompletionItem> GetParamRefItems(ISymbol declaredSymbol)
        {
            var parameters = declaredSymbol?.GetParameters().Select(p => p.Name).ToSet();

            if (parameters == null)
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
            }

            return parameters.Select(p => CreateCompletionItem(
                displayText: FormatParameter(ParamRefTagName, p),
                beforeCaretText: FormatParameterRefTag(ParamRefTagName, p),
                afterCaretText: string.Empty));
        }

        private IEnumerable<CompletionItem> GetTypeParamRefItems(ISymbol declaredSymbol)
        {
            var typeParameters = declaredSymbol?.GetTypeParameters().Select(t => t.Name).ToSet();

            if (typeParameters == null)
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
            }

            return typeParameters.Select(t => CreateCompletionItem(
                displayText: FormatParameter(TypeParamRefTagName, t),
                beforeCaretText: FormatParameterRefTag(TypeParamRefTagName, t),
                afterCaretText: string.Empty));
        }

        protected IEnumerable<CompletionItem> GetAttributeValueItems(ISymbol symbol, string tagName, string attributeName)
        {
            if (attributeName == NameAttributeName)
            {
                if (tagName == ParamTagName || tagName == ParamRefTagName)
                {
                    return GetParamNameItems(symbol);
                }
                else if (tagName == TypeParamTagName || tagName == TypeParamRefTagName)
                {
                    return GetTypeParamNameItems(symbol);
                }
            }
            else if (attributeName == LangwordAttributeName && tagName == SeeTagName)
            {
                return GetKeywordNames().Select(keyword => CreateCompletionItem(keyword));
            }
            else if (attributeName == TypeAttributeName && tagName == ListTagName)
            {
                return _listTypeValues.Select(value => CreateCompletionItem(value));
            }

            return null;
        }

        protected IEnumerable<CompletionItem> GetParamNameItems(ISymbol declaredSymbol)
        {
            var items = declaredSymbol?.GetParameters()
                                       .Select(parameter => CreateCompletionItem(parameter.Name));

            return items ?? SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }

        protected IEnumerable<CompletionItem> GetTypeParamNameItems(ISymbol declaredSymbol)
        {
            var items = declaredSymbol?.GetTypeParameters()
                                       .Select(typeParameter => CreateCompletionItem(typeParameter.Name));

            return items ?? SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }

        protected abstract IEnumerable<string> GetKeywordNames();

        protected IEnumerable<CompletionItem> GetTopLevelRepeatableItems()
        {
            return new[] { ExceptionTagName, IncludeTagName, PermissionTagName }.Select(GetItem);
        }

        protected IEnumerable<CompletionItem> GetTopLevelSingleUseItems(TSyntax syntax)
        {
            var tagNames = new HashSet<string>(new[] { SummaryTagName, RemarksTagName, ExampleTagName, CompletionListTagName });
            tagNames.RemoveAll(GetExistingTopLevelElementNames(syntax).WhereNotNull());
            return tagNames.Select(GetItem);
        }

        protected IEnumerable<CompletionItem> GetListItems()
        {
            return new[] { ListHeaderTagName, TermTagName, ItemTagName, DescriptionTagName }.Select(GetItem);
        }

        protected IEnumerable<CompletionItem> GetListHeaderItems()
        {
            return new[] { TermTagName, DescriptionTagName }.Select(GetItem);
        }

        protected IEnumerable<CompletionItem> GetItemsForSymbol(ISymbol symbol, TSyntax syntax)
        {
            var items = new List<CompletionItem>();

            if (symbol != null)
            {
                items.AddRange(GetParameterItems(symbol.GetParameters(), syntax, ParamTagName));
                items.AddRange(GetParameterItems(symbol.GetTypeParameters(), syntax, TypeParamTagName));

                var property = symbol as IPropertySymbol;
                if (property != null && !HasExistingTopLevelElement(syntax, ValueTagName))
                {
                    items.Add(GetItem(ValueTagName));
                }

                var method = symbol as IMethodSymbol;
                var returns = ((property != null && !property.IsWriteOnly) || (method != null && !method.ReturnsVoid));
                if (returns && !HasExistingTopLevelElement(syntax, ReturnsTagName))
                {
                    items.Add(GetItem(ReturnsTagName));
                }
            }

            return items;
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

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitChar = default(char?), CancellationToken cancellationToken = default(CancellationToken))
        {
            bool includesCommitCharacter = true;

            string beforeCaretText, afterCaretText;
            if (commitChar == ' ' && XmlDocCommentCompletionItem.TryGetInsertionTextOnSpace(item, out beforeCaretText, out afterCaretText))
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

        protected CompletionItem CreateCompletionItem(string displayText,
            string beforeCaretText, string afterCaretText,
            string beforeCaretTextOnSpace = null, string afterCaretTextOnSpace = null)
        {
            return XmlDocCommentCompletionItem.Create(
                displayText, beforeCaretText, afterCaretText,
                beforeCaretTextOnSpace, afterCaretTextOnSpace,
                rules: GetCompletionItemRules(displayText));
        }

        internal static readonly CharacterSetModificationRule WithoutQuoteRule = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '"');
        internal static readonly CharacterSetModificationRule WithoutSpaceRule = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ' ');

        internal static readonly ImmutableArray<CharacterSetModificationRule> FilterRules = ImmutableArray.Create(
            CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, '!', '-', '['));

        protected abstract CompletionItemRules GetCompletionItemRules(string displayText);
    }
}
