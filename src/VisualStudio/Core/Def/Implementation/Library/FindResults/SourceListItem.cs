// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class SourceListItem : AbstractListItem
    {
        private readonly Workspace _workspace;
        private readonly DocumentId _documentId;
        private readonly int _lineNumber;
        private readonly int _offset;

        public SourceListItem(Location location, Solution solution, ushort glyphIndex)
            : this(solution.GetDocument(location.SourceTree), location.SourceSpan, glyphIndex)
        {
        }

        public SourceListItem(Document document, TextSpan sourceSpan, ushort glyphIndex)
            : base(glyphIndex)
        {
            _workspace = document.Project.Solution.Workspace;

            // We store the document ID, line and offset for navigation so that we
            // still provide reasonable navigation if the user makes changes elsewhere
            // in the document other than inserting or removing lines.
            _documentId = document.Id;

            var filePath = document.FilePath;

            var text = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var textLine = text.Lines.GetLineFromPosition(sourceSpan.Start);

            _lineNumber = textLine.LineNumber;
            _offset = sourceSpan.Start - textLine.Start;

            var spanInSecondaryBuffer = text.GetVsTextSpanForLineOffset(_lineNumber, _offset);

            VsTextSpan spanInPrimaryBuffer;
            var succeeded = spanInSecondaryBuffer.TryMapSpanFromSecondaryBufferToPrimaryBuffer(_workspace, document.Id, out spanInPrimaryBuffer);

            var mappedLineNumber = succeeded ? spanInPrimaryBuffer.iStartLine : _lineNumber;
            var mappedOffset = succeeded ? spanInPrimaryBuffer.iStartIndex : _offset;

            SetDisplayProperties(filePath, mappedLineNumber, mappedOffset, _lineNumber, _offset, textLine.ToString(), sourceSpan.Length);
        }

        public override int GoToSource()
        {
            var navigationService = _workspace.Services.GetService<IDocumentNavigationService>();
            navigationService.TryNavigateToLineAndOffset(_workspace, _documentId, _lineNumber, _offset);

            return VSConstants.S_OK;
        }
    }
}
