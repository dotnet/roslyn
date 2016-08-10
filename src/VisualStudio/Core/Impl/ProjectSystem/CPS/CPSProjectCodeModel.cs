// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    internal sealed class CPSProjectCodeModel : AbstractProjectCodeModel
    {
        public CPSProjectCodeModel(CPSProject project, VisualStudioWorkspaceImpl visualStudioWorkspace, IServiceProvider serviceProvider)
            : base(project, visualStudioWorkspace, serviceProvider)
        {
        }

        internal override bool CanCreateFileCodeModelThroughProject(string filePath)
        {
            return GetProjectItem(filePath) != null;
        }

        internal override object CreateFileCodeModelThroughProject(string filePath)
        {
            var projectItem = GetProjectItem(filePath);
            if (projectItem == null)
            {
                return null;
            }

            var codeModelCache = GetCodeModelCache();
            return codeModelCache.GetOrCreateFileCodeModel(filePath, projectItem).Handle;
        }

        private EnvDTE.ProjectItem GetProjectItem(string filePath)
        {
            var codeModelCache = GetCodeModelCache();
            if (codeModelCache == null)
            {
                return null;
            }

            var dteProject = VisualStudioWorkspace.TryGetDTEProject(VSProject.Id);
            if (dteProject == null)
            {
                return null;
            }

            return dteProject.FindItemByPath(filePath, StringComparer.OrdinalIgnoreCase);
        }

        public EnvDTE.CodeModel GetCodeModel(EnvDTE.Project parent)
        {
            var codeModelCache = GetCodeModelCache();
            if (codeModelCache == null)
            {
                return null;
            }

            return codeModelCache.GetOrCreateRootCodeModel(parent);
        }

        public EnvDTE.FileCodeModel GetFileCodeModel(EnvDTE.ProjectItem item)
        {
            var codeModelCache = GetCodeModelCache();
            if (codeModelCache == null)
            {
                return null;
            }

            string filePath;
            if (!item.TryGetFullPath(out filePath))
            {
                return null;
            }

            return codeModelCache.GetOrCreateFileCodeModel(filePath, item).Handle;
        }
    }
}
