// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    /// <summary>
    /// Creates quick info content out of the span of an existing snapshot.  The span will be
    /// used to create a projection buffer out that will then be displayed in the quick info
    /// window.
    /// </summary>
    internal sealed class ProjectionBufferContent
    {
        private readonly IThreadingContext _threadingContext;
        private readonly ImmutableArray<SnapshotSpan> _spans;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly EditorOptionsService _editorOptionsService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IContentType _contentType;
        private readonly ITextViewRoleSet _roleSet;

        private ProjectionBufferContent(
            IThreadingContext threadingContext,
            ImmutableArray<SnapshotSpan> spans,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            EditorOptionsService editorOptionsService,
            ITextEditorFactoryService textEditorFactoryService,
            IContentType contentType = null,
            ITextViewRoleSet roleSet = null)
        {
            _threadingContext = threadingContext;
            _spans = spans;
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsService = editorOptionsService;
            _textEditorFactoryService = textEditorFactoryService;
            _contentType = contentType;
            _roleSet = roleSet ?? _textEditorFactoryService.NoRoles;
        }

        public static ViewHostingControl Create(
            IThreadingContext threadingContext,
            ImmutableArray<SnapshotSpan> spans,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            EditorOptionsService editorOptionsService,
            ITextEditorFactoryService textEditorFactoryService,
            IContentType contentType = null,
            ITextViewRoleSet roleSet = null)
        {
            var content = new ProjectionBufferContent(
                threadingContext,
                spans,
                projectionBufferFactoryService,
                editorOptionsService,
                textEditorFactoryService,
                contentType,
                roleSet);

            return content.Create();
        }

        private ViewHostingControl Create()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            return new ViewHostingControl(CreateView, CreateBuffer);
        }

        private IWpfTextView CreateView(ITextBuffer buffer)
        {
            var view = _textEditorFactoryService.CreateTextView(buffer, _roleSet);

            view.SizeToFit(_threadingContext);
            view.Background = Brushes.Transparent;

            // Zoom out a bit to shrink the text.
            view.ZoomLevel *= 0.75;

            // turn off highlight current line
            view.Options.SetOptionValue(DefaultWpfViewOptions.EnableHighlightCurrentLineId, false);

            return view;
        }

        private IProjectionBuffer CreateBuffer()
        {
            return _projectionBufferFactoryService.CreateProjectionBufferWithoutIndentation(
                _editorOptionsService.Factory.GlobalOptions, _contentType, [.. _spans]);
        }
    }
}
