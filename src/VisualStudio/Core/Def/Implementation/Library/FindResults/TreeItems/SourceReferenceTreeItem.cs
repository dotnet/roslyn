// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class SourceReferenceTreeItem : AbstractSourceTreeItem, IComparable<SourceReferenceTreeItem>
    {
        public SourceReferenceTreeItem(Document document, TextSpan sourceSpan, ushort glyphIndex, int commonPathElements = 0, string displayText = null, bool includeFileLocation = false)
            : base(document, sourceSpan, glyphIndex, commonPathElements)
        {
            if (displayText != null && !includeFileLocation)
            {
                this.DisplayText = displayText;
            }
            else
            {
                SetDisplayProperties(
                    _filePath,
                    _mappedLineNumber,
                    _mappedOffset,
                    _offset,
                    _textLineString,
                    sourceSpan.Length,
                    projectNameDisambiguator: string.Empty,
                    explicitDisplayText: displayText);
            }
        }

        public override bool CanGoToReference()
        {
            return true;
        }

        public override bool UseGrayText
        {
            get
            {
                return false;
            }
        }

        public void AddProjectNameDisambiguator()
        {
            SetDisplayProperties(
                _filePath,
                _mappedLineNumber,
                _mappedOffset,
                _offset,
                _textLineString,
                _sourceSpan.Length,
                projectNameDisambiguator: _projectName);
        }

        int IComparable<SourceReferenceTreeItem>.CompareTo(SourceReferenceTreeItem other)
        {
            if (other == null)
            {
                return 1;
            }

            int compare = _filePath.CompareTo(other._filePath);
            compare = compare != 0 ? compare : _lineNumber.CompareTo(other._lineNumber);
            compare = compare != 0 ? compare : _offset.CompareTo(other._offset);
            compare = compare != 0 ? compare : _projectName.CompareTo(other._projectName);

            return compare;
        }
    }
}
