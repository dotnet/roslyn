// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.LineSeparators
{
    public class AdornmentManagerTests
    {
        internal class Tag : GraphicsTag
        {
            public override GraphicsResult GetGraphics(IWpfTextView textView, Geometry bounds)
            {
                return new GraphicsResult(null, null);
            }
        }

        private class AdornmentManagerTester
        {
            private readonly ITextBuffer _subjectBuffer;

            private readonly Mock<IWpfTextView> _textView;
            private readonly Mock<ITagAggregator<Tag>> _aggregator;
            private Mock<IMappingSpan> _mappingSpan;
            private readonly Mock<IAdornmentLayer> _adornmentLayer;

            public AdornmentManager<Tag> Manager { get; }

            private SnapshotSpan MySnapshotSpan
            {
                get
                {
                    return new SnapshotSpan(_subjectBuffer.CurrentSnapshot, new Span(0, 1));
                }
            }

            public AdornmentManagerTester()
            {
                _subjectBuffer = EditorFactory.CreateBuffer(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, "Hi There");

                _textView = new Mock<IWpfTextView>();
                var aggregatorService = new Mock<IViewTagAggregatorFactoryService>();
                _adornmentLayer = new Mock<IAdornmentLayer>();
                _aggregator = new Mock<ITagAggregator<Tag>>();

                var layerName = "LayerName";

                _textView.Setup(tv => tv.GetAdornmentLayer(layerName)).Returns(_adornmentLayer.Object);
                _textView.SetupGet(tv => tv.VisualElement).Returns(new FrameworkElement());

                aggregatorService.Setup(a => a.CreateTagAggregator<Tag>(_textView.Object)).Returns(_aggregator.Object);

                var textViewModel = new Mock<ITextViewModel>();
                textViewModel.Setup(tvm => tvm.VisualBuffer).Returns(_subjectBuffer);
                _textView.Setup(tv => tv.TextViewModel).Returns(textViewModel.Object);

                var workspace = new TestWorkspace();

                var listener = new AggregateAsynchronousOperationListener(
                    Enumerable.Empty<Lazy<IAsynchronousOperationListener, FeatureMetadata>>(),
                    FeatureAttribute.LineSeparators);
                Manager = AdornmentManager<Tag>.Create(_textView.Object,
                                                       aggregatorService.Object,
                                                       listener,
                                                       adornmentLayerName: layerName);
            }

            public void RaiseLayoutChanged()
            {
                Setup_UpdateSpans_CallOnlyOnUIThread();

                var newLine = new Mock<ITextViewLine>();
                newLine.SetupGet(line => line.ExtentIncludingLineBreak).Returns(_subjectBuffer.CurrentSnapshot.Lines.First().Extent);
                var viewState = new ViewState(_textView.Object);
                _textView.Raise(tv => tv.LayoutChanged += null, new TextViewLayoutChangedEventArgs(viewState, viewState, new[] { newLine.Object }, SpecializedCollections.EmptyArray<ITextViewLine>()));
                _adornmentLayer.Verify(al => al.AddAdornment(AdornmentPositioningBehavior.TextRelative, MySnapshotSpan, It.IsAny<object>(), null, It.IsAny<AdornmentRemovedCallback>()));
            }

            public void RaiseTagsChanged()
            {
                Setup_UpdateSpans_CallOnlyOnUIThread();

                _aggregator.Raise(a => a.TagsChanged += null, new TagsChangedEventArgs(_mappingSpan.Object));

                // The adornment manager posts things to the dispatcher queue.  We need to wait for them to complete.
                // We accomplish that by doing a synchronous invoke at the same priority level
                WaitHelper.WaitForDispatchedOperationsToComplete(DispatcherPriority.Render);

                _adornmentLayer.Verify(al => al.RemoveAdornmentsByVisualSpan(MySnapshotSpan));
                _adornmentLayer.Verify(al => al.AddAdornment(AdornmentPositioningBehavior.TextRelative, MySnapshotSpan, It.IsAny<object>(), null, It.IsAny<AdornmentRemovedCallback>()));
            }

            private void Setup_UpdateSpans_CallOnlyOnUIThread()
            {
                var textViewLineCollection = new Mock<IWpfTextViewLineCollection>();
                var mappingTagSpan = new Mock<IMappingTagSpan<Tag>>();
                _mappingSpan = new Mock<IMappingSpan>();

                _textView.SetupGet(tv => tv.TextViewLines).Returns(textViewLineCollection.Object);
                textViewLineCollection.SetupGet(tvlc => tvlc.Count).Returns(1);
                textViewLineCollection.Setup(tvlc => tvlc.IntersectsBufferSpan(It.IsAny<SnapshotSpan>())).Returns(true);
                textViewLineCollection.Setup(tvlc => tvlc.GetMarkerGeometry(It.IsAny<SnapshotSpan>())).Returns(new RectangleGeometry());

                _aggregator.Setup(a => a.GetTags(It.IsAny<SnapshotSpan>())).Returns(new[] { mappingTagSpan.Object });
                mappingTagSpan.SetupGet(mts => mts.Span).Returns(_mappingSpan.Object);
                mappingTagSpan.SetupGet(mts => mts.Tag).Returns(new Tag());

                _mappingSpan.Setup(ms => ms.GetSpans(It.IsAny<ITextSnapshot>())).Returns(new NormalizedSnapshotSpanCollection(MySnapshotSpan));
            }
        }

#if false
        // TODO(jasonmal): Figure out how to test these.
        [WpfFact, Trait(Traits.Feature, Traits.Features.Adornments)]
        public void Create()
        {
            Assert.NotNull(new AdornmentManagerTester().Manager);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Adornments)]
        public void LayoutChanged()
        {
            var tester = new AdornmentManagerTester();
            tester.RaiseLayoutChanged();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Adornments)]
        public void TagsChanged()
        {
            var tester = new AdornmentManagerTester();
            tester.RaiseTagsChanged();
        }
#endif
    }
}
