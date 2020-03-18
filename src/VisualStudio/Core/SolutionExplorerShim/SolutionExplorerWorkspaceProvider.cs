﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
