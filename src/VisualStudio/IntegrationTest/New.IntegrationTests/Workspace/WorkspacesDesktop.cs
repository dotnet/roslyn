// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.Workspaces
{
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    public class WorkspacesDesktop : WorkspaceBase
    {
        public WorkspacesDesktop()
            : base(WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [IdeFact]
        public override async Task MetadataReference()
        {
            await InitializeWithDefaultSolution();
            await base.MetadataReference();
        }
    }
}

