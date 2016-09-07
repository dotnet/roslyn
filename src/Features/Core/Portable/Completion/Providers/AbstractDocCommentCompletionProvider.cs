// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractDocCommentCompletionProvider : CommonCompletionProvider
    {
        // Tag names
        protected const string CDataPrefixTagName = "![CDATA[";
        protected const string CTagName = "c";
        protected const string CodeTagName = "code";
        protected const string CommentPrefixTagName = "!--";
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
        protected const string NameAttributeName = "name";
        protected const string PathAttributeName = "path";
        protected const string TypeAttributeName = "type";

        private readonly Dictionary<string, string[]> _tagMap =
            new Dictionary<string, string[]>
            {
                { ExceptionTagName,      new[] { $"<{ExceptionTagName} {CrefAttributeName}=\"",      "\"" } },
                { CommentPrefixTagName,  new[] { $"<{CommentPrefixTagName}",                         "-->" } },
                { CDataPrefixTagName,    new[] { $"<{CDataPrefixTagName}",                           "]]>" } },
                { IncludeTagName,        new[] { $"<{IncludeTagName} {FileAttributeName}=\'",        $"\' {PathAttributeName}=\'[@name=\"\"]\'/>" } },
                { PermissionTagName,     new[] { $"<{PermissionTagName} {CrefAttributeName}=\"",     "\"" } },
                { SeeTagName,            new[] { $"<{SeeTagName} {CrefAttributeName}=\"",            "\"/>" } },
                { SeeAlsoTagName,        new[] { $"<{SeeAlsoTagName} {CrefAttributeName}=\"",        "\"/>" } },
                { ListTagName,           new[] { $"<{ListTagName} {TypeAttributeName}=\"",           "\"" } },
                { ParamRefTagName,       new[] { $"<{ParamRefTagName} {NameAttributeName}=\"",       "\"/>" } },
                { TypeParamRefTagName,   new[] { $"<{TypeParamRefTagName} {NameAttributeName}=\"",   "\"/>" } },
                { CompletionListTagName, new[] { $"<{CompletionListTagName} {CrefAttributeName}=\"", "\"/>" } },
            };

        private readonly string[][] _attributeMap =
            new[]
            {
                new[] { ExceptionTagName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { PermissionTagName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { SeeTagName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { SeeAlsoTagName, CrefAttributeName, $"{CrefAttributeName}=\"", "\"" },
                new[] { ListTagName, TypeAttributeName, $"{TypeAttributeName}=\"", "\"" },
                new[] { ParamTagName, NameAttributeName, $"{NameAttributeName}=\"", "\"" },
                new[] { IncludeTagName, FileAttributeName, $"{FileAttributeName}=\"", "\"" },
                new[] { IncludeTagName, PathAttributeName, $"{PathAttributeName}=\"", "\"" }
            };

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

        protected CompletionItem GetItem(string n)
        {
            if (_tagMap.ContainsKey(n))
            {
                var value = _tagMap[n];
                return CreateCompletionItem(n, value[0], value[1]);
            }

            return CreateCompletionItem(n);
        }

        protected IEnumerable<CompletionItem> GetAttributeItem(string n)
        {
            var items = _attributeMap.Where(x => x[0] == n).Select(x => CreateCompletionItem(x[1], x[2], x[3]));

            return items.Any() ? items : SpecializedCollections.SingletonEnumerable(CreateCompletionItem(n));
        }

        protected IEnumerable<CompletionItem> GetAlwaysVisibleItems()
        {
            return new[] { SeeTagName, SeeAlsoTagName, CDataPrefixTagName, CommentPrefixTagName }.Select(GetItem);
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

        protected IEnumerable<CompletionItem> GetTopLevelRepeatableItems()
        {
            return new[] { ExceptionTagName, IncludeTagName, PermissionTagName }.Select(GetItem);
        }

        protected IEnumerable<CompletionItem> GetListItems()
        {
            return new[] { ListHeaderTagName, TermTagName, ItemTagName, DescriptionTagName }.Select(GetItem);
        }

        protected IEnumerable<CompletionItem> GetListHeaderItems()
        {
            return new[] { TermTagName, DescriptionTagName }.Select(GetItem);
        }

        protected string FormatParameter(string kind, string name)
        {
            return $"{kind} {NameAttributeName}=\"{name}\"";
        }

        private string FormatParameterRefTag(string kind, string name)
        {
            return $"<{kind} {NameAttributeName}=\"{name}\"/>";
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitChar = default(char?), CancellationToken cancellationToken = default(CancellationToken))
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var beforeCaretText = XmlDocCommentCompletionItem.GetBeforeCaretText(item);
            var afterCaretText = XmlDocCommentCompletionItem.GetAfterCaretText(item);

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
                newPosition, includesCommitCharacter: true);
        }

        private TextSpan ComputeReplacementSpan(CompletionItem completionItem, SourceText text)
        {
            var currentSpan = completionItem.Span;
            var beforeCaretText = XmlDocCommentCompletionItem.GetBeforeCaretText(completionItem);
            return TextSpan.FromBounds(text[currentSpan.Start - 1] == '<' && beforeCaretText[0] == '<' ? currentSpan.Start - 1 : currentSpan.Start, currentSpan.End);
        }

        protected CompletionItem CreateCompletionItem(string displayText)
        {
            return CreateCompletionItem(displayText, displayText, string.Empty);
        }

        protected CompletionItem CreateCompletionItem(string displayText, string beforeCaretText, string afterCaretText)
        {
            return XmlDocCommentCompletionItem.Create(
                displayText, beforeCaretText, afterCaretText, 
                rules: GetCompletionItemRules(displayText));
        }

        internal static readonly CharacterSetModificationRule WithoutQuoteRule = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '"');
        internal static readonly CharacterSetModificationRule WithoutSpaceRule = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ' ');

        internal static readonly ImmutableArray<CharacterSetModificationRule> FilterRules = ImmutableArray.Create(
            CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, '!', '-', '['));

        protected abstract CompletionItemRules GetCompletionItemRules(string displayText);
    }
}
