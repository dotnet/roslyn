// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal partial class ContainedLanguage<TPackage, TLanguageService> : IVsContainedLanguage
    {
        public int GetColorizer(out IVsColorizer colorizer)
        {
            // We have no legacy colorizer, and so we'll return E_NOTIMPL to opt out of using one
            colorizer = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetLanguageServiceID(out Guid guidLangService)
        {
            guidLangService = _languageService.LanguageServiceId;
            return VSConstants.S_OK;
        }

        public int GetTextViewFilter(
            IVsIntellisenseHost intellisenseHost,
            IOleCommandTarget nextCmdTarget,
            out IVsTextViewFilter textViewFilter)
        {
            var wpfTextView = GetViewFromIVsIntellisenseHost(intellisenseHost);

            if (wpfTextView == null)
            {
                textViewFilter = null;
                return VSConstants.E_FAIL;
            }

            var commandHandlerServiceFactory = ComponentModel.GetService<ICommandHandlerServiceFactory>();
            textViewFilter = new VenusCommandFilter<TPackage, TLanguageService>(_languageService, wpfTextView, commandHandlerServiceFactory, SubjectBuffer, nextCmdTarget, _editorAdaptersFactoryService);

            return VSConstants.S_OK;
        }

        private IWpfTextView GetViewFromIVsIntellisenseHost(IVsIntellisenseHost intellisenseHost)
        {
            // The easiest way (unfortunately) is to get do reflection to get the view from the IVsIntellisenseHost.
            // In practice, the implementations we care about of this are just shim implementations from the editor.
            // The only alternative way to do this is to do very complicated watching of ITextView and IVsTextView
            // lifetimes to correlate them, but that requires running code in those code paths for all views which
            // seems a bit overkill for our needs.
            var field = intellisenseHost.GetType().GetField("_simpleTextViewWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                return null;
            }

            var view = field.GetValue(intellisenseHost) as IVsTextView;
            if (view == null)
            {
                return null;
            }

            return _editorAdaptersFactoryService.GetWpfTextView(view);
        }

        public int Refresh(uint refreshMode)
        {
            return VSConstants.S_OK;
        }

        public int SetBufferCoordinator(IVsTextBufferCoordinator pBC)
        {
            BufferCoordinator = pBC;
            return VSConstants.S_OK;
        }

        public int SetHost(IVsContainedLanguageHost host)
        {
            if (ContainedDocument.ContainedLanguageHost == host)
            {
                return VSConstants.S_OK;
            }

            ContainedDocument.ContainedLanguageHost = host;

            // Are we going away due to the contained language being disconnected?
            if (host == null)
            {
                OnDisconnect();
            }

            return VSConstants.S_OK;
        }

        public int WaitForReadyState()
        {
            return VSConstants.S_OK;
        }
    }
}
