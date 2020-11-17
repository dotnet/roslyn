// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.TestSourceGenerator;
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

            VisualStudio.SolutionExplorer.AddAnalyzerReference(typeof(HelloWorldGenerator).Assembly.Location, new ProjectUtils.Project(ProjectName));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SourceGenerators)]
        public void GoToDefinitionOpensGeneratedFile()
        {
            VisualStudio.Editor.SetText(@"using System;
internal static class Program
{
    public static void Main()
    {
        Console.WriteLine(" + HelloWorldGenerator.GeneratedClassName + @".GetMessage());
    }
}");

            VisualStudio.Editor.PlaceCaret(HelloWorldGenerator.GeneratedClassName);
            VisualStudio.Editor.GoToDefinition();
            Assert.Equal($"{HelloWorldGenerator.GeneratedClassName}.cs {ServicesVSResources.generated_suffix}", VisualStudio.Shell.GetActiveWindowCaption());
            Assert.Equal(HelloWorldGenerator.GeneratedClassName, VisualStudio.Editor.GetSelectedText());
        }
    }
}
