// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
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
        protected AbstractLanguageService<TPackage, TLanguageService> LanguageService { get; }

        protected AbstractVsTextViewFilter(
            AbstractLanguageService<TPackage, TLanguageService> languageService,
            IWpfTextView wpfTextView,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(wpfTextView, editorAdaptersFactoryService, languageService.SystemServiceProvider)
        {
            LanguageService = languageService;
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

                var debugInfo = LanguageService.LanguageDebugInfo;
                if (debugInfo == null)
                {
                    pbstrText = null;
                    return VSConstants.E_FAIL;
                }

                return GetDataTipTextImpl(pSpan, debugInfo, out pbstrText);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e) && false)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected virtual int GetDataTipTextImpl(TextSpan[] pSpan, AbstractLanguageService<TPackage, TLanguageService>.VsLanguageDebugInfo debugInfo, out string pbstrText)
        {
            var subjectBuffer = WpfTextView.GetBufferContainingCaret();
            if (subjectBuffer == null)
            {
                pbstrText = null;
                return VSConstants.E_FAIL;
            }

            return GetDataTipTextImpl(subjectBuffer, pSpan, debugInfo, out pbstrText);
        }

        protected int GetDataTipTextImpl(ITextBuffer subjectBuffer, TextSpan[] pSpan, AbstractLanguageService<TPackage, TLanguageService>.VsLanguageDebugInfo debugInfo, out string pbstrText)
        {
            pbstrText = null;

            var vsBuffer = EditorAdaptersFactory.GetBufferAdapter(subjectBuffer);

            // TODO: broken in REPL
            if (vsBuffer == null)
            {
                return VSConstants.E_FAIL;
            }

            return debugInfo.GetDataTipText(vsBuffer, pSpan, out pbstrText);
        }

        int IVsTextViewFilter.GetPairExtents(int iLine, int iIndex, TextSpan[] pSpan)
        {
            try
            {
                var result = VSConstants.S_OK;
                LanguageService.Package.ComponentModel.GetService<IWaitIndicator>().Wait(
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
            var braceMatcher = LanguageService.Package.ComponentModel.GetService<IBraceMatchingService>();
            return GetPairExtentsWorker(
                WpfTextView,
                LanguageService.Workspace,
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
