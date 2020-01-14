// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Interactive
{
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

        protected override ITextBuffer GetSubjectBufferContainingCaret()
        {
            var result = WpfTextView.GetBufferContainingCaret(contentType: ContentTypeNames.RoslynContentType);

            if (result == null)
            {
                result = WpfTextView.GetBufferContainingCaret(contentType: PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName);
            }

            return result;
        }
    }
}
