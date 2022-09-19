// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionEvents
{
    internal sealed partial class HostSolutionEventsWorkspaceEventListener
    {
        private readonly struct SolutionEvent
        {
            public readonly WorkspaceChangeEventArgs? WorkspaceChangeArgs;
            public readonly TextDocumentEventArgs? DocumentOpenArgs;
            public readonly TextDocumentEventArgs? DocumentCloseArgs;

            public SolutionEvent(
                WorkspaceChangeEventArgs? workspaceChangeArgs,
                TextDocumentEventArgs? documentOpenArgs,
                TextDocumentEventArgs? documentCloseArgs)
            {
                if (workspaceChangeArgs != null)
                {
                    Contract.ThrowIfTrue(workspaceChangeArgs.OldSolution.Workspace != workspaceChangeArgs.NewSolution.Workspace);
                }

                Contract.ThrowIfTrue(workspaceChangeArgs is null && documentOpenArgs is null && documentCloseArgs is null);

                this.WorkspaceChangeArgs = workspaceChangeArgs;
                this.DocumentOpenArgs = documentOpenArgs;
                this.DocumentCloseArgs = documentCloseArgs;
            }

            public Solution Solution => WorkspaceChangeArgs?.OldSolution ?? DocumentOpenArgs?.Document.Project.Solution ?? DocumentCloseArgs!.Document.Project.Solution;
            public Workspace Workspace => Solution.Workspace;
        }





    }
}
