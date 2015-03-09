// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal abstract class AbstractSourceTreeItem : AbstractTreeItem
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

        public AbstractSourceTreeItem(Document document, TextSpan sourceSpan, ushort glyphIndex)
            : base(glyphIndex)
        {
            // We store the document ID, line and offset for navigation so that we
            // still provide reasonable navigation if the user makes changes elsewhere
            // in the document other than inserting or removing lines.

            _workspace = document.Project.Solution.Workspace;
            _documentId = document.Id;
            _projectName = document.Project.Name;
            _filePath = GetFilePath(document);
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

        public override int GoToSource()
        {
            var navigationService = _workspace.Services.GetService<IDocumentNavigationService>();
            navigationService.TryNavigateToLineAndOffset(_workspace, _documentId, _lineNumber, _offset);

            return VSConstants.S_OK;
        }

        private static string GetFilePath(Document document)
        {
            var builder = s_filePathBuilderPool.Allocate();
            try
            {
                builder.Append(document.Project.Name);
                builder.Append('\\');

                foreach (var folder in document.Folders)
                {
                    builder.Append(folder);
                    builder.Append('\\');
                }

                builder.Append(Path.GetFileName(document.FilePath));

                return builder.ToString();
            }
            finally
            {
                s_filePathBuilderPool.ClearAndFree(builder);
            }
        }
    }
}
