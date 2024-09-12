// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class InteractiveWorkspace
    {
        internal sealed class SolutionAnalyzerSetter(InteractiveWorkspace workspace) : ISolutionAnalyzerSetterWorkspaceService
        {
            [ExportWorkspaceServiceFactory(typeof(ISolutionAnalyzerSetterWorkspaceService), WorkspaceKind.Interactive), Shared]
            internal sealed class Factory : IWorkspaceServiceFactory
            {
                [ImportingConstructor]
                [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
                public Factory()
                {
                }

                public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                    => new SolutionAnalyzerSetter((InteractiveWorkspace)workspaceServices.Workspace);
            }

            private readonly InteractiveWorkspace _workspace = workspace;

            public void SetAnalyzerReferences(ImmutableArray<AnalyzerReference> references)
                => _workspace.SetCurrentSolution(s => s.WithAnalyzerReferences(references), WorkspaceChangeKind.SolutionChanged);
        }
    }
}
