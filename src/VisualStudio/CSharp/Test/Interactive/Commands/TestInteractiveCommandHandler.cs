﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            ISendToInteractiveSubmissionProvider sendToInteractiveSubmissionProvider,
            IContentTypeRegistryService contentTypeRegistryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(contentTypeRegistryService, editorOptionsFactoryService, editorOperationsFactoryService)
        {
            _interactiveWindow = interactiveWindow;
            _sendToInteractiveSubmissionProvider = sendToInteractiveSubmissionProvider;
        }

        protected override ISendToInteractiveSubmissionProvider SendToInteractiveSubmissionProvider => _sendToInteractiveSubmissionProvider;

        protected override IInteractiveWindow OpenInteractiveWindow(bool focus)
        {
            return _interactiveWindow;
        }
    }
}
