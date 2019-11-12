// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// this lets us have isolated workspace services between solutions such as option services.
    /// 
    /// otherwise, mutating service in one service call such as changing options, can affect result of other service call
    /// </summary>
    internal class TemporaryWorkspace : Workspace
    {
        private TemporaryWorkspace()
            : base(RoslynServices.HostServices, workspaceKind: WorkspaceKind.RemoteTemporaryWorkspace)
        {
            Options = Options.WithChangedOption(CacheOptions.RecoverableTreeLengthThreshold, 0);

            var documentOptionsProviderFactories = ((IMefHostExportProvider)Services.HostServices).GetExports<IDocumentOptionsProviderFactory, OrderableMetadata>();

            RegisterDocumentOptionProviders(documentOptionsProviderFactories);
        }

        public TemporaryWorkspace(Solution solution) : this()
        {
            this.SetCurrentSolution(solution);
        }

        public TemporaryWorkspace(SolutionInfo solutionInfo) : this()
        {
            this.OnSolutionAdded(solutionInfo);
        }

        // for now, temproary workspace is not mutable. consumer can still freely fork solution as they wish
        // they just can't apply those changes back to the workspace.
        public override bool CanApplyChange(ApplyChangesKind feature) => false;

        public override bool CanOpenDocuments => false;
    }
}
