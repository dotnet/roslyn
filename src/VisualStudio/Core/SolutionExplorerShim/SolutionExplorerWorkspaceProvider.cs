// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    [Export(typeof(ISolutionExplorerWorkspaceProvider))]
    internal class SolutionExplorerWorkspaceProvider : ISolutionExplorerWorkspaceProvider
    {
        private readonly VisualStudioWorkspace _workspace;

        [ImportingConstructor]
        public SolutionExplorerWorkspaceProvider(VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
        }

        public Workspace GetWorkspace()
        {
            return _workspace;
        }
    }
}
