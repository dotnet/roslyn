// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.NavigateTo)]
        public void NavigateTo()
        {
            using var telemetry = VisualStudio.EnableTestTelemetryChannel();

            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "test1.cs", open: false, contents: @"
class FirstClass
{
    void FirstMethod() { }
}");

            VisualStudio.SolutionExplorer.AddFile(project, "test2.cs", open: true, contents: @"
");

            VisualStudio.Editor.InvokeNavigateTo("FirstMethod", VirtualKey.Enter);
            VisualStudio.Editor.WaitForActiveView("test1.cs");
            Assert.Equal("FirstMethod", VisualStudio.Editor.GetSelectedText());

            // Add a VB project and verify that VB files are found when searching from C#
            var vbProject = new ProjectUtils.Project("VBProject");
            VisualStudio.SolutionExplorer.AddProject(vbProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            VisualStudio.SolutionExplorer.AddFile(vbProject, "vbfile.vb", open: true);

            VisualStudio.Editor.InvokeNavigateTo("FirstClass", VirtualKey.Enter);
            VisualStudio.Editor.WaitForActiveView("test1.cs");
            Assert.Equal("FirstClass", VisualStudio.Editor.GetSelectedText());
            telemetry.VerifyFired("vs/ide/vbcs/navigateto/search", "vs/platform/goto/launch");
        }
    }
}
