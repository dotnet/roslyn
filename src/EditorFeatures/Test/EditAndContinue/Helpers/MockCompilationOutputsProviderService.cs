// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
