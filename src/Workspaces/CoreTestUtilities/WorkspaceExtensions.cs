// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests;

public static partial class WorkspaceExtensions
{
    public static DocumentId AddDocument(this Workspace workspace, ProjectId projectId, IEnumerable<string> folders, string name, SourceText initialText, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
    {
        var id = projectId.CreateDocumentId(name, folders);
        var oldSolution = workspace.CurrentSolution;
        var newSolution = oldSolution.AddDocument(id, name, initialText, folders).GetDocument(id)!.WithSourceCodeKind(sourceCodeKind).Project.Solution;
        workspace.TryApplyChanges(newSolution);
        return id;
    }

    public static void RemoveDocument(this Workspace workspace, DocumentId documentId)
    {
        var oldSolution = workspace.CurrentSolution;
        var newSolution = oldSolution.RemoveDocument(documentId);
        workspace.TryApplyChanges(newSolution);
    }

    public static void UpdateDocument(this Workspace workspace, DocumentId documentId, SourceText newText)
    {
        var oldSolution = workspace.CurrentSolution;
        var newSolution = oldSolution.WithDocumentText(documentId, newText);
        workspace.TryApplyChanges(newSolution);
    }

    /// <summary>
    /// Create a new DocumentId based on a name and optional folders
    /// </summary>
    public static DocumentId CreateDocumentId(this ProjectId projectId, string name, IEnumerable<string>? folders = null)
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
        => solution.Projects.Where(p => string.Compare(p.Name, name, StringComparison.OrdinalIgnoreCase) == 0);

    internal static EventWaiter VerifyWorkspaceChangedEvent(this Workspace workspace, Action<WorkspaceChangeEventArgs> action)
    {
        var wew = new EventWaiter();
        _ = workspace.RegisterWorkspaceChangedHandler(wew.Wrap(action));
        return wew;
    }
}
