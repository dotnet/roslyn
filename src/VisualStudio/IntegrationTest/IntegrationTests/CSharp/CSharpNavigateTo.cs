// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;


namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpNavigateTo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpNavigateTo(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpNavigateTo))
        {
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19530"), Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public void NavigateTo()
        {
            using (var telemetry = VisualStudio.EnableTestTelemetryChannel())
            {

                var project = new ProjectUtils.Project(ProjectName);
                VisualStudio.SolutionExplorer.AddFile(project, "test1.cs", open: false, contents: @"
class FirstClass
{
    void FirstMethod() { }
}");


                VisualStudio.SolutionExplorer.AddFile(project, "test2.cs", open: true, contents: @"
");

                VisualStudio.Editor.InvokeNavigateTo("FirstMethod");
                VisualStudio.Editor.NavigateToSendKeys("{ENTER}");
                VisualStudio.Editor.WaitForActiveView("test1.cs");
                Assert.Equal("FirstMethod", VisualStudio.Editor.GetSelectedText());

                // Add a VB project and verify that VB files are found when searching from C#
                var vbProject = new ProjectUtils.Project("VBProject");
                VisualStudio.SolutionExplorer.AddProject(vbProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
                VisualStudio.SolutionExplorer.AddFile(vbProject, "vbfile.vb", open: true);

                VisualStudio.Editor.InvokeNavigateTo("FirstClass");
                VisualStudio.Editor.NavigateToSendKeys("{ENTER}");
                VisualStudio.Editor.WaitForActiveView("test1.cs");
                Assert.Equal("FirstClass", VisualStudio.Editor.GetSelectedText());
                telemetry.VerifyFired("vs/ide/vbcs/navigateto/search", "vs/platform/goto/launch");
            }
        }
    }
}
