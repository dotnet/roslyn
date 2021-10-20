// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpNewDocumentFormatting : AbstractIntegrationTest
    {
        public CSharpNewDocumentFormatting(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudio.SolutionExplorer.CreateSolution(nameof(CSharpNewDocumentFormatting));
        }

        [WpfFact]
        [WorkItem(1411721, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1411721")]
        public void CreateLegacyProjectWithFileScopedNamespaces()
        {
            var project = new ProjectUtils.Project("TestProj");

            VisualStudio.Workspace.SetFileScopedNamespaces(true);

            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp);

            VisualStudio.ErrorList.ShowErrorList();
            VisualStudio.ErrorList.Verify.NoErrors();
        }

        [WpfFact]
        [WorkItem(1411721, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1411721")]
        public void CreateSDKProjectWithFileScopedNamespaces()
        {
            var project = new ProjectUtils.Project("TestProj");

            VisualStudio.Workspace.SetFileScopedNamespaces(true);

            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.CSharpNetCoreConsoleApplication, LanguageNames.CSharp);

            VisualStudio.ErrorList.ShowErrorList();
            VisualStudio.ErrorList.Verify.NoErrors();
        }
    }
}
