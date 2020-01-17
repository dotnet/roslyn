// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    /// <summary>
    /// This command target routes commands in .xaml files.
    /// </summary>
    internal sealed class XamlOleCommandTarget : AbstractOleCommandTarget
    {
        internal XamlOleCommandTarget(
            IWpfTextView wpfTextView,
            IComponentModel componentModel)
            : base(wpfTextView, componentModel)
        {
        }

        protected override ITextBuffer GetSubjectBufferContainingCaret()
        {
            return this.WpfTextView.GetBufferContainingCaret(contentType: ContentTypeNames.XamlContentType);
        }
    }
}
