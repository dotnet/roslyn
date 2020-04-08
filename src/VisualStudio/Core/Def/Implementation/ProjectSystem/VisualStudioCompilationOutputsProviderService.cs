// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed class VisualStudioCompilationOutputsProviderService : ICompilationOutputsProviderService
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        public VisualStudioCompilationOutputsProviderService(VisualStudioWorkspaceImpl workspace)
            => _workspace = workspace;

        public CompilationOutputs GetCompilationOutputs(ProjectId projectId)
            => _workspace.GetCompilationOutputs(projectId);
    }
}
