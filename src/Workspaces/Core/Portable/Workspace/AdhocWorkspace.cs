﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A workspace that allows full manipulation of projects and documents,
    /// but does not persist changes.
    /// </summary>
    public sealed class AdhocWorkspace : Workspace
    {
        public AdhocWorkspace(HostServices host, string workspaceKind = "Custom")
            : base(host, workspaceKind)
        {
        }

        public AdhocWorkspace()
            : this(Host.Mef.MefHostServices.DefaultHost)
        {
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            // all kinds supported.
            return true;
        }

        public override bool CanOpenDocuments =>
                // enables simulation of having documents open.
                true;

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
                throw new ArgumentNullException(nameof(solutionInfo));
            }

            this.OnSolutionAdded(solutionInfo);

            this.UpdateReferencesAfterAdd();

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
                throw new ArgumentNullException(nameof(projectInfo));
            }

            this.OnProjectAdded(projectInfo);

            this.UpdateReferencesAfterAdd();

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
                throw new ArgumentNullException(nameof(projectInfos));
            }

            foreach (var info in projectInfos)
            {
                this.OnProjectAdded(info);
            }

            this.UpdateReferencesAfterAdd();
        }

        /// <summary>
        /// Adds a document to the workspace.
        /// </summary>
        public Document AddDocument(ProjectId projectId, string name, SourceText text)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
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
                throw new ArgumentNullException(nameof(documentInfo));
            }

            this.OnDocumentAdded(documentInfo);

            return this.CurrentSolution.GetDocument(documentInfo.Id);
        }

        /// <summary>
        /// Puts the specified document into the open state.
        /// </summary>
        public override void OpenDocument(DocumentId documentId, bool activate = true)
        {
            var doc = this.CurrentSolution.GetDocument(documentId);
            if (doc != null)
            {
                var text = doc.GetTextSynchronously(CancellationToken.None);
                this.OnDocumentOpened(documentId, text.Container, activate);
            }
        }

        /// <summary>
        /// Puts the specified document into the closed state.
        /// </summary>
        public override void CloseDocument(DocumentId documentId)
        {
            var doc = this.CurrentSolution.GetDocument(documentId);
            if (doc != null)
            {
                var text = doc.GetTextSynchronously(CancellationToken.None);
                var version = doc.GetTextVersionSynchronously(CancellationToken.None);
                var loader = TextLoader.From(TextAndVersion.Create(text, version, doc.FilePath));
                this.OnDocumentClosed(documentId, loader);
            }
        }

        /// <summary>
        /// Puts the specified additional document into the open state.
        /// </summary>
        public override void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
        {
            var doc = this.CurrentSolution.GetAdditionalDocument(documentId);
            if (doc != null)
            {
                var text = doc.GetTextSynchronously(CancellationToken.None);
                this.OnAdditionalDocumentOpened(documentId, text.Container, activate);
            }
        }

        /// <summary>
        /// Puts the specified additional document into the closed state
        /// </summary>
        public override void CloseAdditionalDocument(DocumentId documentId)
        {
            var doc = this.CurrentSolution.GetAdditionalDocument(documentId);
            if (doc != null)
            {
                var text = doc.GetTextSynchronously(CancellationToken.None);
                var version = doc.GetTextVersionSynchronously(CancellationToken.None);
                var loader = TextLoader.From(TextAndVersion.Create(text, version, doc.FilePath));
                this.OnAdditionalDocumentClosed(documentId, loader);
            }
        }
    }
}
