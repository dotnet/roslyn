// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A workspace that allows manual addition of projects and documents.
    /// </summary>
    public class CustomWorkspace : Workspace
    {
        public CustomWorkspace(HostServices host, string workspaceKind = "Custom")
            : base(host, workspaceKind)
        {
        }

        public CustomWorkspace()
            : this(Host.Mef.MefHostServices.DefaultHost)
        {
        }

        /// <summary>
        /// Clears all projects and documents from the workspace.
        /// </summary>
        public new void ClearSolution()
        {
            base.ClearSolution();
        }

        /// <summary>
        /// Adds an entire solution to the workspace, replacing any existing solution.
        /// </summary>
        public void AddSolution(SolutionInfo solutionInfo)
        {
            if (solutionInfo == null)
            {
                throw new ArgumentNullException("solutionInfo");
            }

            this.OnSolutionAdded(solutionInfo);
        }

        /// <summary>
        /// Adds a project to the workspace. All previous projects remain intact.
        /// </summary>
        public ProjectId AddProject(string name, string language)
        {
            var info = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), name, name, language);
            this.AddProject(info);
            return info.Id;
        }

        /// <summary>
        /// Adds a project to the workspace. All previous projects remain intact.
        /// </summary>
        public void AddProject(ProjectInfo projectInfo)
        {
            if (projectInfo == null)
            {
                throw new ArgumentNullException("projectInfo");
            }

            this.OnProjectAdded(projectInfo);
        }

        /// <summary>
        /// Adds multiple projects to the workspace at once. All existing projects remain intact.
        /// </summary>
        /// <param name="projectInfos"></param>
        public void AddProjects(IEnumerable<ProjectInfo> projectInfos)
        {
            if (projectInfos == null)
            {
                throw new ArgumentNullException("projectInfos");
            }

            foreach (var info in projectInfos)
            {
                this.OnProjectAdded(info);
            }
        }

        /// <summary>
        /// Adds a document to the workspace.
        /// </summary>
        public DocumentId AddDocument(ProjectId projectId, string name, string text)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException("projectId");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            var id = DocumentId.CreateNewId(projectId);

            this.AddDocument(
                DocumentInfo.Create(id, name, loader: TextLoader.From(TextAndVersion.Create(SourceText.From(text), VersionStamp.Create()))));

            return id;
        }

        /// <summary>
        /// Adds a document to the workspace.
        /// </summary>
        public void AddDocument(DocumentInfo documentInfo)
        {
            if (documentInfo == null)
            {
                throw new ArgumentNullException("documentInfo");
            }

            this.OnDocumentAdded(documentInfo);
        }
    }
}
