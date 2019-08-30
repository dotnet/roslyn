// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using VsServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService> : IVsContainedLanguageFactory
    {
        private VisualStudioProject FindMatchingProject(IVsHierarchy hierarchy, uint itemid)
        {
            // Here we must determine the project that this file's document is to be a part of.
            // Venus creates a separate Project for a .aspx or .ascx file, and so we must associate
            // the document with that Project. We first query through a Venus-specific interface,
            // and if that fails we then use a general interface which is provided for non-Venus
            // contained language hosts (such as workflow editors.) This ordering is critical: in
            // Sharepoint projects (which are flavored workflow projects), we must prefer the
            // item-specific answer given to us from Venus rather than the project-level answer
            // given, which are going to be different. This was changed for Dev10 bug 839428.
            string projectName = null;
            if (this.SystemServiceProvider.GetService(typeof(SWebApplicationCtxSvc)) is IWebApplicationCtxSvc webApplicationCtxSvc)
            {
                if (webApplicationCtxSvc.GetItemContext(hierarchy, itemid, out var webServiceProvider) >= 0)
                {
                    var webFileCtxServiceGuid = typeof(IWebFileCtxService).GUID;
                    var service = IntPtr.Zero;
                    if (webServiceProvider.QueryService(ref webFileCtxServiceGuid, ref webFileCtxServiceGuid, out service) >= 0)
                    {
                        try
                        {
                            var webFileCtxService = Marshal.GetObjectForIUnknown(service) as IWebFileCtxService;
                            webFileCtxService.GetIntellisenseProjectName(out projectName);
                        }
                        finally
                        {
                            if (service != IntPtr.Zero)
                            {
                                Marshal.Release(service);
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(projectName))
            {
                if (hierarchy is IVsContainedLanguageProjectNameProvider containedLanguageProjectNameProvider)
                {
                    containedLanguageProjectNameProvider.GetProjectName(itemid, out projectName);
                }
            }

            if (string.IsNullOrEmpty(projectName))
            {
                return null;
            }

            return this.Workspace.GetProjectWithHierarchyAndName(hierarchy, projectName);
        }

        public int GetLanguage(IVsHierarchy hierarchy, uint itemid, IVsTextBufferCoordinator bufferCoordinator, out IVsContainedLanguage language)
        {
            var project = FindMatchingProject(hierarchy, itemid);
            if (project == null)
            {
                language = null;
                return VSConstants.E_INVALIDARG;
            }

            language = CreateContainedLanguage(bufferCoordinator, project, hierarchy, itemid);

            return VSConstants.S_OK;
        }
    }
}
