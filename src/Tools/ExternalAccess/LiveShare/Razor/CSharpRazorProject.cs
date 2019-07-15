// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Linq;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Razor
{
    [Export]
    internal class CSharpLspRazorProject
    {
        private readonly SVsServiceProvider _serviceProvider;
        private readonly RemoteLanguageServiceWorkspaceHost _remoteLanguageServiceWorkspaceHost;

#pragma warning disable CS0618 // Type or member is obsolete - used for liveshare.
        public AbstractProject GetProject(string projectName)
        {
            var projectTracker = _remoteLanguageServiceWorkspaceHost.ProjectTracker;
            var project = projectTracker.ImmutableProjects.FirstOrDefault(p => p.ProjectSystemName == projectName);

            if (project != null)
            {
                return project;
            }

            project = new CSharpLspProject(projectTracker, null, projectName, projectName, null, StringConstants.CSharpLspLanguageName, Guid.NewGuid(), _serviceProvider, null, null);
            projectTracker.AddProject(project);
            return project;
        }
#pragma warning restore CS0618 // Type or member is obsolete - used for liveshare.

        [ImportingConstructor]
        public CSharpLspRazorProject(SVsServiceProvider serviceProvider, RemoteLanguageServiceWorkspaceHost remoteLanguageServiceWorkspaceHost)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _remoteLanguageServiceWorkspaceHost = remoteLanguageServiceWorkspaceHost ?? throw new ArgumentNullException(nameof(remoteLanguageServiceWorkspaceHost));
        }
    }

#pragma warning disable CS0618 // Type or member is obsolete
    internal class CSharpLspProject : AbstractProject
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public CSharpLspProject(VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            string projectFilePath,
            IVsHierarchy hierarchy,
            string language,
            Guid projectGuid,
            IServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt,
            ICommandLineParserService commandLineParserServiceOpt = null)
            : base(projectTracker, reportExternalErrorCreatorOpt, projectSystemName, projectFilePath, hierarchy, language, projectGuid, serviceProvider, visualStudioWorkspaceOpt, hostDiagnosticUpdateSourceOpt, commandLineParserServiceOpt)
        {
        }
    }
}
