using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Workspace
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class Workspaces2 : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public Workspaces2(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(Workspaces2), WellKnownProjectTemplates.ClassLibrary)
        {
            EnableFullSolutionAnalysis();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void ProjectProperties()
        {
            Editor.SetText(@"Module Program
    Sub Main()
        Dim x = 42
        M(x)
    End Sub
    Sub M(p As Integer)
    End Sub
    Sub M(p As Object)
    End Sub
End Module");
            PlaceCaret("(x)", charsOffset: -1);
            EnableQuickInfo();
            EnableOptionInfer();
            InvokeQuickInfo();
            VerifyQuickInfo("Sub‎ Program.M‎(p‎ As‎ Integer‎)‎ ‎(‎+‎ 1‎ overload‎)");
            DisableOptionInfer();
            InvokeQuickInfo();
            VerifyQuickInfo("Sub‎ Program.M‎(p‎ As‎ Object‎)‎ ‎(‎+‎ 1‎ overload‎)");
        }
    }
}
