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
                // Set the bin output path so that this project can be hooked up to the project tracker.
                // Note that this method gets the bin output path from the hierarchy, null argument here just sets the object output path to null.
                SetOutputPathAndRelatedData(objOutputPath: null);

                projectTracker.AddProject(this);
            }

            protected override CommandLineArguments ParseCommandLineArguments(IEnumerable<string> arguments)
            {
                throw new NotSupportedException("We only support setting parsed command line arguments");
            }

            protected override void PostSetOptions()
            {
                base.PostSetOptions();

                // Project tracker tracks projects by bin path, so if the output path has been updated, we need to let it know.
                var commandLineArguments = base.GetParsedCommandLineArguments();
                string outputPath;

                try
                {
                    outputPath = Path.Combine(commandLineArguments.OutputDirectory, commandLineArguments.OutputFileName ?? base.ProjectSystemName);
                }
                catch (Exception ex) when (FatalError.ReportWithoutCrash(ex))
                {
                    return;
                }

                if (_lastOutputPath == null || !_lastOutputPath.Equals(outputPath, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.Workspace != null)
                    {
                        SetOutputPathAndRelatedData(outputPath);
                    }
                    else
                    {
                        // This can happen only in tests.
                        // For tests, we need to explicitly update the ProjectTracker bin paths as we don't have a VSWorkspace and
                        // SetOutputPathAndRelatedData (which updates the ProjectTracker bin paths for product code) will bail out early.
                        this.ProjectTracker.UpdateProjectBinPath(this, _lastOutputPath, outputPath);
                    }

                    _lastOutputPath = outputPath;
                }
            }
        }
    }
}
