// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal abstract partial class AbstractRoslynProject : AbstractProject
    {
        internal VsENCRebuildableProjectImpl EditAndContinueImplOpt;

        public AbstractRoslynProject(
            VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            IVsHierarchy hierarchy,
            string language,
            IServiceProvider serviceProvider,
            MiscellaneousFilesWorkspace miscellaneousFilesWorkspaceOpt,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt)
            : base(projectTracker, reportExternalErrorCreatorOpt, projectSystemName, hierarchy, language, serviceProvider, miscellaneousFilesWorkspaceOpt, visualStudioWorkspaceOpt, hostDiagnosticUpdateSourceOpt)
        {
            if (visualStudioWorkspaceOpt != null)
            {
                this.EditAndContinueImplOpt = new VsENCRebuildableProjectImpl(this);
            }
        }

        public override void Disconnect()
        {
            // project is going away
            this.EditAndContinueImplOpt = null;

            base.Disconnect();
        }
    }
}
