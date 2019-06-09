// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    internal static class TextEditorFactoryExtensions
    {
        public static DisposableTextView CreateDisposableTextView(this ITextEditorFactoryService textEditorFactory)
        {
            return new DisposableTextView(textEditorFactory.CreateTextView());
        }

        public static DisposableTextView CreateDisposableTextView(this ITextEditorFactoryService textEditorFactory, ITextBuffer buffer)
        {
            // Every default role but outlining. Starting in 15.2, the editor
            // OutliningManager imports JoinableTaskContext in a way that's 
            // difficult to satisfy in our unit tests. Since we don't directly
            // depend on it, just disable it
            var roles = textEditorFactory.CreateTextViewRoleSet(PredefinedTextViewRoles.Analyzable,
                PredefinedTextViewRoles.Document,
                PredefinedTextViewRoles.Editable,
                PredefinedTextViewRoles.Interactive,
                PredefinedTextViewRoles.Zoomable);

            return new DisposableTextView(textEditorFactory.CreateTextView(buffer, roles));
        }
    }

    public class DisposableTextView : IDisposable
    {
        public DisposableTextView(IWpfTextView textView)
        {
            this.TextView = textView;
        }

        public IWpfTextView TextView { get; }

        public void Dispose()
        {
            TextView.Close();
        }
    }
}
