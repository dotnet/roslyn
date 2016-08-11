// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.QuickInfo;
using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    /// <summary>
    /// Creates quick info content out of the span of an existing snapshot.  The span will be
    /// used to create an elision buffer out that will then be displayed in the quick info
    /// window.
    /// </summary>
    [ExportQuickInfoPresentationProvider(QuickInfoElementKinds.DocumentText)]
    internal class DocumentSpanPresentationProvider : QuickInfoPresentationProvider
    {
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;

        [ImportingConstructor]
        public DocumentSpanPresentationProvider(
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService)
        {
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
        }

        public override FrameworkElement CreatePresentation(QuickInfoElement element, ITextSnapshot snapshot)
        {
            return new ViewHostingControl(
                CreateView,
                () => CreateBuffer(
                    element.Spans.Select(s => new SnapshotSpan(snapshot, new Span(s.Start, s.Length)))
                    ));
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

        private IElisionBuffer CreateBuffer(IEnumerable<SnapshotSpan> spans)
        {
            return _projectionBufferFactoryService.CreateElisionBufferWithoutIndentation(
                            _editorOptionsFactoryService.GlobalOptions, spans.ToArray());
        }
    }
}
