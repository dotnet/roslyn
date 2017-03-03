using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpClassification : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpClassification(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpClassification))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void Verify_Color_Of_Some_Tokens()
        {
            SetEditorText(@"using System;
using System.Collections.Generic;
using System.Text;
namespace ConsoleApplication1
{
    /// <summary>innertext
    /// </summary>
    /// <!--comment-->
    /// <![CDATA[cdata]]>
    /// <typeparam name=""attribute"" />
    public class Program
        {
            public static void Main(string[] args)
            {
                Console.WriteLine(""Hello World"");
            }
        }
    }");

            PlaceCaret("class");
            VerifyTokenType(tokenType: "keyword");
            PlaceCaret("{");
            VerifyTokenType(tokenType: "punctuation");
            PlaceCaret("Program");
            VerifyTokenType(tokenType: "class name");
            PlaceCaret("Main");
            VerifyTokenType(tokenType: "identifier");
            PlaceCaret("Hello");
            VerifyTokenType(tokenType: "string");
            PlaceCaret("Hello");
            VerifyTokenType(tokenType: "string");
            PlaceCaret("<summary", charsOffset: -1);
            VerifyTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("summary");
            VerifyTokenType(tokenType: "xml doc comment - name");
            PlaceCaret("innertext");
            VerifyTokenType(tokenType: "xml doc comment - text");
            PlaceCaret("comment");
            VerifyTokenType(tokenType: "xml doc comment - comment");
            PlaceCaret("CDATA");
            VerifyTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("cdata");
            VerifyTokenType(tokenType: "xml doc comment - cdata section");
            PlaceCaret("attribute");
            VerifyTokenType(tokenType: "identifier");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void Semantic_Classification()
        {
            SetEditorText(@"
using System;
using System.Collections.Generic;
class Program : Attribute
{
    static void Main(string[] args)
    {
        List<int> list = new List<int>();
        Program.Main(null);
    }
}");
            PlaceCaret("Attribute");
            VerifyTokenType(tokenType: "class name");
            PlaceCaret("list", charsOffset: 8);
            VerifyTokenType(tokenType: "class name");
            PlaceCaret("list", charsOffset: -8);
            VerifyTokenType(tokenType: "class name");
            PlaceCaret("null", charsOffset: -8);
            VerifyTokenType(tokenType: "class name");
            DeleteText(@"using System;");
            DeleteText(@"using System.Collections.Generic;");
            PlaceCaret("Attribute");
            VerifyTokenType(tokenType: "identifier");
            PlaceCaret("list", charsOffset: 8);
            VerifyTokenType(tokenType: "identifier");
            PlaceCaret("list", charsOffset: -8);
            VerifyTokenType(tokenType: "identifier");
            PlaceCaret("null", charsOffset: -8);
            VerifyTokenType(tokenType: "class name");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void Verify_Project_Config_Change()
        {
            SetEditorText(@"
namespace ClassLibrary1
{
    public class Class1
    {
#if DEBUG
        void Foo()
        {
        }
#else
        void Bar()
        {
        }
#endif
    }
}
");
            ExecuteCommand("Build.SolutionConfigurations", argument: "Debug");
            PlaceCaret("Foo");
            VerifyTokenType(tokenType: "identifier");
            PlaceCaret("Bar");
            VerifyTokenType(tokenType: "excluded code");
            Editor.MoveCaret(0);
            ExecuteCommand("Build.SolutionConfigurations", argument: "Release");
            PlaceCaret("Foo");
            VerifyTokenType(tokenType: "excluded code");
            PlaceCaret("Bar");
            VerifyTokenType(tokenType: "identifier");
        }

    }
}
