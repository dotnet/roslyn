// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal partial class ContainedLanguage : IVsContainedLanguage
    {
        public int GetColorizer(out IVsColorizer colorizer)
        {
            // We have no legacy colorizer, and so we'll return E_NOTIMPL to opt out of using one
            colorizer = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetLanguageServiceID(out Guid guidLangService)
        {
            guidLangService = _languageServiceGuid;
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

            textViewFilter = new VenusCommandFilter(wpfTextView, SubjectBuffer, nextCmdTarget, ComponentModel);

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

            if (field.GetValue(intellisenseHost) is not IVsTextView view)
            {
                return null;
            }

            return _editorAdaptersFactoryService.GetWpfTextView(view);
        }

        public int Refresh(uint refreshMode)
            => VSConstants.S_OK;

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
            => VSConstants.S_OK;
    }
}
