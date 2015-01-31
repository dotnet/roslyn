// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal abstract class AbstractListItem
    {
        public ushort GlyphIndex { get; private set; }
        public string DisplayText { get; private set; }
        public ushort DisplaySelectionStart { get; private set; }
        public ushort DisplaySelectionLength { get; private set; }

        protected AbstractListItem(ushort glyphIndex)
        {
            this.GlyphIndex = glyphIndex;
        }

        protected void SetDisplayProperties(string filePath, int mappedLineNumber, int mappedOffset, int lineNumber, int offset, string lineText, int spanLength)
        {
            // TODO: Old C# code base has a helper, GetLineTextWithUnicodeDirectionMarkersIfNeeded, which we will need at some point.

            var sourceSnippet = lineText.Replace('\t', ' ').TrimStart(' ');
            var displayText = GetDisplayText(filePath, mappedLineNumber + 1, mappedOffset + 1, sourceSnippet);

            var selectionStart = offset + displayText.Length - lineText.Length;

            displayText = displayText.TrimEnd();
            if (displayText.Length > ushort.MaxValue)
            {
                displayText = displayText.Substring(0, ushort.MaxValue);
            }

            this.DisplayText = displayText;
            this.DisplaySelectionStart = checked((ushort)Math.Min(ushort.MaxValue, selectionStart));
            this.DisplaySelectionLength = checked((ushort)Math.Min(spanLength, DisplayText.Length - DisplaySelectionStart));
        }

        private static string GetDisplayText(string fileName, int lineNumber, int offset, string sourceText)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Format("({0}, {1}) : {2}", lineNumber, offset, sourceText);
            }
            else
            {
                return string.Format("{0} - ({1}, {2}) : {3}", fileName, lineNumber, offset, sourceText);
            }
        }

        public abstract int GoToSource();
    }
}
