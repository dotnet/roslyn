// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    /// <summary>
    /// Creates quick info content out of the span of an existing snapshot.  The span will be
    /// used to create an elision buffer out that will then be displayed in the quick info
    /// window.
    /// </summary>
    internal class ElisionBufferDeferredContent : IDeferredQuickInfoContent
    {
        private readonly SnapshotSpan _span;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;

        public ElisionBufferDeferredContent(
            SnapshotSpan span,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService)
        {
            _span = span;
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
        }

        public FrameworkElement Create()
        {
            return new ViewHostingControl(CreateView, CreateBuffer);
        }

        private IWpfTextView CreateView(ITextBuffer buffer)
        {
            var view = _textEditorFactoryService.CreateTextView(
                buffer, _textEditorFactoryService.NoRoles);

            view.SizeToFit();
            view.Background = Brushes.Transparent;

            // Zoom out a bit to shrink the text.
            view.ZoomLevel *= 0.75;

            return view;
        }

        private IElisionBuffer CreateBuffer()
        {
            return _projectionBufferFactoryService.CreateElisionBufferWithoutIndentation(
                            _editorOptionsFactoryService.GlobalOptions, _span);
        }
    }
}
