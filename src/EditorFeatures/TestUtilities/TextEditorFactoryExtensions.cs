﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    internal static class TextEditorFactoryExtensions
    {
        public static DisposableTextView CreateDisposableTextView(this ITextEditorFactoryService textEditorFactory)
            => new DisposableTextView(textEditorFactory.CreateTextView());

        public static DisposableTextView CreateDisposableTextView(this ITextEditorFactoryService textEditorFactory, ITextBuffer buffer, ImmutableArray<string> roles = default)
        {
            // Every default role but outlining. Starting in 15.2, the editor
            // OutliningManager imports JoinableTaskContext in a way that's 
            // difficult to satisfy in our unit tests. Since we don't directly
            // depend on it, just disable it
            if (roles.IsDefault)
            {
                roles = ImmutableArray.Create(PredefinedTextViewRoles.Analyzable,
                    PredefinedTextViewRoles.Document,
                    PredefinedTextViewRoles.Editable,
                    PredefinedTextViewRoles.Interactive,
                    PredefinedTextViewRoles.Zoomable);
            }

            var roleSet = textEditorFactory.CreateTextViewRoleSet(roles);
            return new DisposableTextView(textEditorFactory.CreateTextView(buffer, roleSet));
        }
    }

    public class DisposableTextView : IDisposable
    {
        public DisposableTextView(IWpfTextView textView)
            => this.TextView = textView;

        public IWpfTextView TextView { get; }

        public void Dispose()
            => TextView.Close();
    }
}
