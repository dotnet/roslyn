// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal class VenusCommandFilter<TPackage, TLanguageService, TProject> : AbstractVsTextViewFilter<TPackage, TLanguageService, TProject>
        where TPackage : AbstractPackage<TPackage, TLanguageService, TProject>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService, TProject>
        where TProject : AbstractProject
    {
        private readonly ITextBuffer _subjectBuffer;

        public VenusCommandFilter(
            TLanguageService languageService,
            IWpfTextView wpfTextView,
            ICommandHandlerServiceFactory commandHandlerServiceFactory,
            ITextBuffer subjectBuffer,
            IOleCommandTarget nextCommandTarget,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(languageService, wpfTextView, editorAdaptersFactoryService, commandHandlerServiceFactory)
        {
            Contract.ThrowIfNull(wpfTextView);
            Contract.ThrowIfNull(subjectBuffer);
            Contract.ThrowIfNull(nextCommandTarget);

            _subjectBuffer = subjectBuffer;
            CurrentHandlers = commandHandlerServiceFactory.GetService(subjectBuffer);
            NextCommandTarget = nextCommandTarget;
        }

        protected override ITextBuffer GetSubjectBufferContainingCaret()
        {
            return _subjectBuffer;
        }

        public override int GetDataTipText(TextSpan[] pSpan, out string pbstrText)
        {
            if (pSpan == null || pSpan.Length != 1)
            {
                pbstrText = null;
                return VSConstants.E_INVALIDARG;
            }

            // We need to map the TextSpan from the DataBuffer to our subject buffer
            // In the event that the position is on a seam and maps to multiple 
            // source spans, take the one that had a length of 1 (since that's how long
            // our input span was).
            // If we had no such spans, just return.
            var span = WpfTextView.TextViewModel.DataBuffer.CurrentSnapshot.GetSpan(pSpan[0]);
            var subjectSpan = WpfTextView.BufferGraph.Hack_WorkaroundElisionBuffers_MapDownToBuffer(span, SpanTrackingMode.EdgeInclusive, _subjectBuffer)
                                .SingleOrDefault(x => x.Length == 1);

            if (subjectSpan == default(SnapshotSpan))
            {
                pbstrText = null;
                return VSConstants.E_FAIL;
            }

            pSpan[0] = subjectSpan.ToVsTextSpan();

            int hr = base.GetDataTipText(pSpan, out pbstrText);

            // pSpan is an in/out parameter, so map it back to the Databuffer.
            if (ErrorHandler.Succeeded(hr))
            {
                subjectSpan = _subjectBuffer.CurrentSnapshot.GetSpan(pSpan[0]);

                // When mapping back up to the surface buffer, if we get more than one span,
                // take the span that intersects with the input span, since that's probably
                // the one we care about.
                // If there are no such spans, just return.
                var surfaceSpan = WpfTextView.BufferGraph.MapUpToBuffer(subjectSpan, SpanTrackingMode.EdgeInclusive, WpfTextView.TextViewModel.DataBuffer)
                                    .SingleOrDefault(x => x.IntersectsWith(span));

                if (surfaceSpan == default(SnapshotSpan))
                {
                    pbstrText = null;
                    return VSConstants.E_FAIL;
                }

                pSpan[0] = surfaceSpan.ToVsTextSpan();
            }

            return hr;
        }
    }
}
