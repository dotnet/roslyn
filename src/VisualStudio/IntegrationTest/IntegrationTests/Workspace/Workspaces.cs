using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class Workspaces : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public Workspaces(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(Workspaces), WellKnownProjectTemplates.ClassLibrary)
        {
            EnableFullSolutionAnalysis();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void AddDocuments()
        {
            AddFile("NewDocument.cs");
            OpenFile("NewDocument.cs");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void OpenCSharpThenVBSolution()
        {
            Editor.SetText(@"using System; class Program { Exception e; }");
            PlaceCaret("Exception");
            VerifyCurrentTokenType(tokenType: "class name");
            CloseSolution();
            CreateSolution(nameof(Workspaces));
            AddProject(WellKnownProjectTemplates.ClassLibrary, languageName: LanguageNames.VisualBasic);
            Editor.SetText(@"Imports System
Class Program
    Private e As Exception
End Class");
            PlaceCaret("Exception");
            VerifyCurrentTokenType(tokenType: "class name");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void MetadataReference()
        {
            AddMetadataReference("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            Editor.SetText("class C { System.Windows.Point p; }");
            PlaceCaret("Point");
            VerifyCurrentTokenType("struct name");
            RemoveMetadataReference("WindowsBase");
            WaitForAsyncOperations(FeatureAttribute.Workspace);
            VerifyCurrentTokenType("identifier");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectReference()
        {
            AddProject(projectName: "CSProj2", projectTemplate: WellKnownProjectTemplates.ClassLibrary);
            AddProjectReference("CSProj2", ProjectName);
            AddFile("Program.cs", open: true, contents: "public class Class1 { }");
            AddFile("Program.cs", projectName: "CSProj2", open: true, contents: "public class Class2 { Class1 c; }");
            OpenFile("Program.cs", projectName: "CSProj2");
            PlaceCaret("Class1");
            VerifyCurrentTokenType("class name");
            RemoveProjectReference(ProjectName, "CSProj2");
            WaitForAsyncOperations(FeatureAttribute.Workspace);
            VerifyCurrentTokenType("identifier");
        }
    }
}
