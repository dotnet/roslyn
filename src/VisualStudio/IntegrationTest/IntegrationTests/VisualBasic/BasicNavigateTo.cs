// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicNavigateTo : AbstractIdeEditorTest
    {
        public BasicNavigateTo()
            : base(nameof(BasicNavigateTo))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public async Task NavigateToAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "test1.vb", open: false, contents: @"
Class FirstClass
    Sub FirstMethod()
    End Sub
End Class");


            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "test2.vb", open: true, contents: @"
");
            await VisualStudio.Editor.InvokeNavigateToAsync("FirstMethod");
            await VisualStudio.Editor.WaitForActiveViewAsync("test1.vb", HangMitigatingCancellationToken);
            Assert.Equal("FirstMethod", await VisualStudio.Editor.GetSelectedTextAsync());

            // Verify C# files are found when navigating from VB
            await VisualStudio.SolutionExplorer.AddProjectAsync("CSProject", WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            await VisualStudio.SolutionExplorer.AddFileAsync("CSProject", "csfile.cs", open: true);

            await VisualStudio.Editor.InvokeNavigateToAsync("FirstClass");
            await VisualStudio.Editor.WaitForActiveViewAsync("test1.vb", HangMitigatingCancellationToken);
            Assert.Equal("FirstClass", await VisualStudio.Editor.GetSelectedTextAsync());
        }
    }
}
