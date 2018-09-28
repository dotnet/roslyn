// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public static partial class WorkspaceExtensions
    {
        public static DocumentId AddDocument(this Workspace workspace, ProjectId projectId, IEnumerable<string> folders, string name, SourceText initialText, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
        {
            var id = projectId.CreateDocumentId(name, folders);
            var oldSolution = workspace.CurrentSolution;
            var newSolution = oldSolution.AddDocument(id, name, initialText, folders).GetDocument(id).WithSourceCodeKind(sourceCodeKind).Project.Solution;
            workspace.TryApplyChanges(newSolution);
            return id;
        }

        public static void RemoveDocument(this Workspace workspace, DocumentId documentId)
        {
            var oldSolution = workspace.CurrentSolution;
            var newSolution = oldSolution.RemoveDocument(documentId);
            workspace.TryApplyChanges(newSolution);
        }

        public static void Updatedocument(this Workspace workspace, DocumentId documentId, SourceText newText)
        {
            var oldSolution = workspace.CurrentSolution;
            var newSolution = oldSolution.WithDocumentText(documentId, newText);
            workspace.TryApplyChanges(newSolution);
        }

        /// <summary>
        /// Create a new DocumentId based on a name and optional folders
        /// </summary>
        public static DocumentId CreateDocumentId(this ProjectId projectId, string name, IEnumerable<string> folders = null)
        {
            if (folders != null)
            {
                var uniqueName = string.Join("/", folders) + "/" + name;
                return DocumentId.CreateNewId(projectId, uniqueName);
            }
            else
            {
                return DocumentId.CreateNewId(projectId, name);
            }
        }

        public static IEnumerable<Project> GetProjectsByName(this Solution solution, string name)
        {
            return solution.Projects.Where(p => string.Compare(p.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        internal static EventWaiter VerifyWorkspaceChangedEvent(this Workspace workspace, Action<WorkspaceChangeEventArgs> action)
        {
            var wew = new EventWaiter();
            workspace.WorkspaceChanged += wew.Wrap<WorkspaceChangeEventArgs>((sender, args) => action(args));
            return wew;
        }

        internal static EventWaiter VerifyWorkspaceFailedEvent(this Workspace workspace, Action<WorkspaceDiagnosticEventArgs> action)
        {
            var wew = new EventWaiter();
            workspace.WorkspaceFailed += wew.Wrap<WorkspaceDiagnosticEventArgs>((sender, args) => action(args));
            return wew;
        }
    }
}
