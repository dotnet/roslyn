// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpNavigateTo : AbstractIdeEditorTest
    {
        public CSharpNavigateTo()
            : base(nameof(CSharpNavigateTo))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task NavigateToAsync()
        {
            using (var telemetry = await VisualStudio.VisualStudio.EnableTestTelemetryChannelAsync())
            {
                await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "test1.cs", open: false, contents: @"
class FirstClass
{
    void FirstMethod() { }
}");


                await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "test2.cs", open: true, contents: @"
");

                await VisualStudio.Editor.InvokeNavigateToAsync("FirstMethod");
                await VisualStudio.Editor.WaitForActiveViewAsync("test1.cs", HangMitigatingCancellationToken);
                Assert.Equal("FirstMethod", await VisualStudio.Editor.GetSelectedTextAsync());

                // Add a VB project and verify that VB files are found when searching from C#
                await VisualStudio.SolutionExplorer.AddProjectAsync("VBProject", WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
                await VisualStudio.SolutionExplorer.AddFileAsync("VBProject", "vbfile.vb", open: true);

                await VisualStudio.Editor.InvokeNavigateToAsync("FirstClass");
                await VisualStudio.Editor.WaitForActiveViewAsync("test1.cs", HangMitigatingCancellationToken);
                Assert.Equal("FirstClass", await VisualStudio.Editor.GetSelectedTextAsync());
                await telemetry.VerifyFiredAsync("vs/ide/vbcs/navigateto/search", "vs/platform/goto/launch");
            }
        }
    }
}
