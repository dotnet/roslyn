// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.InteractiveWindow.Shell;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive
{
    [Export(typeof(IVsInteractiveWindowOleCommandTargetProvider))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName)]
    internal sealed class CSharpVsInteractiveWindowCommandProvider : IVsInteractiveWindowOleCommandTargetProvider
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;
        private readonly ICommandHandlerServiceFactory _commandHandlerServiceFactory;
        private readonly System.IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public CSharpVsInteractiveWindowCommandProvider(
            ICommandHandlerServiceFactory commandHandlerServiceFactory,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            SVsServiceProvider serviceProvider)
        {
            _commandHandlerServiceFactory = commandHandlerServiceFactory;
            _editorAdaptersFactory = editorAdaptersFactoryService;
            _serviceProvider = serviceProvider;
        }

        public IOleCommandTarget GetCommandTarget(IWpfTextView textView, IOleCommandTarget nextTarget)
        {
            var target = new ScriptingOleCommandTarget(textView, _commandHandlerServiceFactory, _editorAdaptersFactory, _serviceProvider);
            target.RefreshCommandFilters();
            target.NextCommandTarget = nextTarget;
            return target;
        }
    }
}
