// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.Interactive;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive.Commands
{
    internal class TestInteractiveCommandHandler : InteractiveCommandHandler
    {
        private readonly IInteractiveWindow _interactiveWindow;

        private readonly ISendToInteractiveSubmissionProvider _sendToInteractiveSubmissionProvider;

        public TestInteractiveCommandHandler(
            IInteractiveWindow interactiveWindow,
            IContentTypeRegistryService contentTypeRegistryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IWaitIndicator waitIndicator)
            : base(contentTypeRegistryService, editorOptionsFactoryService, editorOperationsFactoryService, waitIndicator)
        {
            _interactiveWindow = interactiveWindow;
            _sendToInteractiveSubmissionProvider = new CSharpSendToInteractiveSubmissionProvider();
        }

        protected override ISendToInteractiveSubmissionProvider SendToInteractiveSubmissionProvider => _sendToInteractiveSubmissionProvider;

        protected override IInteractiveWindow OpenInteractiveWindow(bool focus)
        {
            return _interactiveWindow;
        }
    }
}
