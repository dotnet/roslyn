// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices.ProjectInfoService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectInfoService
{
    internal sealed class DefaultProjectInfoService : IProjectInfoService
    {
        public bool GeneratedTypesMustBePublic(Project project)
        {
            if (!(project.Solution.Workspace is VisualStudioWorkspaceImpl workspace))
            {
                return false;
            }

            // TODO: reimplement

            return false;
        }
    }
}
