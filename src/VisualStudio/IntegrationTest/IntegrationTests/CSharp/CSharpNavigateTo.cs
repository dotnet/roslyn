// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpNavigateTo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpNavigateTo( )
            : base( nameof(CSharpNavigateTo))
        {
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/19530"), TestCategory(Traits.Features.NavigateTo)]
        public void NavigateTo()
        {
            using (var telemetry = VisualStudioInstance.EnableTestTelemetryChannel())
            {

                var project = new ProjectUtils.Project(ProjectName);
                VisualStudioInstance.SolutionExplorer.AddFile(project, "test1.cs", open: false, contents: @"
class FirstClass
{
    void FirstMethod() { }
}");


                VisualStudioInstance.SolutionExplorer.AddFile(project, "test2.cs", open: true, contents: @"
");

                VisualStudioInstance.Editor.InvokeNavigateTo("FirstMethod");
                VisualStudioInstance.Editor.NavigateToSendKeys("{ENTER}");
                VisualStudioInstance.Editor.WaitForActiveView("test1.cs");
                Assert.AreEqual("FirstMethod", VisualStudioInstance.Editor.GetSelectedText());

                // Add a VB project and verify that VB files are found when searching from C#
                var vbProject = new ProjectUtils.Project("VBProject");
                VisualStudioInstance.SolutionExplorer.AddProject(vbProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
                VisualStudioInstance.SolutionExplorer.AddFile(vbProject, "vbfile.vb", open: true);

                VisualStudioInstance.Editor.InvokeNavigateTo("FirstClass");
                VisualStudioInstance.Editor.NavigateToSendKeys("{ENTER}");
                VisualStudioInstance.Editor.WaitForActiveView("test1.cs");
                Assert.AreEqual("FirstClass", VisualStudioInstance.Editor.GetSelectedText());
                telemetry.VerifyFired("vs/ide/vbcs/navigateto/search", "vs/platform/goto/launch");
            }
        }
    }
}
