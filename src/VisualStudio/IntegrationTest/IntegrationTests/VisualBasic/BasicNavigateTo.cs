using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicNavigateTo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicNavigateTo(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicNavigateTo))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)]
        public void NavigateTo()
        {
            AddFile("test1.vb", open: false, contents: @"
Class FirstClass
    Sub FirstMethod()
    End Sub
End Class");


            AddFile("test2.vb", open: true, contents: @"
");

            InvokeNavigateToAndPressEnter("FirstMethod");
            Editor.WaitForActiveView("test1.vb");
            Assert.Equal("FirstMethod", Editor.GetSelectedText());

            // Verify C# files are found when navigating from VB
            VisualStudio.Instance.SolutionExplorer.AddProject("CSProject", WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp);
            VisualStudio.Instance.SolutionExplorer.AddFile("CSProject", "csfile.cs", open: true);

            InvokeNavigateToAndPressEnter("FirstClass");
            Editor.WaitForActiveView("test1.vb");
            Assert.Equal("FirstClass", Editor.GetSelectedText());
        }
    }
}