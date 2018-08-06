// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    /// <summary>
    /// Creates quick info content out of the span of an existing snapshot.  The span will be
    /// used to create an projection buffer out that will then be displayed in the quick info
    /// window.
    /// </summary>
    internal class ProjectionBufferDeferredContent : IDeferredQuickInfoContent
    {
        internal SnapshotSpan Span { get; }
        internal IContentType ContentType { get; }
        internal ITextViewRoleSet RoleSet { get; }

        public ProjectionBufferDeferredContent(
            SnapshotSpan span,
            IContentType contentType = null,
            ITextViewRoleSet roleSet = null)
        {
            Span = span;
            ContentType = contentType;
            RoleSet = roleSet;
        }
    }
}
