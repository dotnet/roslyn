// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicBuild : AbstractIntegrationTest
    {
        public BasicBuild() : base() { }

        [TestInitialize]
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudioInstance.SolutionExplorer.CreateSolution(nameof(BasicBuild));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudioInstance.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);
        }

        [TestMethod, TestCategory(Traits.Features.Build)]
        public void BuildProject()
        {
            var editorText = @"Module Program

    Sub Main()
        Console.WriteLine(""Hello, World!"")
    End Sub

End Module";

            VisualStudioInstance.Editor.SetText(editorText);

            // TODO: Validate build works as expected
        }
    }
}
