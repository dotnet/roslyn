// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Interactive;

/// <summary>
/// This command target routes commands in interactive window, .csx files and also interactive
/// commands in .cs files.
/// </summary>
internal sealed class ScriptingOleCommandTarget : AbstractOleCommandTarget
{
    internal ScriptingOleCommandTarget(
        IWpfTextView wpfTextView,
        IComponentModel componentModel)
        : base(wpfTextView, componentModel)
    {
    }

    protected override ITextBuffer? GetSubjectBufferContainingCaret()
    {
        var result = WpfTextView.GetBufferContainingCaret(contentType: ContentTypeNames.RoslynContentType);

        result ??= WpfTextView.GetBufferContainingCaret(contentType: PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName);

        return result;
    }
}
