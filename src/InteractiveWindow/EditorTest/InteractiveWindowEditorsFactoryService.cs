// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Roslyn.InteractiveWindow.UnitTests
{
    [Export(typeof(IInteractiveWindowEditorFactoryService))]
    internal class InteractiveWindowEditorsFactoryService : IInteractiveWindowEditorFactoryService
    {
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;

        [ImportingConstructor]
        public InteractiveWindowEditorsFactoryService(ITextBufferFactoryService textBufferFactoryService, ITextEditorFactoryService textEditorFactoryService)
        {
            _textBufferFactoryService = textBufferFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
        }

        IWpfTextView IInteractiveWindowEditorFactoryService.CreateTextView(IInteractiveWindow window, ITextBuffer buffer, ITextViewRoleSet roles)
        {
            var textView = _textEditorFactoryService.CreateTextView(buffer, roles);
            return _textEditorFactoryService.CreateTextViewHost(textView, false).TextView;
        }

        ITextBuffer IInteractiveWindowEditorFactoryService.CreateAndActivateBuffer(IInteractiveWindow window, IContentType contentType)
        {
            return _textBufferFactoryService.CreateTextBuffer(contentType);
        }
    }
}
