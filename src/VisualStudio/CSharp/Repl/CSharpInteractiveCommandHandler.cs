// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive
{
    [ExportCommandHandler("Interactive Command Handler", ContentTypeNames.CSharpContentType)]
    internal sealed class CSharpInteractiveCommandHandler : InteractiveCommandHandler
    {
        private readonly CSharpVsInteractiveWindowProvider _interactiveWindowProvider;

        [ImportingConstructor]
        public CSharpInteractiveCommandHandler(
            CSharpVsInteractiveWindowProvider interactiveWindowProvider,
            IContentTypeRegistryService contentTypeRegistryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(contentTypeRegistryService, editorOptionsFactoryService, editorOperationsFactoryService)
        {
            _interactiveWindowProvider = interactiveWindowProvider;
        }

        protected override IInteractiveWindow OpenInteractiveWindow(bool focus)
        {
            return _interactiveWindowProvider.Open(instanceId: 0, focus: focus).InteractiveWindow;
        }
    }
}
