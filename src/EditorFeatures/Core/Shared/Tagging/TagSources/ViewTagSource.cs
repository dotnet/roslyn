// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging.TagSources
{
    /// <summary>
    /// A derivation of <see cref="ProducerPopulatedTagSource{TTag}"/> that tags a single subject buffer in a view, and maps
    /// the caret position to that subject buffer.
    /// </summary>
    internal class ViewTagSource<TTag> : ProducerPopulatedTagSource<TTag> where TTag : ITag
    {
        private readonly ITextView _textView;

        public ViewTagSource(
            ITextView textView,
            ITextBuffer subjectBuffer,
            IAsynchronousTaggerDataSource<TTag> dataSource,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
                : base(textView, subjectBuffer, dataSource, asyncListener, notificationService)
        {
            _textView = textView;
        }

        protected override ICollection<SnapshotSpan> GetInitialSpansToTag()
        {
            // For a standard tagger, the spans to tag is the span of the entire snapshot.
            return new[] { SubjectBuffer.CurrentSnapshot.GetFullSpan() };
        }

        protected override SnapshotPoint? GetCaretPoint()
        {
            return _textView.GetCaretPoint(SubjectBuffer);
        }
    }
}
