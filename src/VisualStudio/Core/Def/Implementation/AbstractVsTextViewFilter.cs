// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

internal abstract class AbstractVsTextViewFilter(
    IWpfTextView wpfTextView,
    IComponentModel componentModel) : AbstractOleCommandTarget(wpfTextView, componentModel), IVsTextViewFilter
{
    int IVsTextViewFilter.GetDataTipText(TextSpan[] pSpan, out string pbstrText)
    {
        (pbstrText, var result) = this.ThreadingContext.JoinableTaskFactory.Run(() => GetDataTipTextAsync(pSpan));
        return result;
    }

    private async Task<(string pbstrText, int result)> GetDataTipTextAsync(TextSpan[] pSpan)
    {
        try
        {
            if (pSpan == null || pSpan.Length != 1)
                return (null, VSConstants.E_INVALIDARG);

            return await GetDataTipTextImplAsync(pSpan).ConfigureAwait(true);
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e) && false)
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    protected virtual async Task<(string pbstrText, int result)> GetDataTipTextImplAsync(TextSpan[] pSpan)
    {
        var subjectBuffer = WpfTextView.GetBufferContainingCaret();
        if (subjectBuffer == null)
            return (null, VSConstants.E_FAIL);

        return await GetDataTipTextImplAsync(subjectBuffer, pSpan).ConfigureAwait(true);
    }

    protected async Task<(string pbstrText, int result)> GetDataTipTextImplAsync(ITextBuffer subjectBuffer, TextSpan[] pSpan)
    {
        var vsBuffer = EditorAdaptersFactory.GetBufferAdapter(subjectBuffer);

        // TODO: broken in REPL
        if (vsBuffer == null)
            return (null, VSConstants.E_FAIL);

        using (Logger.LogBlock(FunctionId.Debugging_VsLanguageDebugInfo_GetDataTipText, CancellationToken.None))
        {
            if (pSpan == null || pSpan.Length != 1)
                return (null, VSConstants.E_INVALIDARG);

            var result = VSConstants.E_FAIL;
            string pbstrText = null;

            var uiThreadOperationExecutor = ComponentModel.GetService<IUIThreadOperationExecutor>();
            using var context = uiThreadOperationExecutor.BeginExecute(
                title: ServicesVSResources.Debugger,
                defaultDescription: ServicesVSResources.Getting_DataTip_text,
                allowCancellation: true,
                showProgress: false);

            IServiceProvider serviceProvider = ComponentModel.GetService<SVsServiceProvider>();
            var debugger = (IVsDebugger)serviceProvider.GetService(typeof(SVsShellDebugger));
            var debugMode = new DBGMODE[1];

            var cancellationToken = context.UserCancellationToken;
            if (ErrorHandler.Succeeded(debugger.GetMode(debugMode)) && debugMode[0] != DBGMODE.DBGMODE_Design)
            {
                var textSpan = pSpan[0];

                var textSnapshot = subjectBuffer.CurrentSnapshot;
                var document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();

                if (document != null)
                {
                    var languageDebugInfo = document.Project.Services.GetService<ILanguageDebugInfoService>();
                    if (languageDebugInfo != null)
                    {
                        var spanOpt = textSnapshot.TryGetSpan(textSpan);
                        if (spanOpt.HasValue)
                        {
                            // 'kind' is an lsp-only concept, so we don't want/need to include it here (especially
                            // as it can be expensive to compute, and we don't want to block the UI thread).
                            var dataTipInfo = await languageDebugInfo.GetDataTipInfoAsync(
                                document, spanOpt.Value.Start, includeKind: false, cancellationToken).ConfigureAwait(true);
                            if (!dataTipInfo.IsDefault)
                            {
                                var resultSpan = dataTipInfo.Span.ToSnapshotSpan(textSnapshot);
                                var textOpt = dataTipInfo.Text;

                                pSpan[0] = resultSpan.ToVsTextSpan();
                                result = debugger.GetDataTipValue((IVsTextLines)vsBuffer, pSpan, textOpt, out pbstrText);
                            }
                        }
                    }
                }
            }

            return (pbstrText, result);
        }
    }

    int IVsTextViewFilter.GetPairExtents(int iLine, int iIndex, TextSpan[] pSpan)
    {
        return this.ThreadingContext.JoinableTaskFactory.Run(() => GetPairExtentsAsync(iLine, iIndex, pSpan));
    }

    private async Task<int> GetPairExtentsAsync(int iLine, int iIndex, TextSpan[] pSpan)
    {
        using var waitContext = ComponentModel.GetService<IUIThreadOperationExecutor>().BeginExecute(
            "Intellisense",
            defaultDescription: "",
            allowCancellation: true,
            showProgress: false);

        var braceMatcher = ComponentModel.GetService<IBraceMatchingService>();
        var globalOptions = ComponentModel.GetService<IGlobalOptionService>();

        return await GetPairExtentsAsync(
            WpfTextView,
            braceMatcher,
            globalOptions,
            iLine,
            iIndex,
            pSpan,
            (VSConstants.VSStd2KCmdID)this.CurrentlyExecutingCommand == VSConstants.VSStd2KCmdID.GOTOBRACE_EXT,
            waitContext.UserCancellationToken).ConfigureAwait(true);
    }

    // Internal for testing purposes
    internal static async Task<int> GetPairExtentsAsync(
        ITextView textView,
        IBraceMatchingService braceMatcher,
        IGlobalOptionService globalOptions,
        int iLine,
        int iIndex,
        TextSpan[] pSpan,
        bool extendSelection,
        CancellationToken cancellationToken)
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
                    var options = globalOptions.GetBraceMatchingOptions(document.Project.Language);
                    var matchingSpan = await braceMatcher.FindMatchingSpanAsync(
                        document, position, options, cancellationToken).ConfigureAwait(true);

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
                                    var closingSpans = await braceMatcher.FindMatchingSpanAsync(
                                        document, matchingSpan.Value.Start, options, cancellationToken).ConfigureAwait(true);
                                    var vsClosingSpans = textView.GetSpanInView(closingSpans.Value.ToSnapshotSpan(subjectBuffer.CurrentSnapshot)).First().ToVsTextSpan();
                                    pSpan[0].iEndIndex = vsClosingSpans.iStartIndex;
                                }

                                return VSConstants.S_OK;
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
                                    var openingSpans = await braceMatcher.FindMatchingSpanAsync(
                                        document, matchingSpan.Value.End, options, cancellationToken).ConfigureAwait(true);
                                    var vsOpeningSpans = textView.GetSpanInView(openingSpans.Value.ToSnapshotSpan(subjectBuffer.CurrentSnapshot)).First().ToVsTextSpan();
                                    pSpan[0].iStartIndex = vsOpeningSpans.iStartIndex;
                                }

                                return VSConstants.S_OK;
                            }
                        }
                    }
                }
            }
        }

        return VSConstants.S_FALSE;
    }

    int IVsTextViewFilter.GetWordExtent(int iLine, int iIndex, uint dwFlags, TextSpan[] pSpan)
        => VSConstants.E_NOTIMPL;
}
