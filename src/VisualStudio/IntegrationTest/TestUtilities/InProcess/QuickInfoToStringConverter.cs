using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    public static class QuickInfoToStringConverter
    {
        public static string GetStringFromBulkContent(IEnumerable<object> content)
        {
            return string.Join(Environment.NewLine, content.Select(item => GetStringFromItem(item) ?? string.Empty));
        }

        private static string GetStringFromItem(object item)
        {
            switch (item)
            {
                case StackPanel displayPanel:
                    return displayPanel.ToString();
                case string itemString:
                    return itemString;
                case TextBlock textBlock:
                    return GetStringFromTextBlock(textBlock);
                case ITextBuffer textBuffer:
                    return textBuffer.CurrentSnapshot.GetText();
            }

            return null;
        }

        private static string GetStringFromTextBlock(TextBlock textBlock)
        {
            if (!string.IsNullOrEmpty(textBlock.Text))
            {
                return textBlock.Text;
            }

            var sb = new StringBuilder();
            BuildStringFromInlineCollection(textBlock.Inlines, sb);
            return sb.ToString();
        }

        private static void BuildStringFromInlineCollection(InlineCollection inlines, StringBuilder sb)
        {
            foreach (var inline in inlines)
            {
                if (inline != null)
                {
                    string inlineText = GetStringFromInline(inline);
                    if (!string.IsNullOrEmpty(inlineText))
                    {
                        sb.Append(inlineText);
                    }
                }
            }
        }

        private static string GetStringFromInline(Inline currentInline)
        {
            if (currentInline is LineBreak lineBreak)
            {
                return Environment.NewLine;
            }

            var run = currentInline as Run;
            if (run == null)
            {
                return null;
            }

            return run.Text;
        }
    }
}
