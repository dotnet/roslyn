// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.ProjectFileLoader;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial class MSBuildWorkspace
    {
        /// <summary>
        /// Manages the flow of information between the workspace and a project file on disk.
        /// </summary>
        internal class ProjectData
        {
            private readonly MSBuildWorkspace workspace;
            private readonly ProjectInfo projectInfo;
            private readonly Guid guid;
            private readonly Dictionary<DocumentId, DocumentData> documents;
            private readonly IProjectFileLoaderLanguageService loader;

            internal ProjectData(
                MSBuildWorkspace workspace,
                ProjectInfo info,
                Guid guid,
                Dictionary<DocumentId, DocumentData> documents,
                IProjectFileLoaderLanguageService loader)
            {
                this.workspace = workspace;
                this.projectInfo = info;
                this.guid = guid;
                this.documents = documents;
                this.loader = loader;
            }

            public ProjectId Id
            {
                get { return this.projectInfo.Id; }
            }

            public Guid Guid
            {
                get { return this.guid; }
            }

            public string Language
            {
                get { return this.projectInfo.Language; }
            }

            public string Name
            {
                get { return Path.GetFileNameWithoutExtension(this.FilePath); }
            }

            public string FilePath
            {
                get { return this.projectInfo.FilePath; }
            }

            public ProjectInfo ProjectInfo
            {
                get { return this.projectInfo; }
            }

            public IProjectFileLoaderLanguageService Loader
            {
                get { return this.loader; }
            }

            public DocumentData GetDocument(DocumentId id)
            {
                DocumentData doc;
                this.documents.TryGetValue(id, out doc);
                return doc;
            }

            private static string GetAbsolutePath(string basePath, string relativePath)
            {
                if (!Path.IsPathRooted(relativePath))
                {
                    var combinedPath = Path.Combine(basePath, relativePath);
                    return Path.GetFullPath(combinedPath);
                }

                return relativePath;
            }

            private IProjectFile GetProjectFile(CancellationToken cancellationToken)
            {
                return this.workspace.projectCache.GetProjectFile(this.FilePath, this.loader, cancellationToken);
            }

            public void AddDocument(DocumentId id, IList<string> folders, string name, SourceCodeKind sourceCodeKind, SourceText text)
            {
                var projectFile = this.GetProjectFile(CancellationToken.None);

                var extension = projectFile.GetDocumentExtension(sourceCodeKind);
                var fileName = Path.ChangeExtension(name, extension);

                var relativePath = folders != null ? Path.Combine(Path.Combine(folders.ToArray()), fileName) : fileName;
                var fullPath = GetAbsolutePath(Path.GetDirectoryName(this.FilePath), relativePath);

                var document = new DocumentData(this.workspace, id, fullPath, fileName, folders, sourceCodeKind, isGenerated: false);
                this.documents.Add(document.Id, document);
                this.workspace.OnDocumentAdded(document.InitialState);

                // save text
                document.Save(text);

                // add to project file model
                projectFile.AddDocument(relativePath);

                try
                {
                    // save project file
                    projectFile.Save();
                }
                catch (System.IO.IOException exception)
                {
                    this.workspace.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.FileAccessFailure, exception.Message, this.Id));
                }
            }

            public void RemoveDocument(DocumentId documentId)
            {
                var doc = this.GetDocument(documentId);

                try
                {
                    // remove file from project
                    var projectFile = this.GetProjectFile(CancellationToken.None);
                    var filePath = GetAbsolutePath(Path.GetDirectoryName(this.FilePath), doc.FilePath);

                    // notify the workspace
                    this.workspace.OnDocumentRemoved(documentId);

                    projectFile.RemoveDocument(filePath);

                    // save project file
                    projectFile.Save();

                    // delete text file
                    doc.DeleteFile();
                }
                catch (System.IO.IOException exception)
                {
                    this.workspace.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.FileAccessFailure, exception.Message, this.Id));
                }
            }
        }
    }
}
