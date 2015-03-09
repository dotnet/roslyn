// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class SourceReferenceTreeItem : AbstractSourceTreeItem, IComparable<SourceReferenceTreeItem>
    {
        public SourceReferenceTreeItem(Location location, Solution solution, ushort glyphIndex)
            : this(solution.GetDocument(location.SourceTree), location.SourceSpan, glyphIndex)
        {
        }

        public SourceReferenceTreeItem(Document document, TextSpan sourceSpan, ushort glyphIndex)
            : this(document, sourceSpan, glyphIndex, includeProjectNameDisambiguator: false)
        {
        }

        public SourceReferenceTreeItem(Document document, TextSpan sourceSpan, ushort glyphIndex, bool includeProjectNameDisambiguator)
            : base(document, sourceSpan, glyphIndex)
        {
            SetDisplayProperties(
                _filePath,
                _mappedLineNumber,
                _mappedOffset,
                _offset,
                _textLineString,
                sourceSpan.Length,
                projectNameDisambiguator: string.Empty);
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
