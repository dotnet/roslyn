// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// this let us have isolated workspace services between solutions such as option services
    /// </summary>
    internal class TemporaryWorkspace : Workspace
    {
        private const string WorkspaceKind_TemporaryWorkspace = "TemporaryWorkspace";

        public TemporaryWorkspace(Solution solution)
            : base(RoslynServices.HostServices, workspaceKind: TemporaryWorkspace.WorkspaceKind_TemporaryWorkspace)
        {
            Options = Options.WithChangedOption(CacheOptions.RecoverableTreeLengthThreshold, 0);

            this.SetCurrentSolution(solution);
        }

        public TemporaryWorkspace(SolutionInfo solutionInfo)
            : base(RoslynServices.HostServices, workspaceKind: TemporaryWorkspace.WorkspaceKind_TemporaryWorkspace)
        {
            Options = Options.WithChangedOption(CacheOptions.RecoverableTreeLengthThreshold, 0);

            this.OnSolutionAdded(solutionInfo);
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            // apply change is not allowed
            return false;
        }

        public override bool CanOpenDocuments => false;
    }
}
