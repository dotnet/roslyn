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
            return new DisposableTextView(textEditorFactory.CreateTextView(buffer));
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
