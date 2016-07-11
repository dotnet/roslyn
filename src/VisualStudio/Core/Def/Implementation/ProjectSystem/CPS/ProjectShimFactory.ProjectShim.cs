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
                Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreator,
                string projectName,
                IVsHierarchy hierarchy,
                string language,
                IServiceProvider serviceProvider,
                VisualStudioWorkspaceImpl visualStudioWorkspace,
                HostDiagnosticUpdateSource hostDiagnosticUpdateSource)
                : base(projectTracker, reportExternalErrorCreator, projectName, hierarchy, language, serviceProvider, visualStudioWorkspace, hostDiagnosticUpdateSource)
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
                catch (IOException ex) when (FatalError.ReportWithoutCrash(ex))
                {
                    return;
                }

                if (_lastOutputPath == null || !_lastOutputPath.Equals(outputPath, StringComparison.OrdinalIgnoreCase))
                {
                    SetOutputPathAndRelatedData(outputPath);
                    _lastOutputPath = outputPath;
                }
            }
        }
    }
}
