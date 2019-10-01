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
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IServiceProvider systemServiceProvider)
            : base(wpfTextView, editorAdaptersFactoryService, systemServiceProvider)
        {
        }

        // Internal for testing purposes
        internal static int GetPairExtentsWorker(ITextView textView, CodeAnalysis.Workspace workspace, IBraceMatchingService braceMatcher, int iLine, int iIndex, TextSpan[] pSpan, bool extendSelection, CancellationToken cancellationToken)
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

                                // caret is at close parenthesis
                                if (matchingSpan.Value.Start < position)
                                {
                                    pSpan[0].iStartLine = vsTextSpan.iStartLine;
                                    pSpan[0].iStartIndex = vsTextSpan.iStartIndex;

                                    // For text selection using goto matching brace, tweak spans to suit the VS editor's behavior.
                                    // The vs editor sets selection for GotoBraceExt (Ctrl + Shift + ]) like so :
                                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                    // if (fExtendSelection)
                                    // {
                                    //      textSpan.iEndIndex++;
                                    //      this.SetSelection(textSpan.iStartLine, textSpan.iStartIndex, textSpan.iEndLine, textSpan.iEndIndex);
                                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                    // Notice a couple of things: it arbitrarily increments EndIndex by 1 and does nothing similar for StartIndex.
                                    // So, if we're extending selection: 
                                    //    case a: set EndIndex to left of closing parenthesis -- ^}
                                    //            this adjustment is for any of the four cases where caret could be. left or right of open or close parenthesis -- ^{^ ^}^
                                    //    case b: set StartIndex to left of opening parenthesis -- ^{
                                    //            this adjustment is for cases where caret was originally to the right of the open parenthesis -- {^ }

                                    // if selecting, adjust end position by using the matching opening span that we just computed.
                                    if (extendSelection)
                                    {
                                        // case a.
                                        var closingSpans = braceMatcher.FindMatchingSpanAsync(document, matchingSpan.Value.Start, cancellationToken).WaitAndGetResult(cancellationToken);
                                        var vsClosingSpans = textView.GetSpanInView(closingSpans.Value.ToSnapshotSpan(subjectBuffer.CurrentSnapshot)).ToList().First().ToVsTextSpan();
                                        pSpan[0].iEndIndex = vsClosingSpans.iStartIndex;
                                    }
                                }
                                else if (matchingSpan.Value.End > position) // caret is at open parenthesis
                                {
                                    pSpan[0].iEndLine = vsTextSpan.iEndLine;
                                    pSpan[0].iEndIndex = vsTextSpan.iEndIndex;

                                    // if selecting, adjust start position by using the matching closing span that we computed
                                    if (extendSelection)
                                    {
                                        // case a.
                                        pSpan[0].iEndIndex = vsTextSpan.iStartIndex;

                                        // case b.
                                        var openingSpans = braceMatcher.FindMatchingSpanAsync(document, matchingSpan.Value.End, cancellationToken).WaitAndGetResult(cancellationToken);
                                        var vsOpeningSpans = textView.GetSpanInView(openingSpans.Value.ToSnapshotSpan(subjectBuffer.CurrentSnapshot)).ToList().First().ToVsTextSpan();
                                        pSpan[0].iStartIndex = vsOpeningSpans.iStartIndex;
                                    }
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
