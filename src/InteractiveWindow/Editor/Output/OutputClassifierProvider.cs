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
        private static readonly object textBufferPropertyKey = new object();

        [Import]
        private IClassificationTypeRegistryService classificationRegistry = null;

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return new Classifier(
                textBuffer,
                classificationRegistry.GetClassificationType(FormatDefinitions.Output.Name),
                classificationRegistry.GetClassificationType(FormatDefinitions.ErrorOutput.Name));
        }

        internal static void AttachToBuffer(ITextBuffer buffer, SortedSpans spans)
        {
            buffer.Properties[textBufferPropertyKey] = spans;
        }

        internal static void ClearSpans(ITextBuffer buffer)
        {
            SortedSpans errorSpans;
            if (buffer.Properties.TryGetProperty(textBufferPropertyKey, out errorSpans))
            {
                errorSpans.Clear();
            }
        }

        private sealed class Classifier : IClassifier
        {
            private readonly ITextBuffer buffer;
            private readonly IClassificationType outputType;
            private readonly IClassificationType errorOutputType;

            public Classifier(ITextBuffer buffer, IClassificationType outputType, IClassificationType errorOutputType)
            {
                this.outputType = outputType;
                this.errorOutputType = errorOutputType;
                this.buffer = buffer;
            }

            public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
            {
                SortedSpans errorSpans;
                if (!buffer.Properties.TryGetProperty(textBufferPropertyKey, out errorSpans))
                {
                    return SpecializedCollections.EmptyList<ClassificationSpan>();
                }

                List<ClassificationSpan> classifications = new List<ClassificationSpan>();
                classifications.Add(new ClassificationSpan(span, outputType));

                foreach (var overlap in errorSpans.GetOverlap(span.Span))
                {
                    classifications.Add(new ClassificationSpan(new SnapshotSpan(span.Snapshot, overlap), errorOutputType));
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
