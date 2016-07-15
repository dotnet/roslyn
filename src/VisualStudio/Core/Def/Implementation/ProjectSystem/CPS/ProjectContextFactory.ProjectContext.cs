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
    internal partial class ProjectContextFactory
    {
        private sealed partial class ProjectContext : AbstractProject
        {
            public ProjectContext(
                CommandLineArguments commandLineArguments,
                VisualStudioProjectTracker projectTracker,
                Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
                string projectDisplayName,
                string projectFilePath,
                Guid projectGuid,
                string projectTypeGuid,
                IVsHierarchy hierarchy,
                string language,
                IServiceProvider serviceProvider,
                VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
                HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt)
                : base(projectTracker, reportExternalErrorCreatorOpt, projectDisplayName, projectFilePath, projectGuid,
                       projectTypeGuid, hierarchy, language, serviceProvider, visualStudioWorkspaceOpt, hostDiagnosticUpdateSourceOpt)
            {
                // Set the initial options from the command line before we add the project to the project tracker.
                SetCommandLineArguments(commandLineArguments);

                projectTracker.AddProject(this);
            }

            protected override CommandLineArguments ParseCommandLineArguments(IEnumerable<string> arguments)
            {
                throw new NotSupportedException();
            }

            protected sealed override bool TryGetOutputPathFromHierarchy(out string binOutputPath)
            {
                throw new NotSupportedException();
            }

            // TODO: CPS should push design time build status for every design time build.
            protected sealed override bool DesignTimeBuildStatus => true;

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
