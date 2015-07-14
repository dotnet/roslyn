// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging.TagSources
{
    /// <summary>
    /// A derivation of <see cref="ProducerPopulatedTagSource{TTag}"/> that tags a single subject buffer. It does not use a view.
    /// </summary>
    /// <typeparam name="TTag"></typeparam>
    internal class BufferTagSource<TTag> : ProducerPopulatedTagSource<TTag> where TTag : ITag
    {
        public BufferTagSource(
            ITextBuffer subjectBuffer,
            IAsynchronousTaggerDataSource<TTag> dataSource,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
                : base(/*textViewOpt:*/ null, subjectBuffer, dataSource, asyncListener, notificationService)
        {
        }

        protected override ICollection<SnapshotSpan> GetInitialSpansToTag()
        {
            return new[] { SubjectBuffer.CurrentSnapshot.GetFullSpan() };
        }

        protected override SnapshotPoint? GetCaretPoint()
        {
            return null;
        }
    }
}
