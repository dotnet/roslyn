// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    public class BasicProjectExistsUIContext : AbstractIntegrationTest
    {
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(BasicProjectExistsUIContext), HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task ProjectContextChanges()
        {
            var workspace = await TestServices.Shell.GetComponentModelServiceAsync<VisualStudioWorkspace>(HangMitigatingCancellationToken);
            var contextProvider = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetRequiredService<IProjectExistsUIContextProviderLanguageService>();
            var context = contextProvider.GetUIContext();

            Assert.False(context.IsActive);

            await TestServices.SolutionExplorer.AddProjectAsync("TestVisualBasicProject", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic, HangMitigatingCancellationToken);

            Assert.True(context.IsActive);

            await TestServices.SolutionExplorer.CloseSolutionAsync(HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);

            Assert.False(context.IsActive);
        }
    }
}
