// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class InertClassifierProvider
    {
        private class InertClassifier : IClassifier
        {
            private readonly ITextBuffer _textBuffer;

            public InertClassifier(ITextBuffer textBuffer)
                => _textBuffer = textBuffer;

#pragma warning disable 67
            public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67

            public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
            {
                // See if we have cached classifications for this text buffer and return the ones
                // that intersect the requested span if we do.
                if (_textBuffer.Properties.TryGetProperty<IList<ClassificationSpan>>(s_classificationsKey, out var classifications))
                {
                    return classifications.Where(c => c.Span.IntersectsWith(span)).ToList();
                }

                return [];
            }
        }
    }
}
