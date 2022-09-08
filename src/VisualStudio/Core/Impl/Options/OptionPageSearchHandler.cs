// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Windows;
using System;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class OptionPageSearchHandler
    {
        public static readonly Brush HighlightForeground = SystemColors.HighlightTextBrush;
        public static readonly Brush HighlightBackground = SystemColors.HighlightBrush;

        private readonly ContentControl _control;
        private readonly string _originalContent;
        private readonly int _accessKeyIndex;
        private readonly string _content;

        public OptionPageSearchHandler(ContentControl control, string originalContent)
        {
            _control = control;
            _originalContent = originalContent;

            // Currently only support one access key, and no underscores
            Debug.Assert(_originalContent.Split('_').Length <= 2);

            _accessKeyIndex = _originalContent.IndexOf('_');

            // We strip out the access key so it doesn't interupt search, and because we have to handle displaying it ourselves anyway.
            // Since we strip it out, we also don't need to worry about the access key being the character after the underscore
            _content = _originalContent.Replace("_", "");
        }

        public bool TryHighlightSearchString(string searchTerm)
        {
            var index = _content.IndexOf(searchTerm, StringComparison.CurrentCultureIgnoreCase);
            if (index == -1 || string.IsNullOrWhiteSpace(searchTerm))
            {
                if (_accessKeyIndex != -1)
                {
                    // Unregister and let the content control handle access keys
                    AccessKeyManager.Unregister(_content[_accessKeyIndex].ToString(), _control);
                }

                _control.Content = _originalContent;
                return false;
            }

            if (_accessKeyIndex != -1)
            {
                // Because we are overriding the content entirely, we have to handle access keys
                AccessKeyManager.Register(_content[_accessKeyIndex].ToString(), _control);
            }

            _control.Content = CreateHighlightingTextRun(index, searchTerm.Length);
            return true;
        }

        public void EnsureVisible()
        {
            _control.BringIntoView();
        }

        private TextBlock CreateHighlightingTextRun(int highlightStart, int length)
        {
            var textBlock = new TextBlock();
            AddTextRun(textBlock, 0, highlightStart, highlight: false);
            AddTextRun(textBlock, highlightStart, length, highlight: true);

            var highlightEnd = highlightStart + length;
            AddTextRun(textBlock, highlightEnd, _content.Length - highlightEnd, highlight: false);

            return textBlock;
        }

        private void AddTextRun(TextBlock textBlock, int start, int length, bool highlight)
        {
            if (length <= 0)
                return;

            // If the access key is in this run, then we actually need to add three runs
            if (_accessKeyIndex >= start && _accessKeyIndex < start + length)
            {
                var firstPartLength = _accessKeyIndex - start;
                var lastPartLength = length - firstPartLength - 1;

                if (firstPartLength > 0)
                    textBlock.Inlines.Add(CreateRun(start, firstPartLength, highlight, underline: false));

                textBlock.Inlines.Add(CreateRun(_accessKeyIndex, 1, highlight, underline: true));

                if (lastPartLength > 0)
                    textBlock.Inlines.Add(CreateRun(_accessKeyIndex + 1, lastPartLength, highlight, underline: false));
            }
            else
            {
                textBlock.Inlines.Add(CreateRun(start, length, highlight, underline: false));
            }
        }

        private Run CreateRun(int start, int length, bool highlight, bool underline)
        {
            var run = new Run(_content.Substring(start, length));

            if (highlight)
            {
                run.Background = HighlightBackground;
                run.Foreground = HighlightForeground;
            }

            if (underline)
                run.TextDecorations.Add(TextDecorations.Underline);

            return run;
        }
    }
}
