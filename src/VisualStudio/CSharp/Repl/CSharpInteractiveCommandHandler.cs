// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.CSharp.Interactive;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive
{
    [ExportCommandHandler("Interactive Command Handler")]
    internal sealed class CSharpInteractiveCommandHandler : InteractiveCommandHandler
    {
        private readonly CSharpVsInteractiveWindowProvider _interactiveWindowProvider;

        private readonly ISendToInteractiveSubmissionProvider _sendToInteractiveSubmissionProvider;

        [ImportingConstructor]
        public CSharpInteractiveCommandHandler(
            CSharpVsInteractiveWindowProvider interactiveWindowProvider,
            IContentTypeRegistryService contentTypeRegistryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IWaitIndicator waitIndicator)
            : base(contentTypeRegistryService, editorOptionsFactoryService, editorOperationsFactoryService, waitIndicator)
        {
            _interactiveWindowProvider = interactiveWindowProvider;
            _sendToInteractiveSubmissionProvider = new CSharpSendToInteractiveSubmissionProvider();
        }

        protected override ISendToInteractiveSubmissionProvider SendToInteractiveSubmissionProvider => _sendToInteractiveSubmissionProvider;

        protected override IInteractiveWindow OpenInteractiveWindow(bool focus)
        {
            return _interactiveWindowProvider.Open(instanceId: 0, focus: focus).InteractiveWindow;
        }
    }
}
