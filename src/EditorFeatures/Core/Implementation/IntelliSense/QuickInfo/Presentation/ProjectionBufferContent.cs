// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo.Presentation
{
    /// <summary>
    /// Creates quick info content out of the span of an existing snapshot.  The span will be
    /// used to create a projection buffer out that will then be displayed in the quick info
    /// window.
    /// </summary>
    internal class ProjectionBufferContent : ForegroundThreadAffinitizedObject
    {
        private readonly ImmutableArray<SnapshotSpan> _spans;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IContentType _contentType;
        private readonly ITextViewRoleSet _roleSet;

        private ProjectionBufferContent(
            ImmutableArray<SnapshotSpan> spans,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IContentType contentType = null,
            ITextViewRoleSet roleSet = null)
        {
            _spans = spans;
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
            _contentType = contentType;
            _roleSet = roleSet ?? _textEditorFactoryService.NoRoles;
        }

        public static ContentControl Create(
            ImmutableArray<SnapshotSpan> spans,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IContentType contentType = null,
            ITextViewRoleSet roleSet = null)
        {
            var content = new ProjectionBufferContent(
                spans,
                projectionBufferFactoryService,
                editorOptionsFactoryService,
                textEditorFactoryService,
                contentType,
                roleSet);

            return content.Create();
        }

        private ContentControl Create()
        {
            AssertIsForeground();

            return new ViewHostingControl(CreateView, CreateBuffer);
        }

        private IWpfTextView CreateView(ITextBuffer buffer)
        {
            var view = _textEditorFactoryService.CreateTextView(buffer, _roleSet);

            view.SizeToFit();
            view.Background = Brushes.Transparent;

            // Zoom out a bit to shrink the text.
            view.ZoomLevel *= 0.75;

            return view;
        }

        private IProjectionBuffer CreateBuffer()
        {
            return _projectionBufferFactoryService.CreateProjectionBufferWithoutIndentation(
                _editorOptionsFactoryService.GlobalOptions, _contentType, _spans.ToArray());
        }
    }
}
