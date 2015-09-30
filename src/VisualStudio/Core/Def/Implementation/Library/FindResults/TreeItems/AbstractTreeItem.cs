// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal abstract class AbstractTreeItem
    {
        public IList<AbstractTreeItem> Children { get; protected set; }
        public ushort GlyphIndex { get; protected set; }

        // TODO: Old C# code base has a helper, GetLineTextWithUnicodeDirectionMarkersIfNeeded, which we will need at some point.
        public string DisplayText { get; protected set; }
        public ushort DisplaySelectionStart { get; protected set; }
        public ushort DisplaySelectionLength { get; protected set; }

        public virtual bool UseGrayText
        {
            get
            {
                return this.Children == null || this.Children.Count == 0;
            }
        }

        protected static readonly SymbolDisplayFormat definitionDisplayFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword | SymbolDisplayKindOptions.IncludeNamespaceKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword,
                localOptions: SymbolDisplayLocalOptions.IncludeType,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        protected AbstractTreeItem(ushort glyphIndex)
        {
            this.Children = new List<AbstractTreeItem>();
            this.GlyphIndex = glyphIndex;
        }

        public abstract int GoToSource();

        public virtual bool CanGoToReference()
        {
            return false;
        }

        public virtual bool CanGoToDefinition()
        {
            return false;
        }

        protected void SetDisplayProperties(string filePath, int mappedLineNumber, int mappedOffset, int offset, string lineText, int spanLength, string projectNameDisambiguator, string explicitDisplayText = null)
        {
            var sourceSnippet = explicitDisplayText ?? lineText.Replace('\t', ' ').TrimStart(' ');
            var displayText = GetDisplayText(filePath, projectNameDisambiguator, mappedLineNumber + 1, mappedOffset + 1, sourceSnippet);

            var selectionStart = offset + displayText.Length - lineText.Length;

            displayText = displayText.TrimEnd();
            if (displayText.Length > ushort.MaxValue)
            {
                displayText = displayText.Substring(0, ushort.MaxValue);
            }

            this.DisplayText = displayText;

            if (explicitDisplayText == null)
            {
                this.DisplaySelectionStart = checked((ushort)Math.Min(ushort.MaxValue, selectionStart));
                this.DisplaySelectionLength = checked((ushort)Math.Min(spanLength, DisplayText.Length - DisplaySelectionStart));
            }
        }

        private static string GetDisplayText(string fileName, string projectNameDisambiguator, int lineNumber, int offset, string sourceText)
        {
            var fileLocationDescription = GetFileLocationsText(fileName, projectNameDisambiguator);
            return string.IsNullOrWhiteSpace(fileLocationDescription)
                ? $"({lineNumber}, {offset}) : {sourceText}"
                : $"{fileLocationDescription} - ({lineNumber}, {offset}) : {sourceText}";
        }

        private static string GetFileLocationsText(string fileName, string projectNameDisambiguator)
        {
            if (!string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(projectNameDisambiguator))
            {
                return $"{fileName} [{projectNameDisambiguator}]";
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }

            if (!string.IsNullOrWhiteSpace(projectNameDisambiguator))
            {
                return $"[{projectNameDisambiguator}]";
            }

            return string.Empty;
        }

        internal virtual void SetReferenceCount(int referenceCount)
        {
        }
    }
}
