// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpRazor : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpRazor(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpRazor), WellKnownProjectTemplates.BlazorApplication, WellKnownProjectTemplates.BlazorTemplateParameters)
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Razor)]
        public void ErrorListDiagnostics()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, "Pages/Index.razor");
            VisualStudio.Editor.SetText(
@"@page ""/""
<a onerror=""foo(@aaaa,
@bbbb, @ccccc)""></a>");

            VisualStudio.ErrorList.ShowErrorList();
            var expectedContents = new[] {
                new ErrorListItem(
                    severity: "Error",
                    description: "The name 'aaaa' does not exist in the current context",
                    project: null,
                    fileName: "Index.razor",
                    line: 2,
                    column: 18),
                new ErrorListItem(
                    severity: "Error",
                    description: "The name 'bbbb' does not exist in the current context",
                    project: null,
                    fileName: "Index.razor",
                    line: 3,
                    column: 2),
                new ErrorListItem(
                    severity: "Error",
                    description: "The name 'ccccc' does not exist in the current context",
                    project: null,
                    fileName: "Index.razor",
                    line: 3,
                    column: 9),
            };
            // Razor diagnostic items currently do not include the project. Calling GetHierarchy on them will throw.
            var actualContents = VisualStudio.ErrorList.GetErrorListContents(includeProject: false);
            Assert.Equal(expectedContents, actualContents);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Razor)]
        public void CrossFileNavigation()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.OpenFile(project, "Pages/Index.razor");
            VisualStudio.Editor.SetText(
@"@code
{
    public void Method()
    {
    }
}");
            VisualStudio.Workspace.WaitForAllAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles);
            VisualStudio.SolutionExplorer.OpenFile(project, "Program.cs");
            VisualStudio.Editor.SetText(
@"using TestProj.Pages;

public class Program
{
    public static void Main(string[] args)
    {
        new Index().Method();
    }
}");
            VisualStudio.Editor.PlaceCaret("Method");
            VisualStudio.Editor.GoToDefinition();
            VisualStudio.Editor.Verify.TextContains(@"public void Method$$()", assertCaretPosition: true);
        }
    }
}
