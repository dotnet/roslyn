﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis;
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

            var projectTracker = Workspace.GetProjectTrackerAndInitializeIfNecessary(SystemServiceProvider);

            var project = new CSharpProjectShim(
                projectRoot,
                projectTracker,
                id => new ProjectExternalErrorReporter(id, "CS", this.SystemServiceProvider),
                projectName,
                hierarchy,
                this.SystemServiceProvider,
                this.Workspace,
                this.HostDiagnosticUpdateSource,
                this.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<ICommandLineParserService>());

            projectRoot.SetProjectSite(project);
        }
    }
}
