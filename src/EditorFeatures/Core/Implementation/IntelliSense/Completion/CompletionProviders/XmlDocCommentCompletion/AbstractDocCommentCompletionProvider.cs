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
        private readonly Dictionary<string, string[]> _tagMap =
            new Dictionary<string, string[]>
            {
                { "exception", new[] { "<exception cref=\"", "\"" } },
                { "!--", new[] { "<!--", "-->" } },
                { "![CDATA[", new[] { "<![CDATA[", "]]>" } },
                { "include", new[] { "<include file=\'", "\' path=\'[@name=\"\"]\'/>" } },
                { "permission", new[] { "<permission cref=\"", "\"" } },
                { "see", new[] { "<see cref=\"", "\"/>" } },
                { "seealso", new[] { "<seealso cref=\"", "\"/>" } },
                { "list", new[] { "<list type=\"", "\"" } },
                { "paramref", new[] { "<paramref name=\"", "\"/>" } },
                { "typeparamref", new[] { "<typeparamref name=\"", "\"/>" } },
                { "completionlist", new[] { "<completionlist cref=\"", "\"/>" } },
            };

        private readonly string[][] _attributeMap =
            new[]
            {
                new[] { "exception", "cref", "cref=\"", "\"" },
                new[] { "permission",  "cref", "cref=\"", "\"" },
                new[] { "see", "cref", "cref=\"", "\"" },
                new[] { "seealso", "cref", "cref=\"", "\"" },
                new[] { "list", "type", "type=\"", "\"" },
                new[] { "param", "name", "name=\"", "\"" },
                new[] { "include", "file", "file=\"", "\"" },
                new[] { "include", "path", "path=\"", "\"" }
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
            return new[] { "see", "seealso", "![CDATA[", "!--" }
                .Select(t => GetItem(t, filterSpan));
        }

        protected IEnumerable<CompletionItem> GetNestedTags(TextSpan filterSpan)
        {
            return new[] { "c", "code", "para", "list", "paramref", "typeparamref" }
                .Select(t => GetItem(t, filterSpan));
        }

        protected IEnumerable<CompletionItem> GetTopLevelRepeatableItems(TextSpan filterSpan)
        {
            return new[] { "exception", "include", "permission" }
                .Select(t => GetItem(t, filterSpan));
        }

        protected IEnumerable<CompletionItem> GetListItems(TextSpan span)
        {
            return new[] { "listheader", "term", "item", "description" }
                .Select(t => GetItem(t, span));
        }

        protected IEnumerable<CompletionItem> GetListHeaderItems(TextSpan span)
        {
            return new[] { "term", "description" }
                .Select(t => GetItem(t, span));
        }

        protected string FormatParameter(string kind, string name)
        {
            return string.Format("{0} name=\"{1}\"", kind, name);
        }

        public void Commit(CompletionItem completionItem, ITextView textView, ITextBuffer subjectBuffer, ITextSnapshot triggerSnapshot, char? commitChar)
        {
            var item = (XmlDocCommentCompletionItem)completionItem;
            item.Commit(textView, subjectBuffer, triggerSnapshot, commitChar);
        }

        protected abstract AbstractXmlDocCommentCompletionItemRules GetCompletionItemRules();
    }
}
