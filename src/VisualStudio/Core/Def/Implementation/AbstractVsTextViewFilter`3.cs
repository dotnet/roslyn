// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal abstract class AbstractVsTextViewFilter<TPackage, TLanguageService, TProject> : AbstractVsTextViewFilter, IVsTextViewFilter, IVsReadOnlyViewNotification
        where TPackage : AbstractPackage<TPackage, TLanguageService, TProject>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService, TProject>
        where TProject : AbstractProject
    {
        private readonly AbstractLanguageService<TPackage, TLanguageService, TProject> _languageService;

        protected AbstractVsTextViewFilter(
            AbstractLanguageService<TPackage, TLanguageService, TProject> languageService,
            IWpfTextView wpfTextView,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            ICommandHandlerServiceFactory commandHandlerServiceFactory)
            : base(wpfTextView, commandHandlerServiceFactory, editorAdaptersFactoryService, languageService.SystemServiceProvider)
        {
            _languageService = languageService;
        }

        public virtual int GetDataTipText(TextSpan[] pSpan, out string pbstrText)
        {
            pbstrText = null;

            var debugInfo = _languageService.LanguageDebugInfo;
            if (debugInfo != null)
            {
                var subjectBuffer = WpfTextView.GetBufferContainingCaret();
                if (subjectBuffer == null)
                {
                    return VSConstants.E_FAIL;
                }

                var vsBuffer = this.EditorAdaptersFactory.GetBufferAdapter(subjectBuffer);

                // TODO: broken in REPL
                if (vsBuffer == null)
                {
                    return VSConstants.E_FAIL;
                }

                return debugInfo.GetDataTipText(vsBuffer, pSpan, pbstrText);
            }

            return VSConstants.E_FAIL;
        }

        public int GetPairExtents(int iLine, int iIndex, TextSpan[] pSpan)
        {
            int result = VSConstants.S_OK;
            _languageService.Package.ComponentModel.GetService<IWaitIndicator>().Wait(
                "Intellisense",
                allowCancel: true,
                action: c => result = GetPairExtentsWorker(iLine, iIndex, pSpan, c.CancellationToken));

            return result;
        }

        private int GetPairExtentsWorker(int iLine, int iIndex, TextSpan[] pSpan, CancellationToken cancellationToken)
        {
            var braceMatcher = _languageService.Package.ComponentModel.GetService<IBraceMatchingService>();
            return GetPairExtentsWorker(
                WpfTextView,
                _languageService.Workspace,
                braceMatcher,
                iLine,
                iIndex,
                pSpan,
                (VSConstants.VSStd2KCmdID)this.CurrentlyExecutingCommand == VSConstants.VSStd2KCmdID.GOTOBRACE_EXT,
                cancellationToken);
        }

        public int GetWordExtent(int iLine, int iIndex, uint dwFlags, TextSpan[] pSpan)
        {
            return VSConstants.E_NOTIMPL;
        }

        #region Edit and Continue 

        int IVsReadOnlyViewNotification.OnDisabledEditingCommand(ref Guid pguidCmdGuid, uint dwCmdId)
        {
            var container = GetSubjectBufferContainingCaret().AsTextContainer();

            Workspace workspace;
            if (!Workspace.TryGetWorkspace(container, out workspace))
            {
                return VSConstants.S_OK;
            }

            var vsWorkspace = workspace as VisualStudioWorkspaceImpl;
            if (vsWorkspace == null)
            {
                return VSConstants.S_OK;
            }

            foreach (var documentId in vsWorkspace.GetRelatedDocumentIds(container))
            {
                var hostProject = vsWorkspace.GetHostProject(documentId.ProjectId) as AbstractRoslynProject;
                if (hostProject?.EditAndContinueImplOpt != null)
                {
                    if (hostProject.EditAndContinueImplOpt.OnEdit(documentId))
                    {
                        break;
                    }
                }
            }

            return VSConstants.S_OK;
        }

        #endregion
    }
}
