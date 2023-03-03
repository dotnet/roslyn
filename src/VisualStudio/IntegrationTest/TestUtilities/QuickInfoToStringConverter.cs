// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class QuickInfoToStringConverter
    {
        public static string GetStringFromBulkContent(IEnumerable<object> content)
        {
            return string.Join(Environment.NewLine, content.Select(GetStringFromItem));
        }

        private static string? GetStringFromItem(object item)
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
                case ContainerElement containerElement:
                    string separator;
                    switch (containerElement.Style)
                    {
                        case ContainerElementStyle.Wrapped:
                            separator = "";
                            break;

                        case ContainerElementStyle.Stacked | ContainerElementStyle.VerticalPadding:
                            separator = Environment.NewLine + Environment.NewLine;
                            break;

                        case ContainerElementStyle.Stacked:
                        default:
                            separator = Environment.NewLine;
                            break;
                    }

                    return string.Join(separator, containerElement.Elements.Select(GetStringFromItem));
                case ClassifiedTextElement classifiedTextElement:
                    return string.Join("", classifiedTextElement.Runs.Select(GetStringFromItem));
                case ClassifiedTextRun classifiedTextRun:
                    return classifiedTextRun.Text;
                case ImageElement imageElement:
                    return "";
            }

            return null;
        }

        private static string GetStringFromTextBlock(TextBlock textBlock)
        {
            if (!string.IsNullOrEmpty(textBlock.Text))
                return textBlock.Text;

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
                    var inlineText = GetStringFromInline(inline);
                    if (!string.IsNullOrEmpty(inlineText))
                        sb.Append(inlineText);
                }
            }
        }

        private static string? GetStringFromInline(Inline currentInline)
        {
            if (currentInline is LineBreak)
                return Environment.NewLine;

            var run = currentInline as Run;
            if (run == null)
                return null;

            return run.Text;
        }
    }
}
