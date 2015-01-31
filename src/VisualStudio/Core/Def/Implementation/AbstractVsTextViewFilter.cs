// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal abstract class AbstractVsTextViewFilter : AbstractOleCommandTarget
    {
        public AbstractVsTextViewFilter(
            IWpfTextView wpfTextView,
            ICommandHandlerServiceFactory commandHandlerServiceFactory,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IServiceProvider systemServiceProvider)
            : base(wpfTextView, commandHandlerServiceFactory, editorAdaptersFactoryService, systemServiceProvider)
        {
        }

        // Internal for testing purposes
        internal static int GetPairExtentsWorker(ITextView textView, Workspace workspace, IBraceMatchingService braceMatcher, int iLine, int iIndex, TextSpan[] pSpan, CancellationToken cancellationToken)
        {
            pSpan[0].iStartLine = pSpan[0].iEndLine = iLine;
            pSpan[0].iStartIndex = pSpan[0].iEndIndex = iIndex;

            var pointInViewBuffer = textView.TextSnapshot.GetLineFromLineNumber(iLine).Start + iIndex;

            var subjectBuffer = textView.GetBufferContainingCaret();
            if (subjectBuffer != null)
            {
                // PointTrackingMode and PositionAffinity chosen arbitrarily.
                var positionInSubjectBuffer = textView.BufferGraph.MapDownToBuffer(pointInViewBuffer, PointTrackingMode.Positive, subjectBuffer, PositionAffinity.Successor);
                if (!positionInSubjectBuffer.HasValue)
                {
                    positionInSubjectBuffer = textView.BufferGraph.MapDownToBuffer(pointInViewBuffer, PointTrackingMode.Positive, subjectBuffer, PositionAffinity.Predecessor);
                }

                if (positionInSubjectBuffer.HasValue)
                {
                    var position = positionInSubjectBuffer.Value;

                    var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document != null)
                    {
                        var matchingSpan = braceMatcher.FindMatchingSpanAsync(document, position, cancellationToken).WaitAndGetResult(cancellationToken);

                        if (matchingSpan.HasValue)
                        {
                            var resultsInView = textView.GetSpanInView(matchingSpan.Value.ToSnapshotSpan(subjectBuffer.CurrentSnapshot)).ToList();
                            if (resultsInView.Count == 1)
                            {
                                var vsTextSpan = resultsInView[0].ToVsTextSpan();

                                if (matchingSpan.Value.Start < position)
                                {
                                    pSpan[0].iStartLine = vsTextSpan.iStartLine;
                                    pSpan[0].iStartIndex = vsTextSpan.iStartIndex;
                                }
                                else if (matchingSpan.Value.End > position)
                                {
                                    pSpan[0].iEndLine = vsTextSpan.iEndLine;
                                    pSpan[0].iEndIndex = vsTextSpan.iEndIndex;
                                }
                            }
                        }
                    }
                }
            }

            return VSConstants.S_OK;
        }
    }
}
