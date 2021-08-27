﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.CommandHandlers;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive
{
    [ExportInteractive(typeof(IExecuteInInteractiveCommandHandler), ContentTypeNames.CSharpContentType)]
    internal sealed class CSharpInteractiveCommandHandler : InteractiveCommandHandler, IExecuteInInteractiveCommandHandler
    {
        private readonly CSharpVsInteractiveWindowProvider _interactiveWindowProvider;

        private readonly ISendToInteractiveSubmissionProvider _sendToInteractiveSubmissionProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpInteractiveCommandHandler(
            CSharpVsInteractiveWindowProvider interactiveWindowProvider,
            ISendToInteractiveSubmissionProvider sendToInteractiveSubmissionProvider,
            IContentTypeRegistryService contentTypeRegistryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(contentTypeRegistryService, editorOptionsFactoryService, editorOperationsFactoryService)
        {
            _interactiveWindowProvider = interactiveWindowProvider;
            _sendToInteractiveSubmissionProvider = sendToInteractiveSubmissionProvider;
        }

        protected override ISendToInteractiveSubmissionProvider SendToInteractiveSubmissionProvider => _sendToInteractiveSubmissionProvider;

        protected override IInteractiveWindow OpenInteractiveWindow(bool focus)
            => _interactiveWindowProvider.Open(instanceId: 0, focus: focus).InteractiveWindow;
    }
}
