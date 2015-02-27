// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Classifies error text in interactive window output.
    /// </summary>
    [Export(typeof(IClassifierProvider))]
    [ContentType(PredefinedInteractiveContentTypes.InteractiveOutputContentTypeName)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class OutputClassifierProvider : IClassifierProvider
    {
        private static readonly object s_textBufferPropertyKey = new object();

        [Import]
        private IClassificationTypeRegistryService _classificationRegistry = null;

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return new Classifier(
                textBuffer,
                _classificationRegistry.GetClassificationType(FormatDefinitions.Output.Name),
                _classificationRegistry.GetClassificationType(FormatDefinitions.ErrorOutput.Name));
        }

        internal static void AttachToBuffer(ITextBuffer buffer, SortedSpans spans)
        {
            buffer.Properties[s_textBufferPropertyKey] = spans;
        }

        internal static void ClearSpans(ITextBuffer buffer)
        {
            SortedSpans errorSpans;
            if (buffer.Properties.TryGetProperty(s_textBufferPropertyKey, out errorSpans))
            {
                errorSpans.Clear();
            }
        }

        private sealed class Classifier : IClassifier
        {
            private readonly ITextBuffer _buffer;
            private readonly IClassificationType _outputType;
            private readonly IClassificationType _errorOutputType;

            public Classifier(ITextBuffer buffer, IClassificationType outputType, IClassificationType errorOutputType)
            {
                _outputType = outputType;
                _errorOutputType = errorOutputType;
                _buffer = buffer;
            }

            public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
            {
                SortedSpans errorSpans;
                if (!_buffer.Properties.TryGetProperty(s_textBufferPropertyKey, out errorSpans))
                {
                    return SpecializedCollections.EmptyList<ClassificationSpan>();
                }

                List<ClassificationSpan> classifications = new List<ClassificationSpan>();
                classifications.Add(new ClassificationSpan(span, _outputType));

                foreach (var overlap in errorSpans.GetOverlap(span.Span))
                {
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(span.Snapshot, overlap), _errorOutputType));
                }

                return classifications ?? (IList<ClassificationSpan>)SpecializedCollections.EmptyList<ClassificationSpan>();
            }

            public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
            {
                add { }
                remove { }
            }
        }

        private static class FormatDefinitions
        {
            [Export(typeof(EditorFormatDefinition))]
            [ClassificationType(ClassificationTypeNames = Name)]
            [Name(Name)]
            [DisplayName(Name)]
            [UserVisible(true)]
            internal sealed class Output : ClassificationFormatDefinition
            {
                public const string Name = "Roslyn - Interactive Window Output";

                [Export]
                [Name(Name)]
                [BaseDefinition(PredefinedClassificationTypeNames.NaturalLanguage)]
                internal static readonly ClassificationTypeDefinition Definition = null;

                public Output()
                {
                    this.ForegroundColor = Color.FromRgb(0, 0, 0);
                }
            }

            [Export(typeof(EditorFormatDefinition))]
            [ClassificationType(ClassificationTypeNames = Name)]
            [Name(Name)]
            [DisplayName(Name)]
            [UserVisible(true)]
            internal sealed class ErrorOutput : ClassificationFormatDefinition
            {
                public const string Name = "Roslyn - Interactive Window Error Output";

                [Export]
                [Name(Name)]
                [BaseDefinition(PredefinedClassificationTypeNames.NaturalLanguage)]
                internal static readonly ClassificationTypeDefinition Definition = null;

                public ErrorOutput()
                {
                    this.ForegroundColor = Color.FromRgb(0xff, 0, 0);
                }
            }
        }
    }
}
