// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class SourceReferenceTreeItem : AbstractTreeItem, IComparable<SourceReferenceTreeItem>
    {
        protected readonly Workspace _workspace;
        protected readonly DocumentId _documentId;
        protected readonly string _projectName;
        protected readonly string _filePath;
        protected readonly TextSpan _sourceSpan;
        protected readonly string _textLineString;
        protected readonly int _lineNumber;
        protected readonly int _offset;
        protected readonly int _mappedLineNumber;
        protected readonly int _mappedOffset;

        private static readonly ObjectPool<StringBuilder> s_filePathBuilderPool = new ObjectPool<StringBuilder>(() => new StringBuilder());

        private SourceReferenceTreeItem(
            Document document, TextSpan sourceSpan, ushort glyphIndex, int commonPathElements)
            : base(glyphIndex)
        {
            // We store the document ID, line and offset for navigation so that we
            // still provide reasonable navigation if the user makes changes elsewhere
            // in the document other than inserting or removing lines.

            _workspace = document.Project.Solution.Workspace;
            _documentId = document.Id;
            _projectName = document.Project.Name;
            _filePath = GetFilePath(document, commonPathElements);
            _sourceSpan = sourceSpan;

            var text = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var textLine = text.Lines.GetLineFromPosition(_sourceSpan.Start);
            _textLineString = textLine.ToString();

            _lineNumber = textLine.LineNumber;
            _offset = sourceSpan.Start - textLine.Start;

            var spanInSecondaryBuffer = text.GetVsTextSpanForLineOffset(_lineNumber, _offset);

            VsTextSpan spanInPrimaryBuffer;
            var succeeded = spanInSecondaryBuffer.TryMapSpanFromSecondaryBufferToPrimaryBuffer(_workspace, _documentId, out spanInPrimaryBuffer);

            _mappedLineNumber = succeeded ? spanInPrimaryBuffer.iStartLine : _lineNumber;
            _mappedOffset = succeeded ? spanInPrimaryBuffer.iStartIndex : _offset;
        }

        public SourceReferenceTreeItem(
            Document document, TextSpan sourceSpan, ushort glyphIndex,
            int commonPathElements, string displayText = null, bool includeFileLocation = false)
            : this(document, sourceSpan, glyphIndex, commonPathElements)
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

        public override int GoToSource()
        {
            var navigationService = _workspace.Services.GetService<IDocumentNavigationService>();
            navigationService.TryNavigateToLineAndOffset(_workspace, _documentId, _lineNumber, _offset);

            return VSConstants.S_OK;
        }

        private static string GetFilePath(Document document, int commonPathElements)
        {
            var builder = s_filePathBuilderPool.Allocate();
            try
            {
                if (commonPathElements <= 0)
                {
                    builder.Append(document.Project.Name);
                    builder.Append('\\');
                }

                commonPathElements--;
                foreach (var folder in document.Folders)
                {
                    if (commonPathElements <= 0)
                    {
                        builder.Append(folder);
                        builder.Append('\\');
                    }

                    commonPathElements--;
                }

                builder.Append(Path.GetFileName(document.FilePath));

                return builder.ToString();
            }
            finally
            {
                s_filePathBuilderPool.ClearAndFree(builder);
            }
        }

        public override bool CanGoToReference() => true;

        public override bool UseGrayText => false;

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

        private void SetDisplayProperties(string filePath, int mappedLineNumber, int mappedOffset, int offset, string lineText, int spanLength, string projectNameDisambiguator, string explicitDisplayText = null)
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

        int IComparable<SourceReferenceTreeItem>.CompareTo(SourceReferenceTreeItem other)
        {
            if (other == null)
            {
                return 1;
            }

            int compare = LogicalStringComparer.Instance.Compare(_filePath, _filePath);
            compare = compare != 0 ? compare : _lineNumber.CompareTo(other._lineNumber);
            compare = compare != 0 ? compare : _offset.CompareTo(other._offset);
            compare = compare != 0 ? compare : _projectName.CompareTo(other._projectName);

            return compare;
        }
    }
}