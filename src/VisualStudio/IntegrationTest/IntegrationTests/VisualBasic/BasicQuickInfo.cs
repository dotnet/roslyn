using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicQuickInfo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicQuickInfo(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicQuickInfo))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void QuickInfo1()
        {
            SetUpEditor(@"
''' <summary>Hello!</summary>
Class Program
    Sub Main(ByVal args As String$$())
    End Sub
End Class");
            InvokeQuickInfo();
            Assert.Equal("Class\u200e System.String\r\nRepresents text as a sequence of UTF-16 code units.To browse the .NET Framework source code for this type, see the Reference Source.",
                Editor.GetQuickInfo());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void International()
        {
            SetUpEditor(@"
''' <summary>
''' This is an XML doc comment defined in code.
''' </summary>
Class العربية123
    Shared Sub Foo()
         Dim foo as العربية123$$
    End Sub
End Class");
            InvokeQuickInfo();
            Assert.Equal(@"Class" + '\u200e' + @" TestProj.العربية123
This is an XML doc comment defined in code.", Editor.GetQuickInfo());
        }
    }
}
