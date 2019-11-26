// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Razor
{
    [Export]
    internal class CSharpLspRazorProjectFactory
    {
        private readonly RemoteLanguageServiceWorkspaceHost _remoteLanguageServiceWorkspaceHost;
        private readonly Dictionary<string, ProjectId> _projects = new Dictionary<string, ProjectId>();

        public ProjectId GetProject(string projectName)
        {
            if (_projects.TryGetValue(projectName, out var projectId))
            {
                return projectId;
            }

            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(projectName), VersionStamp.Default, projectName, projectName, StringConstants.CSharpLspLanguageName);

            _remoteLanguageServiceWorkspaceHost.Workspace.OnManagedProjectAdded(projectInfo);

            _projects.Add(projectName, projectInfo.Id);

            return projectInfo.Id;
        }

        public Workspace Workspace => _remoteLanguageServiceWorkspaceHost.Workspace;

        [ImportingConstructor]
        public CSharpLspRazorProjectFactory(RemoteLanguageServiceWorkspaceHost remoteLanguageServiceWorkspaceHost)
        {
            _remoteLanguageServiceWorkspaceHost = remoteLanguageServiceWorkspaceHost ?? throw new ArgumentNullException(nameof(remoteLanguageServiceWorkspaceHost));
        }
    }
}
