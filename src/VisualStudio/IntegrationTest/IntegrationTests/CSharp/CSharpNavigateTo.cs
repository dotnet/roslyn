// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
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

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void NavigateTo()
        {
            var project = new ProjectUtils.Project(ProjectName);
            this.AddFile("test1.cs", project: project, open: false, contents: @"
class FirstClass
{
    void FirstMethod() { }
}");


            this.AddFile("test2.cs", project: project, open: true, contents: @"
");

            this.InvokeNavigateToAndPressEnter("FirstMethod");
            Editor.WaitForActiveView("test1.cs");
            Assert.Equal("FirstMethod", Editor.GetSelectedText());

            // Add a VB project and verify that VB files are found when searching from C#
            VisualStudio.Instance.SolutionExplorer.AddProject("VBProject", WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            VisualStudio.Instance.SolutionExplorer.AddFile("VBProject", "vbfile.vb", open: true);

            this.InvokeNavigateToAndPressEnter("FirstClass");
            Editor.WaitForActiveView("test1.cs");
            Assert.Equal("FirstClass", Editor.GetSelectedText());
        }
    }
}