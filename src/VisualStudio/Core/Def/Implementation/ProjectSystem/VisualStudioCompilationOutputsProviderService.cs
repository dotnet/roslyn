// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed class VisualStudioCompilationOutputsProviderService : ICompilationOutputsProviderService
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        public VisualStudioCompilationOutputsProviderService(VisualStudioWorkspaceImpl workspace)
        {
            _workspace = workspace;
        }

        public CompilationOutputs GetCompilationOutputs(ProjectId projectId)
            => _workspace.GetCompilationOutputs(projectId);
    }
}
