// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    internal partial class CSharpProjectShimWithServices : CSharpProjectShim
    {
        public CSharpProjectShimWithServices(
            ICSharpProjectRoot projectRoot,
            VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            IVsHierarchy hierarchy,
            IServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt,
            ICommandLineParserService commandLineParserServiceOpt)
            : base(
                  projectRoot,
                  projectTracker,
                  reportExternalErrorCreatorOpt,
                  projectSystemName,
                  hierarchy,
                  serviceProvider,
                  visualStudioWorkspaceOpt,
                  hostDiagnosticUpdateSourceOpt,
                  commandLineParserServiceOpt)
        {
            ProjectCodeModel = new ProjectCodeModel(this.Id, this, (VisualStudioWorkspaceImpl)this.Workspace, ServiceProvider);
        }
    }
}
