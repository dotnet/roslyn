// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;

[Export(typeof(IVsInteractiveWindowOleCommandTargetProvider))]
[ContentType(ContentTypeNames.CSharpContentType)]
[ContentType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName)]
internal sealed class CSharpVsInteractiveWindowCommandProvider : IVsInteractiveWindowOleCommandTargetProvider
{
    private readonly System.IServiceProvider _serviceProvider;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
