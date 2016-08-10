// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    internal sealed partial class CPSProject : AbstractProject
    {
        private bool _lastDesignTimeBuildSucceeded;

        public CPSProject(
            VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectDisplayName,
            string projectFilePath,
            IVsHierarchy hierarchy,
            string language,
            Guid projectGuid,
            string commandLineForOptions,
            IServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt,
            ICommandLineParserService commandLineParserServiceOpt)
            : base(projectTracker, reportExternalErrorCreatorOpt, projectDisplayName, projectFilePath,
                   hierarchy, language, projectGuid, serviceProvider, visualStudioWorkspaceOpt, hostDiagnosticUpdateSourceOpt, commandLineParserServiceOpt)
        {
            // Initialize the options.
            SetCommandLineArguments(commandLineForOptions);

            // We need to ensure that the bin output path for the project has been initialized before we hookup the project with the project tracker.
            // If we were unable to set the output path from SetCommandLineArguments (due to null output file name or directory in the given commandLineForOptions),
            // we set a default unique output path.
            if (this.TryGetBinOutputPath() == null)
            {
                var uniqueDefaultOutputPath = PathUtilities.CombinePathsUnchecked(Path.GetTempPath(), projectDisplayName + projectGuid.GetHashCode().ToString());
                SetOutputPathAndRelatedData(objOutputPath: uniqueDefaultOutputPath, hasSameBinAndObjOutputPaths: true);
            }

            Contract.ThrowIfNull(this.TryGetBinOutputPath());

            // Now hook up the project to the project tracker.
            projectTracker.AddProject(this);

            _lastDesignTimeBuildSucceeded = true;
        }

        protected sealed override bool TryGetOutputPathFromHierarchy(out string binOutputPath)
        {
            throw new NotSupportedException();
        }

        protected sealed override bool LastDesignTimeBuildSucceeded => _lastDesignTimeBuildSucceeded;

        // We might we invoked from a background thread, so schedule the disconnect on foreground task scheduler.
        public sealed override void Disconnect()
        {
            InvokeBelowInputPriority(() =>
            {
                // clear code model cache and shutdown instances, if any exists.
                _projectCodeModel?.OnProjectClosed();

                base.Disconnect();
            });
        }
    }
}
