// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TextViewRole(TextViewRoles.PreviewRole)]
    internal class PreviewClassificationTaggerProvider : ITaggerProvider
    {
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        public PreviewClassificationTaggerProvider(ClassificationTypeMap typeMap)
        {
            _typeMap = typeMap;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new Tagger(_typeMap, buffer) as ITagger<T>;
        }

        private class Tagger : ITagger<IClassificationTag>
        {
            private readonly ClassificationTypeMap _typeMap;
            private readonly ITextBuffer _buffer;

            public Tagger(ClassificationTypeMap typeMap, ITextBuffer buffer)
            {
                _typeMap = typeMap;
                _buffer = buffer;
            }

            public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                if (!_buffer.Properties.TryGetProperty(PredefinedPreviewTaggerKeys.StaticClassificationSpansKey, out ImmutableArray<ClassifiedSpan> classifiedSpans))
                {
                    yield break;
                }

                foreach (var span in spans)
                {
                    // we don't need to care about snapshot since everything is static and never changes in preview
                    var requestSpan = span.Span.ToTextSpan();

                    foreach (var classifiedSpan in classifiedSpans)
                    {
                        if (classifiedSpan.TextSpan.IntersectsWith(requestSpan))
                        {
                            yield return ClassificationUtilities.Convert(_typeMap, span.Snapshot, classifiedSpan);
                        }
                    }
                }
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged = (s, e) => { };
        }
    }
}
