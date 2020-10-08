// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive
{
    [Export(typeof(IInteractiveWindowEditorFactoryService))]
    internal class TestInteractiveWindowEditorFactoryService : IInteractiveWindowEditorFactoryService
    {
        public const string ContentType = "text";

        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IContentTypeRegistryService _contentTypeRegistry;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestInteractiveWindowEditorFactoryService(ITextBufferFactoryService textBufferFactoryService, ITextEditorFactoryService textEditorFactoryService, IContentTypeRegistryService contentTypeRegistry)
        {
            _textBufferFactoryService = textBufferFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
            _contentTypeRegistry = contentTypeRegistry;
        }

        IWpfTextView IInteractiveWindowEditorFactoryService.CreateTextView(IInteractiveWindow window, ITextBuffer buffer, ITextViewRoleSet roles)
        {
            WpfTestRunner.RequireWpfFact($"Creates an {nameof(IWpfTextView)} in {nameof(TestInteractiveWindowEditorFactoryService)}");

            var textView = _textEditorFactoryService.CreateTextView(buffer, roles);
            return _textEditorFactoryService.CreateTextViewHost(textView, false).TextView;
        }

        ITextBuffer IInteractiveWindowEditorFactoryService.CreateAndActivateBuffer(IInteractiveWindow window)
        {
            if (!window.Properties.TryGetProperty(typeof(IContentType), out IContentType contentType))
            {
                contentType = _contentTypeRegistry.GetContentType(ContentType);
            }

            return _textBufferFactoryService.CreateTextBuffer(contentType);
        }
    }
}
