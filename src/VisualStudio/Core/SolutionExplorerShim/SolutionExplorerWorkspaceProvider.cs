// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(ISolutionExplorerWorkspaceProvider))]
    internal class SolutionExplorerWorkspaceProvider : ISolutionExplorerWorkspaceProvider
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        [ImportingConstructor]
        public SolutionExplorerWorkspaceProvider(VisualStudioWorkspaceImpl workspace)
        {
            _workspace = workspace;
        }

        public Microsoft.CodeAnalysis.Workspace GetWorkspace()
        {
            return _workspace;
        }
    }
}
