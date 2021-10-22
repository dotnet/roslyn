// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices.ProjectInfoService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectInfoService
{
    internal sealed class DefaultProjectInfoService : IProjectInfoService
    {
        public bool GeneratedTypesMustBePublic(Project project)
        {
            if (project.Solution.Workspace is not VisualStudioWorkspaceImpl)
            {
                return false;
            }

            // TODO: reimplement

            return false;
        }
    }
}
