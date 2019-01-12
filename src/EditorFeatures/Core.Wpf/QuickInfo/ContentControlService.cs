// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    [ExportWorkspaceService(typeof(IContentControlService), layer: ServiceLayer.Editor), Shared]
    internal partial class ContentControlService : IContentControlService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ContentControlService(
            IThreadingContext threadingContext,
            ITextEditorFactoryService textEditorFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService)
        {
            _threadingContext = threadingContext;
            _textEditorFactoryService = textEditorFactoryService;
            _contentTypeRegistryService = contentTypeRegistryService;
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
        }

        public void AttachToolTipToControl(FrameworkElement element, Func<DisposableToolTip> createToolTip)
        {
            LazyToolTip.AttachTo(element, _threadingContext, createToolTip);
        }

        public DisposableToolTip CreateDisposableToolTip(Document baseDocument, ITextBuffer textBuffer, Span contentSpan, object backgroundResourceKey)
        {
            var control = CreateViewHostingControl(textBuffer, contentSpan);

            // Create the actual tooltip around the region of that text buffer we want to show.
            var toolTip = new ToolTip
            {
                Content = control,
                Background = (Brush)Application.Current.Resources[backgroundResourceKey]
            };

            // Create a preview workspace for this text buffer and open it's corresponding 
            // document. 
            // 
            // our underlying preview tagger and mechanism to attach tagger to associated buffer of
            // opened document will light up automatically
            var document = baseDocument.WithText(textBuffer.AsTextContainer().CurrentText);
            var workspace = new PreviewWorkspace(document.Project.Solution);
            workspace.OpenDocument(document.Id);

            return new DisposableToolTip(toolTip, workspace);
        }

        public DisposableToolTip CreateDisposableToolTip(ITextBuffer textBuffer, object backgroundResourceKey)
        {
            var control = CreateViewHostingControl(textBuffer, textBuffer.CurrentSnapshot.GetFullSpan().Span);

            // Create the actual tooltip around the region of that text buffer we want to show.
            var toolTip = new ToolTip
            {
                Content = control,
                Background = (Brush)Application.Current.Resources[backgroundResourceKey]
            };

            // we have stand alone view that is not associated with roslyn solution
            return new DisposableToolTip(toolTip, workspaceOpt: null);
        }

        public ViewHostingControl CreateViewHostingControl(ITextBuffer textBuffer, Span contentSpan)
        {
            var snapshotSpan = textBuffer.CurrentSnapshot.GetSpan(contentSpan);

            var contentType = _contentTypeRegistryService.GetContentType(
                IProjectionBufferFactoryServiceExtensions.RoslynPreviewContentType);

            var roleSet = _textEditorFactoryService.CreateTextViewRoleSet(
                TextViewRoles.PreviewRole,
                PredefinedTextViewRoles.Analyzable,
                PredefinedTextViewRoles.Document,
                PredefinedTextViewRoles.Editable);

            var contentControl = ProjectionBufferContent.Create(
                _threadingContext,
                ImmutableArray.Create(snapshotSpan),
                _projectionBufferFactoryService,
                _editorOptionsFactoryService,
                _textEditorFactoryService,
                contentType,
                roleSet);

            return contentControl;
        }
    }
}
