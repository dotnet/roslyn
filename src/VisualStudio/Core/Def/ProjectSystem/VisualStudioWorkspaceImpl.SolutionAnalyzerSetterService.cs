// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl
    {
        internal sealed class SolutionAnalyzerSetter : ISolutionAnalyzerSetterWorkspaceService
        {
            [ExportWorkspaceServiceFactory(typeof(ISolutionAnalyzerSetterWorkspaceService), WorkspaceKind.Host), Shared]
            internal sealed class Factory : IWorkspaceServiceFactory
            {
                [ImportingConstructor]
                [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
                public Factory()
                {
                }

                public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                    => new SolutionAnalyzerSetter((VisualStudioWorkspaceImpl)workspaceServices.Workspace);
            }

            private readonly VisualStudioWorkspaceImpl _workspace;

            public SolutionAnalyzerSetter(VisualStudioWorkspaceImpl workspace)
                => _workspace = workspace;

            public void SetAnalyzerReferences(ImmutableArray<AnalyzerReference> references)
                => _workspace.ProjectSystemProjectFactory.ApplyChangeToWorkspace(w => w.SetCurrentSolution(s => s.WithAnalyzerReferences(references), WorkspaceChangeKind.SolutionChanged));
        }
    }
}
