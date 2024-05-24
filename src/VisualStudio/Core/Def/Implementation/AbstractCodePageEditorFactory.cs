// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.Shell.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

internal abstract class AbstractCodePageEditorFactory : IVsEditorFactory
{
    private readonly AbstractEditorFactory _editorFactory;

    protected AbstractCodePageEditorFactory(AbstractEditorFactory editorFactory)
        => _editorFactory = editorFactory;

    int IVsEditorFactory.CreateEditorInstance(
        uint grfCreateDoc,
        string pszMkDocument,
        string pszPhysicalView,
        IVsHierarchy vsHierarchy,
        uint itemid,
        IntPtr punkDocDataExisting,
        out IntPtr ppunkDocView,
        out IntPtr ppunkDocData,
        out string pbstrEditorCaption,
        out Guid pguidCmdUI,
        out int pgrfCDW)
    {
        if (punkDocDataExisting != IntPtr.Zero)
        {
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pbstrEditorCaption = null;
            pguidCmdUI = Guid.Empty;
            pgrfCDW = 0;

            return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
        }

        _editorFactory.SetEncoding(true);
        try
        {
            return _editorFactory.CreateEditorInstance(
                grfCreateDoc, pszMkDocument, pszPhysicalView, vsHierarchy, itemid,
                punkDocDataExisting, out ppunkDocView, out ppunkDocData,
                out pbstrEditorCaption, out pguidCmdUI, out pgrfCDW);
        }
        finally
        {
            _editorFactory.SetEncoding(false);
        }
    }

    int IVsEditorFactory.MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
        => _editorFactory.MapLogicalView(ref rguidLogicalView, out pbstrPhysicalView);

    int IVsEditorFactory.SetSite(IOleServiceProvider psp)
        => VSConstants.S_OK;

    int IVsEditorFactory.Close()
        => VSConstants.S_OK;
}
