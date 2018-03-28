// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    internal sealed partial class CPSProject
    {
        public EnvDTE.CodeModel GetCodeModel(EnvDTE.Project parent)
        {
            return ProjectCodeModel.GetOrCreateRootCodeModel(parent);
        }

        public EnvDTE.FileCodeModel GetFileCodeModel(EnvDTE.ProjectItem item)
        {
            if (!item.TryGetFullPath(out var filePath))
            {
                return null;
            }

            return ProjectCodeModel.GetOrCreateFileCodeModel(filePath, item);
        }

        private class CPSCodeModelInstanceFactory : ICodeModelInstanceFactory
        {
            private CPSProject _project;

            public CPSCodeModelInstanceFactory(CPSProject project)
            {
                _project = project;
            }

            EnvDTE.FileCodeModel ICodeModelInstanceFactory.TryCreateFileCodeModelThroughProjectSystem(string filePath)
            {
                var projectItem = GetProjectItem(filePath);
                if (projectItem == null)
                {
                    return null;
                }

                return _project.ProjectCodeModel.GetOrCreateFileCodeModel(filePath, projectItem);
            }

            private EnvDTE.ProjectItem GetProjectItem(string filePath)
            {
                var dteProject = ((VisualStudioWorkspaceImpl)_project.Workspace).TryGetDTEProject(_project.Id);
                if (dteProject == null)
                {
                    return null;
                }

                return dteProject.FindItemByPath(filePath, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
