// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class ProjectShimFactory
    {
        private sealed partial class ProjectShim : AbstractRoslynProject
        {
            private string _lastOutputPath;

            public ProjectShim(
                CommandLineArguments commandLineArguments,
                VisualStudioProjectTracker projectTracker,
                Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
                string projectName,
                IVsHierarchy hierarchy,
                string language,
                IServiceProvider serviceProvider,
                VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
                HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt)
                : base(projectTracker, reportExternalErrorCreatorOpt, projectName, hierarchy, language, serviceProvider, visualStudioWorkspaceOpt, hostDiagnosticUpdateSourceOpt)
            {
                // Set the initial options from the command line before we add the project to the project tracker.
                SetCommandLineArguments(commandLineArguments);

                projectTracker.AddProject(this);
            }

            protected override CommandLineArguments ParseCommandLineArguments(IEnumerable<string> arguments)
            {
                throw new NotSupportedException("We only support setting parsed command line arguments");
            }

            protected override void PostSetOptions()
            {
                base.PostSetOptions();

                // Invoke SetOutputPathAndRelatedData to update the project tracker bin path for this project.
                string outputPath;
                if (!base.TryGetOutputPathFromBuildManager(out outputPath))
                {
                    // This can happen for tests.
                    outputPath = null;
                }

                if (_lastOutputPath == null || outputPath != null && !_lastOutputPath.Equals(outputPath, StringComparison.OrdinalIgnoreCase))
                {
                    SetOutputPathAndRelatedData(outputPath);
                    _lastOutputPath = outputPath;
                }
            }
        }
    }
}
