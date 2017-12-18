using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    [Export(typeof(IDeferredQuickInfoContentToFrameworkElementConverter))]
    class ProjectionBufferDeferredContentConverter : IDeferredQuickInfoContentToFrameworkElementConverter
    {
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;

        [ImportingConstructor]
        public ProjectionBufferDeferredContentConverter(
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService)
        {
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
        }

        public FrameworkElement CreateFrameworkElement(IDeferredQuickInfoContent deferredContent, DeferredContentFrameworkElementFactory factory)
        {
            var projectionBufferDeferredContent = (ProjectionBufferDeferredContent)deferredContent;
            return new ViewHostingControl(buffer => CreateView(projectionBufferDeferredContent, buffer), () => CreateBuffer(projectionBufferDeferredContent));
        }

        private IWpfTextView CreateView(ProjectionBufferDeferredContent deferredContent, ITextBuffer buffer)
        {
            var view = _textEditorFactoryService.CreateTextView(
                buffer, deferredContent.RoleSet ?? _textEditorFactoryService.NoRoles);

            view.SizeToFit();
            view.Background = Brushes.Transparent;

            // Zoom out a bit to shrink the text.
            view.ZoomLevel *= 0.75;

            return view;
        }

        private IProjectionBuffer CreateBuffer(ProjectionBufferDeferredContent deferredContent)
        {
            return _projectionBufferFactoryService.CreateProjectionBufferWithoutIndentation(
                _editorOptionsFactoryService.GlobalOptions, deferredContent.ContentType, deferredContent.Span);
        }

        public Type GetApplicableType()
        {
            return typeof(ProjectionBufferDeferredContent);
        }
    }
}
