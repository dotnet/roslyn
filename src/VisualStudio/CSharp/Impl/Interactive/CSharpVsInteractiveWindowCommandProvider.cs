// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive
{
    [Export(typeof(IVsInteractiveWindowOleCommandTargetProvider))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName)]
    internal sealed class CSharpVsInteractiveWindowCommandProvider : IVsInteractiveWindowOleCommandTargetProvider
    {
        private readonly System.IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public CSharpVsInteractiveWindowCommandProvider(
            SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IOleCommandTarget GetCommandTarget(IWpfTextView textView, IOleCommandTarget nextTarget)
        {
            var target = new ScriptingOleCommandTarget(textView, (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel)));
            target.NextCommandTarget = nextTarget;
            return target;
        }
    }
}
