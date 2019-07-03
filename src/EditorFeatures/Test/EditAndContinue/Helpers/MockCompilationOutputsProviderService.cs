// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal sealed class MockCompilationOutputsProviderService : ICompilationOutputsProviderService
    {
        public readonly Dictionary<ProjectId, CompilationOutputs> Outputs = new Dictionary<ProjectId, CompilationOutputs>();

        public CompilationOutputs GetCompilationOutputs(ProjectId projectId)
            => Outputs.ContainsKey(projectId) ? Outputs[projectId] : new MockCompilationOutputs(Guid.NewGuid());
    }
}
