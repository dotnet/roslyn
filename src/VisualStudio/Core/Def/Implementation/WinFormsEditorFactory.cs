// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.WinForms.Interfaces;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal class WinFormsEditorFactory
    {
        private static WinFormsEditorFactory? _instance;

        private WinFormsEditorFactory()
        {

        }

        public static WinFormsEditorFactory Instance
            => _instance ??= new WinFormsEditorFactory();

        public int CreateEditorInstance(
            IVsHierarchy vsHierarchy,
            uint itemid,
            OLE.Interop.IServiceProvider oleServiceProvider,
            IVsTextBuffer textBuffer,
            READONLYSTATUS readOnlyStatus,
            out IntPtr ppunkDocView,
            out string pbstrEditorCaption,
            out Guid pguidCmdUI)
        {
            ppunkDocView = IntPtr.Zero;
            pbstrEditorCaption = string.Empty;
            pguidCmdUI = Guid.Empty;

            var winFormsEditorFactory = (IWinFormsEditorFactory)PackageUtilities.QueryService<IWinFormsEditorFactory>(oleServiceProvider);

            if (winFormsEditorFactory is null)
            {
                return VSConstants.E_FAIL;
            }

            return winFormsEditorFactory.CreateEditorInstance(
                vsHierarchy,
                itemid,
                oleServiceProvider,
                textBuffer,
                readOnlyStatus,
                out ppunkDocView,
                out pbstrEditorCaption,
                out pguidCmdUI);
        }
    }
}
