// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    internal partial class CSharpLanguageService : ICSharpProjectHost
    {
        public void BindToProject(ICSharpProjectRoot projectRoot, IVsHierarchy hierarchy)
        {
            var projectName = Path.GetFileName(projectRoot.GetFullProjectName()); // GetFullProjectName returns the path to the project file w/o the extension?

            var project = new CSharpProjectShim(
                projectRoot,
                projectName,
                hierarchy,
                this.SystemServiceProvider,
                this.Package.ComponentModel.GetService<IThreadingContext>());

            projectRoot.SetProjectSite(project);
        }
    }
}
