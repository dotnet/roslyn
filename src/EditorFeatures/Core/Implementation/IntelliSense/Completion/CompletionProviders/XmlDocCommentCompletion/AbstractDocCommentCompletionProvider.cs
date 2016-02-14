// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.CompletionProviders.XmlDocCommentCompletion
{
    internal abstract class AbstractDocCommentCompletionProvider : CompletionListProvider, ICustomCommitCompletionProvider
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

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            if (!context.Options.GetOption(CompletionOptions.ShowXmlDocCommentCompletion))
            {
                return;
            }

            var items = await GetItemsWorkerAsync(context.Document, context.Position, context.TriggerInfo, context.CancellationToken).ConfigureAwait(false);
            if (items != null)
            {
                context.AddItems(items);
            }
        }

        protected abstract Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken);

        protected CompletionItem GetItem(string n, TextSpan span)
        {
            if (_tagMap.ContainsKey(n))
            {
                var value = _tagMap[n];
                return new XmlDocCommentCompletionItem(this, span, n, value[0], value[1], GetCompletionItemRules());
            }

            return new XmlDocCommentCompletionItem(this, span, n, GetCompletionItemRules());
        }

        protected IEnumerable<CompletionItem> GetAttributeItem(string n, TextSpan span)
        {
            var items = _attributeMap.Where(x => x[0] == n).Select(x => new XmlDocCommentCompletionItem(this, span, x[1], x[2], x[3], GetCompletionItemRules()));

            return items.Any() ? items : SpecializedCollections.SingletonEnumerable(new XmlDocCommentCompletionItem(this, span, n, GetCompletionItemRules()));
        }

        protected IEnumerable<CompletionItem> GetAlwaysVisibleItems(TextSpan filterSpan)
        {
            return new[] { SeeTagName, SeeAlsoTagName, CDataPrefixTagName, CommentPrefixTagName }
                .Select(t => GetItem(t, filterSpan));
        }

        protected IEnumerable<CompletionItem> GetNestedTags(TextSpan filterSpan)
        {
            return new[] { CTagName, CodeTagName, ParaTagName, ListTagName, ParamRefTagName, TypeParamRefTagName }
                .Select(t => GetItem(t, filterSpan));
        }

        protected IEnumerable<CompletionItem> GetTopLevelRepeatableItems(TextSpan filterSpan)
        {
            return new[] { ExceptionTagName, IncludeTagName, PermissionTagName }
                .Select(t => GetItem(t, filterSpan));
        }

        protected IEnumerable<CompletionItem> GetListItems(TextSpan span)
        {
            return new[] { ListHeaderTagName, TermTagName, ItemTagName, DescriptionTagName }
                .Select(t => GetItem(t, span));
        }

        protected IEnumerable<CompletionItem> GetListHeaderItems(TextSpan span)
        {
            return new[] { TermTagName, DescriptionTagName }
                .Select(t => GetItem(t, span));
        }

        protected string FormatParameter(string kind, string name)
        {
            return $"{kind} {NameAttributeName}=\"{name}\"";
        }

        public void Commit(CompletionItem completionItem, ITextView textView, ITextBuffer subjectBuffer, ITextSnapshot triggerSnapshot, char? commitChar)
        {
            var item = (XmlDocCommentCompletionItem)completionItem;
            item.Commit(textView, subjectBuffer, triggerSnapshot, commitChar);
        }

        protected abstract AbstractXmlDocCommentCompletionItemRules GetCompletionItemRules();
    }
}
