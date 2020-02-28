﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.CallHierarchy;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy
{
    internal class CallHierarchyDetail : ICallHierarchyItemDetails
    {
        private readonly TextSpan _span;
        private readonly DocumentId _documentId;
        private readonly Workspace _workspace;
        private readonly int _endColumn;
        private readonly int _endLine;
        private readonly string _sourceFile;
        private readonly int _startColumn;
        private readonly int _startLine;
        private readonly string _text;

        public CallHierarchyDetail(Location location, Workspace workspace)
        {
            _span = location.SourceSpan;
            _documentId = workspace.CurrentSolution.GetDocumentId(location.SourceTree);
            _workspace = workspace;
            _endColumn = location.GetLineSpan().Span.End.Character;
            _endLine = location.GetLineSpan().EndLinePosition.Line;
            _sourceFile = location.SourceTree.FilePath;
            _startColumn = location.GetLineSpan().StartLinePosition.Character;
            _startLine = location.GetLineSpan().StartLinePosition.Line;
            _text = ComputeText(location);
        }

        private string ComputeText(Location location)
        {
            var lineSpan = location.GetLineSpan();
            var start = location.SourceTree.GetText().Lines[lineSpan.StartLinePosition.Line].Start;
            var end = location.SourceTree.GetText().Lines[lineSpan.EndLinePosition.Line].End;
            return location.SourceTree.GetText().GetSubText(TextSpan.FromBounds(start, end)).ToString();
        }

        public int EndColumn => _endColumn;

        public int EndLine => _endLine;

        public string File => _sourceFile;

        public int StartColumn => _startColumn;

        public int StartLine => _startLine;

        public bool SupportsNavigateTo => true;

        public string Text => _text;

        public void NavigateTo()
        {
            var document = _workspace.CurrentSolution.GetDocument(_documentId);

            if (document != null)
            {
                var navigator = _workspace.Services.GetService<IDocumentNavigationService>();
                var options = _workspace.Options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true);
                navigator.TryNavigateToSpan(_workspace, document.Id, _span, options);
            }
        }
    }
}
