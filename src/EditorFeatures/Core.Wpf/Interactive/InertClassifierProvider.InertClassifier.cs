// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    internal partial class InertClassifierProvider
    {
        private class InertClassifier : IClassifier
        {
            private readonly ITextBuffer _textBuffer;

            public InertClassifier(ITextBuffer textBuffer)
            {
                _textBuffer = textBuffer;
            }

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

                return SpecializedCollections.EmptyList<ClassificationSpan>();
            }
        }
    }
}
