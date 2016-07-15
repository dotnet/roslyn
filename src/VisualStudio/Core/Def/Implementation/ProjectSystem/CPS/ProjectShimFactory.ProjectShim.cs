// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class ProjectShimFactory
    {
        private sealed partial class ProjectShim : AbstractRoslynProject
        {
            public ProjectShim(
                CommandLineArguments commandLineArguments,
                VisualStudioProjectTracker projectTracker,
                Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
                IVsHierarchy hierarchy,
                string language,
                IServiceProvider serviceProvider,
                VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
                HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt,
                string projectFilePath,
                Guid projectGuid)
                : base(projectTracker, reportExternalErrorCreatorOpt, GetProjectDisplayName(projectFilePath), hierarchy, language, serviceProvider,
                      visualStudioWorkspaceOpt, hostDiagnosticUpdateSourceOpt, projectFilePath, projectGuid, isWebsiteProject: false, connectHierarchyEvents: false)
            {
                // Set the initial options from the command line before we add the project to the project tracker.
                SetCommandLineArguments(commandLineArguments);

                projectTracker.AddProject(this);
            }

            private static string GetProjectDisplayName(string projectFilePath)
            {
                return PathUtilities.GetFileName(projectFilePath, includeExtension: false);
            }

            protected override CommandLineArguments ParseCommandLineArguments(IEnumerable<string> arguments)
            {
                throw new NotSupportedException("We only support setting parsed command line arguments");
            }

            protected override void PostSetOptions()
            {
                base.PostSetOptions();

                // If outputPath has changed, then invoke SetOutputPathAndRelatedData to update the project tracker bin path for this project.
                var commandLineArguments = GetParsedCommandLineArguments();
                if (commandLineArguments.OutputFileName != null && commandLineArguments.OutputDirectory != null)
                {
                    var newOutputPath = PathUtilities.CombinePathsUnchecked(commandLineArguments.OutputDirectory, commandLineArguments.OutputFileName);
                    SetOutputPathAndRelatedData(newOutputPath, hasSameBinAndObjOutputPaths: true);
                }
            }
        }
    }
}
