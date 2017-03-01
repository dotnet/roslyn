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
    public class BasicNavigateTo: AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicNavigateTo(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicNavigateTo))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void NavigateTo1()
        {
            AddFile("test1.vb", open:false, contents: @"
Class FirstClass
    Sub FirstMethod()
    End Sub
End Class");


            AddFile("test2.cs", open: true, contents: @"
");

            WaitForAsyncOperations(FeatureAttribute.Workspace);
            InvokeNavigateTo("FirstClass");
            Assert.Equal("FirstClass", Editor.GetSelectedText());
            InvokeNavigateTo("FirstMethod");
            Assert.Equal("FirstMethod", Editor.GetSelectedText());

            VisualStudio.Instance.SolutionExplorer.AddProject("CSProject", WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            VisualStudio.Instance.SolutionExplorer.AddFile("CSProject", "csfile.cs", open: true);
            WaitForAsyncOperations(FeatureAttribute.Workspace);

            InvokeNavigateTo("FirstClass");
            Assert.Equal("FirstClass", Editor.GetSelectedText());
        }
    }
}