// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal abstract class AbstractVsTextViewFilter<TPackage, TLanguageService> : AbstractVsTextViewFilter, IVsTextViewFilter
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        protected AbstractVsTextViewFilter(
            IWpfTextView wpfTextView,
            IComponentModel componentModel)
            : base(wpfTextView, componentModel)
        {
        }

        int IVsTextViewFilter.GetDataTipText(TextSpan[] pSpan, out string pbstrText)
        {
            try
            {
                if (pSpan == null || pSpan.Length != 1)
                {
                    pbstrText = null;
                    return VSConstants.E_INVALIDARG;
                }

                return GetDataTipTextImpl(pSpan, out pbstrText);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e) && false)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected virtual int GetDataTipTextImpl(TextSpan[] pSpan, out string pbstrText)
        {
            var subjectBuffer = WpfTextView.GetBufferContainingCaret();
            if (subjectBuffer == null)
            {
                pbstrText = null;
                return VSConstants.E_FAIL;
            }

            return GetDataTipTextImpl(subjectBuffer, pSpan, out pbstrText);
        }

        protected int GetDataTipTextImpl(ITextBuffer subjectBuffer, TextSpan[] pSpan, out string pbstrText)
        {
            pbstrText = null;

            var vsBuffer = EditorAdaptersFactory.GetBufferAdapter(subjectBuffer);

            // TODO: broken in REPL
            if (vsBuffer == null)
            {
                return VSConstants.E_FAIL;
            }

            using (Logger.LogBlock(FunctionId.Debugging_VsLanguageDebugInfo_GetDataTipText, CancellationToken.None))
            {
                pbstrText = null;
                if (pSpan == null || pSpan.Length != 1)
                {
                    return VSConstants.E_INVALIDARG;
                }

                var result = VSConstants.E_FAIL;
                string pbstrTextInternal = null;

                var waitIndicator = ComponentModel.GetService<IWaitIndicator>();

                waitIndicator.Wait(
                    title: ServicesVSResources.Debugger,
                    message: ServicesVSResources.Getting_DataTip_text,
                    allowCancel: true,
                    action: waitContext =>
                {
                    IServiceProvider serviceProvider = ComponentModel.GetService<SVsServiceProvider>();
                    var debugger = (IVsDebugger)serviceProvider.GetService(typeof(SVsShellDebugger));
                    var debugMode = new DBGMODE[1];

                    var cancellationToken = waitContext.CancellationToken;
                    if (ErrorHandler.Succeeded(debugger.GetMode(debugMode)) && debugMode[0] != DBGMODE.DBGMODE_Design)
                    {
                        var textSpan = pSpan[0];

                        var textSnapshot = subjectBuffer.CurrentSnapshot;
                        var document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();

                        if (document != null)
                        {
                            var languageDebugInfo = document.Project.LanguageServices.GetService<ILanguageDebugInfoService>();
                            if (languageDebugInfo != null)
                            {
                                var spanOpt = textSnapshot.TryGetSpan(textSpan);
                                if (spanOpt.HasValue)
                                {
                                    var dataTipInfo = languageDebugInfo.GetDataTipInfoAsync(document, spanOpt.Value.Start, cancellationToken).WaitAndGetResult(cancellationToken);
                                    if (!dataTipInfo.IsDefault)
                                    {
                                        var resultSpan = dataTipInfo.Span.ToSnapshotSpan(textSnapshot);
                                        var textOpt = dataTipInfo.Text;

                                        pSpan[0] = resultSpan.ToVsTextSpan();
                                        result = debugger.GetDataTipValue((IVsTextLines)vsBuffer, pSpan, textOpt, out pbstrTextInternal);
                                    }
                                }
                            }
                        }
                    }
                });

                pbstrText = pbstrTextInternal;
                return result;
            }
        }

        int IVsTextViewFilter.GetPairExtents(int iLine, int iIndex, TextSpan[] pSpan)
        {
            try
            {
                var result = VSConstants.S_OK;
                ComponentModel.GetService<IWaitIndicator>().Wait(
                    "Intellisense",
                    allowCancel: true,
                    action: c => result = GetPairExtentsWorker(iLine, iIndex, pSpan, c.CancellationToken));

                return result;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e) && false)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private int GetPairExtentsWorker(int iLine, int iIndex, TextSpan[] pSpan, CancellationToken cancellationToken)
        {
            var braceMatcher = ComponentModel.GetService<IBraceMatchingService>();
            return GetPairExtentsWorker(
                WpfTextView,
                braceMatcher,
                iLine,
                iIndex,
                pSpan,
                (VSConstants.VSStd2KCmdID)this.CurrentlyExecutingCommand == VSConstants.VSStd2KCmdID.GOTOBRACE_EXT,
                cancellationToken);
        }

        int IVsTextViewFilter.GetWordExtent(int iLine, int iIndex, uint dwFlags, TextSpan[] pSpan)
            => VSConstants.E_NOTIMPL;
    }
}
