// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.LanguageServices;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpSourceGenerators : AbstractEditorTest
    {
        public CSharpSourceGenerators(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpSourceGenerators), WellKnownProjectTemplates.ConsoleApplication)
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // Right now source generators are still restricted to /langver:preview
            VisualStudio.SolutionExplorer.SetLanguageVersion(new ProjectUtils.Project(ProjectName), "preview");
            VisualStudio.SolutionExplorer.AddAnalyzerReference(typeof(IntegrationTestSourceGenerator).Assembly.Location, new ProjectUtils.Project(ProjectName));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SourceGenerators)]
        public void GoToDefinitionOpensGeneratedFile()
        {
            VisualStudio.Editor.SetText(@"using System;
internal static class Program
{
    public static void Main()
    {
        Console.WriteLine(" + IntegrationTestSourceGenerator.GeneratedClassName + @".GetMessage());
    }
}");

            VisualStudio.Editor.PlaceCaret(IntegrationTestSourceGenerator.GeneratedClassName);
            VisualStudio.Editor.GoToDefinition();
            Assert.Equal($"{IntegrationTestSourceGenerator.GeneratedClassName}.cs {ServicesVSResources.generated_suffix}", VisualStudio.Shell.GetActiveWindowCaption());
        }
    }
}
