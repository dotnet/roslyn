// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;

namespace Microsoft.VisualStudio.LanguageServices.Interactive
{
    /// <summary>
    /// This command target routes commands in interactive window, .csx files and also interactive
    /// commands in .cs files.
    /// </summary>
    internal sealed class ScriptingOleCommandTarget : AbstractOleCommandTarget
    {
        internal ScriptingOleCommandTarget(
            IWpfTextView wpfTextView,
            ICommandHandlerServiceFactory commandHandlerServiceFactory,
            IVsEditorAdaptersFactoryService editorAdaptersFactory,
            IServiceProvider serviceProvider)
            : base(wpfTextView, commandHandlerServiceFactory, editorAdaptersFactory, serviceProvider)
        {
            wpfTextView.Closed += OnTextViewClosed;
            wpfTextView.BufferGraph.GraphBufferContentTypeChanged += OnGraphBuffersChanged;
            wpfTextView.BufferGraph.GraphBuffersChanged += OnGraphBuffersChanged;

            RefreshCommandFilters();
        }

        private void OnGraphBuffersChanged(object sender, EventArgs e)
        {
            RefreshCommandFilters();
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            this.WpfTextView.Closed -= OnTextViewClosed;
            this.WpfTextView.BufferGraph.GraphBufferContentTypeChanged -= OnGraphBuffersChanged;
            this.WpfTextView.BufferGraph.GraphBuffersChanged -= OnGraphBuffersChanged;
        }

        protected override ITextBuffer GetSubjectBufferContainingCaret()
        {
            var result = WpfTextView.GetBufferContainingCaret(contentType: ContentTypeNames.RoslynContentType);

            if (result == null)
            {
                result = WpfTextView.GetBufferContainingCaret(contentType: PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName);
            }

            return result;
        }
    }
}
