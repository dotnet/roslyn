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
        public Solution AddSolution(SolutionInfo solutionInfo)
        {
            if (solutionInfo == null)
            {
                throw new ArgumentNullException("solutionInfo");
            }

            this.OnSolutionAdded(solutionInfo);

            return this.CurrentSolution;
        }

        /// <summary>
        /// Adds a project to the workspace. All previous projects remain intact.
        /// </summary>
        public Project AddProject(string name, string language)
        {
            var info = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), name, name, language);
            return this.AddProject(info);
        }

        /// <summary>
        /// Adds a project to the workspace. All previous projects remain intact.
        /// </summary>
        public Project AddProject(ProjectInfo projectInfo)
        {
            if (projectInfo == null)
            {
                throw new ArgumentNullException("projectInfo");
            }

            this.OnProjectAdded(projectInfo);

            return this.CurrentSolution.GetProject(projectInfo.Id);
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
        public Document AddDocument(ProjectId projectId, string name, SourceText text)
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
            var loader = TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()));

            return this.AddDocument(DocumentInfo.Create(id, name, loader: loader));
        }

        /// <summary>
        /// Adds a document to the workspace.
        /// </summary>
        public Document AddDocument(DocumentInfo documentInfo)
        {
            if (documentInfo == null)
            {
                throw new ArgumentNullException("documentInfo");
            }

            this.OnDocumentAdded(documentInfo);

            return this.CurrentSolution.GetDocument(documentInfo.Id);
        }
    }
}
