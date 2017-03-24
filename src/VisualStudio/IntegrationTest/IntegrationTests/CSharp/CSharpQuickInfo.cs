using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpQuickInfo : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpQuickInfo(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpQuickInfo))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void QuickInfo_MetadataDocumentation()
        {
            SetUpEditor(@"
///<summary>Hello!</summary>
class Program
{
    static void Main(string$$[] args)
    {
    }
}");
            InvokeQuickInfo();
            Assert.Equal(
                "class\u200e System\u200e.String\r\nRepresents text as a sequence of UTF-16 code units.To browse the .NET Framework source code for this type, see the Reference Source.",
                Editor.GetQuickInfo());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void QuickInfo_Documentation()
        {
            SetUpEditor(@"
///<summary>Hello!</summary>
class Program$$
{
    static void Main(string[] args)
    {
    }
}");
            InvokeQuickInfo();
            Assert.Equal("class\u200e Program\r\nHello!", Editor.GetQuickInfo());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void International()
        {
            SetUpEditor(@"
/// <summary>
/// This is an XML doc comment defined in code.
/// </summary>
class العربية123
{
    static void Main()
    {
         العربية123$$ foo;
    }
}");
            InvokeQuickInfo();
            Assert.Equal(@"class" + '\u200e' + @" العربية123
This is an XML doc comment defined in code.", Editor.GetQuickInfo());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void SectionOrdering()
        {
            SetUpEditor(@"
using System;
using System.Threading.Tasks;

class C
{
    /// <exception cref=""Exception""></exception>
    async Task <int> M()
    {
                return await M$$();
            }
        }");

            InvokeQuickInfo();
            var expected = "\u200e(awaitable\u200e)\u200e Task\u200e<int\u200e>\u200e C\u200e.M\u200e(\u200e)\u000d\u000a\u000d\u000aUsage:\u000d\u000a  int\u200e x\u200e \u200e=\u200e await\u200e M\u200e(\u200e\u200e)\u200e;\u000d\u000a\u000d\u000aExceptions:\u200e\u000d\u000a\u200e  Exception";
            Assert.Equal(expected, Editor.GetQuickInfo());
        }
    }
}
