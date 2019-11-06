// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal sealed class ParameterTypeEditorControl : AbstractOleCommandTarget
    {
        internal ParameterTypeEditorControl(
            IWpfTextView wpfTextView,
            IVsEditorAdaptersFactoryService editorAdaptersFactory,
            IServiceProvider serviceProvider)
            : base(wpfTextView, editorAdaptersFactory, serviceProvider)
        {
        }

        protected override ITextBuffer GetSubjectBufferContainingCaret()
        {
            return this.WpfTextView.GetBufferContainingCaret(contentType: ContentTypeNames.RoslynContentType);
        }

        public string GetText() => this.WpfTextView.TextSnapshot.GetText();
    }
}
