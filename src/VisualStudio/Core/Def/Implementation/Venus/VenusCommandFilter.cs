// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal class VenusCommandFilter : AbstractVsTextViewFilter
    {
        private readonly ITextBuffer _subjectBuffer;

        public VenusCommandFilter(
            IWpfTextView wpfTextView,
            ITextBuffer subjectBuffer,
            IOleCommandTarget nextCommandTarget,
            IComponentModel componentModel)
            : base(wpfTextView, componentModel)
        {
            Contract.ThrowIfNull(wpfTextView);
            Contract.ThrowIfNull(subjectBuffer);
            Contract.ThrowIfNull(nextCommandTarget);

            _subjectBuffer = subjectBuffer;

            // Chain in editor command handler service. It will execute all our command handlers migrated to the modern editor commanding.
            var vsCommandHandlerServiceAdapterFactory = componentModel.GetService<IVsCommandHandlerServiceAdapterFactory>();
            var vsCommandHandlerServiceAdapter = vsCommandHandlerServiceAdapterFactory.Create(wpfTextView, _subjectBuffer, nextCommandTarget);
            NextCommandTarget = vsCommandHandlerServiceAdapter;
        }

        protected override ITextBuffer GetSubjectBufferContainingCaret()
            => _subjectBuffer;

        protected override int GetDataTipTextImpl(TextSpan[] pSpan, out string pbstrText)
        {
            var textViewModel = WpfTextView.TextViewModel;
            if (textViewModel == null)
            {
                Debug.Assert(WpfTextView.IsClosed);
                pbstrText = null;
                return VSConstants.E_FAIL;
            }

            // We need to map the TextSpan from the DataBuffer to our subject buffer.
            var span = textViewModel.DataBuffer.CurrentSnapshot.GetSpan(pSpan[0]);
            var subjectSpans = WpfTextView.BufferGraph.MapDownToBuffer(span, SpanTrackingMode.EdgeInclusive, _subjectBuffer);

            // The following loop addresses the case where the position is on a seam and maps to multiple source spans.
            // In these cases, we assume it's okay to return the first span that successfully returns a DataTip.
            // It's most likely that either only one will succeed or both with fail.
            var expectedSpanLength = span.Length;
            foreach (var candidateSpan in subjectSpans)
            {
                // First, we'll only consider spans whose length matches our input span. 
                if (candidateSpan.Length != expectedSpanLength)
                {
                    continue;
                }

                // Next, we'll check to see if there is actually a DataTip for this candidate.
                // If there is, we'll map this span back to the DataBuffer and return it.
                pSpan[0] = candidateSpan.ToVsTextSpan();
                var hr = base.GetDataTipTextImpl(_subjectBuffer, pSpan, out pbstrText);
                if (ErrorHandler.Succeeded(hr))
                {
                    var subjectSpan = _subjectBuffer.CurrentSnapshot.GetSpan(pSpan[0]);

                    // When mapping back up to the surface buffer, if we get more than one span,
                    // take the span that intersects with the input span, since that's probably
                    // the one we care about.
                    // If there are no such spans, just return.
                    var surfaceSpan = WpfTextView.BufferGraph.MapUpToBuffer(subjectSpan, SpanTrackingMode.EdgeInclusive, textViewModel.DataBuffer)
                                        .SingleOrDefault(x => x.IntersectsWith(span));

                    if (surfaceSpan == default)
                    {
                        pbstrText = null;
                        return VSConstants.E_FAIL;
                    }

                    // pSpan is an in/out parameter
                    pSpan[0] = surfaceSpan.ToVsTextSpan();

                    return hr;
                }
            }

            pbstrText = null;
            return VSConstants.E_FAIL;
        }
    }
}
