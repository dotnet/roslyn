// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Interactive
{
    // TODO (tomat): this needs to be polished and tested
    internal static class CommonVsUtils
    {
        internal const string OutputWindowId = "34e76e81-ee4a-11d0-ae2e-00a0c90fffc3";

        internal static string GetFilePath(ITextBuffer textBuffer)
        {
            if (textBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var textDocument))
            {
                return textDocument.FilePath;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the current IWpfTextView that is the active document.
        /// </summary>
        /// <returns></returns>
        public static IWpfTextView GetActiveTextView()
        {
            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
            if (monitorSelection == null)
            {
                return null;
            }

            if (ErrorHandler.Failed(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out var curDocument)))
            {
                // TODO: Report error
                return null;
            }

            if (!(curDocument is IVsWindowFrame frame))
            {
                // TODO: Report error
                return null;
            }

            if (ErrorHandler.Failed(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var docView)))
            {
                // TODO: Report error
                return null;
            }

            if (docView is IVsCodeWindow)
            {
                if (ErrorHandler.Failed(((IVsCodeWindow)docView).GetPrimaryView(out var textView)))
                {
                    // TODO: Report error
                    return null;
                }

                var model = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                var adapterFactory = model.GetService<IVsEditorAdaptersFactoryService>();
                var wpfTextView = adapterFactory.GetWpfTextView(textView);
                return wpfTextView;
            }

            return null;
        }
    }
}
