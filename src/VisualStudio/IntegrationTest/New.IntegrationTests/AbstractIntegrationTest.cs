// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [IdeSettings(MinVersion = VisualStudioVersion.VS2022, RootSuffix = "RoslynDev")]
    public abstract class AbstractIntegrationTest : AbstractIdeIntegrationTest
    {
        protected const string ProjectName = "TestProj";
        protected const string SolutionName = "TestSolution";

        protected AbstractIntegrationTest()
        {
            WorkspaceInProcess.EnableAsynchronousOperationTracking();
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await TestServices.StateReset.ResetGlobalOptionsAsync(HangMitigatingCancellationToken);
            await TestServices.StateReset.ResetHostSettingsAsync(HangMitigatingCancellationToken);
        }
    }
}
