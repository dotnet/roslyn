// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    /// <summary>
    /// This command target routes commands in .xaml files.
    /// </summary>
    internal sealed class XamlOleCommandTarget : AbstractOleCommandTarget
    {
        internal XamlOleCommandTarget(
            IWpfTextView wpfTextView,
            ICommandHandlerServiceFactory commandHandlerServiceFactory,
            IVsEditorAdaptersFactoryService editorAdaptersFactory,
            IServiceProvider serviceProvider)
            : base(wpfTextView, commandHandlerServiceFactory, editorAdaptersFactory, serviceProvider)
        {
            wpfTextView.Closed += OnTextViewClosed;
            wpfTextView.BufferGraph.GraphBufferContentTypeChanged += OnGraphBuffersChanged;
            wpfTextView.BufferGraph.GraphBuffersChanged += OnGraphBuffersChanged;
        }

        private void OnGraphBuffersChanged(object sender, EventArgs e)
        {
            RefreshCommandFilters();
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            WpfTextView.Closed -= OnTextViewClosed;
            WpfTextView.BufferGraph.GraphBufferContentTypeChanged -= OnGraphBuffersChanged;
            WpfTextView.BufferGraph.GraphBuffersChanged -= OnGraphBuffersChanged;
        }

        protected override ITextBuffer GetSubjectBufferContainingCaret()
        {
            ITextBuffer result = this.WpfTextView.GetBufferContainingCaret(contentType: ContentTypeNames.XamlContentType);

            return result;
        }
    }
}
