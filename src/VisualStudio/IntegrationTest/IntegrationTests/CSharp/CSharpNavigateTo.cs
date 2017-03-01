using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpNavigateTo: AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpNavigateTo(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpNavigateTo))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void NavigateTo1()
        {
            AddFile("test1.cs", open:false, contents: @"
class FirstClass
{
    void FirstMethod() { }
}");


            AddFile("test2.cs", open: true, contents: @"
");

            WaitForAsyncOperations(FeatureAttribute.Workspace);
            InvokeNavigateTo("FirstClass");
            Assert.Equal("FirstClass", Editor.GetSelectedText());
            InvokeNavigateTo("FirstMethod");
            Assert.Equal("FirstMethod", Editor.GetSelectedText());

            VisualStudio.Instance.SolutionExplorer.AddProject("VBProject", WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic);
            VisualStudio.Instance.SolutionExplorer.AddFile("VBProject", "vbfile.vb", open: true);
            WaitForAsyncOperations(FeatureAttribute.Workspace);

            InvokeNavigateTo("FirstClass");
            Assert.Equal("FirstClass", Editor.GetSelectedText());
        }
    }
}