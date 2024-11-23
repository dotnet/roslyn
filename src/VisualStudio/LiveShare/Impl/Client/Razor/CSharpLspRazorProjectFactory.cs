// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Razor
{
    [Export]
    internal class CSharpLspRazorProjectFactory
    {
        private readonly RemoteLanguageServiceWorkspaceHost _remoteLanguageServiceWorkspaceHost;
        private readonly Dictionary<string, ProjectId> _projects = [];

        public ProjectId GetProject(string projectName)
        {
            if (_projects.TryGetValue(projectName, out var projectId))
            {
                return projectId;
            }

            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(projectName), VersionStamp.Default, projectName, projectName, LanguageNames.CSharp);

            _remoteLanguageServiceWorkspaceHost.Workspace.OnProjectAdded(projectInfo);

            _projects.Add(projectName, projectInfo.Id);

            return projectInfo.Id;
        }

        public CodeAnalysis.Workspace Workspace => _remoteLanguageServiceWorkspaceHost.Workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpLspRazorProjectFactory(RemoteLanguageServiceWorkspaceHost remoteLanguageServiceWorkspaceHost)
            => _remoteLanguageServiceWorkspaceHost = remoteLanguageServiceWorkspaceHost ?? throw new ArgumentNullException(nameof(remoteLanguageServiceWorkspaceHost));
    }
}
