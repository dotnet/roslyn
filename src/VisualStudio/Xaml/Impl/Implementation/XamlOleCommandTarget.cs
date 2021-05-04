// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        protected override ITextBuffer? GetSubjectBufferContainingCaret()
        {
            return this.WpfTextView.GetBufferContainingCaret(contentType: ContentTypeNames.XamlContentType);
        }
    }
}
