// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void VerifyColorOfSomeTokens()
        {
            VisualStudio.Editor.SetText(@"using System;
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

            VisualStudio.Editor.PlaceCaret("class");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "keyword");
            VisualStudio.Editor.PlaceCaret("{");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "punctuation");
            VisualStudio.Editor.PlaceCaret("Program");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudio.Editor.PlaceCaret("Main");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "method name");
            VisualStudio.Editor.PlaceCaret("Hello");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "string");
            VisualStudio.Editor.PlaceCaret("<summary", charsOffset: -1);
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudio.Editor.PlaceCaret("summary");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - name");
            VisualStudio.Editor.PlaceCaret("innertext");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - text");
            VisualStudio.Editor.PlaceCaret("comment");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - comment");
            VisualStudio.Editor.PlaceCaret("CDATA");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudio.Editor.PlaceCaret("cdata");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - cdata section");
            VisualStudio.Editor.PlaceCaret("attribute");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "identifier");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void SemanticClassification()
        {
            VisualStudio.Editor.SetText(@"
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
            VisualStudio.Editor.PlaceCaret("Attribute");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudio.Editor.PlaceCaret("list", charsOffset: 8);
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudio.Editor.PlaceCaret("list", charsOffset: -8);
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudio.Editor.PlaceCaret("null", charsOffset: -8);
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudio.Editor.MoveCaret(0);
            VisualStudio.Editor.DeleteText(@"using System;");
            VisualStudio.Editor.DeleteText(@"using System.Collections.Generic;");
            VisualStudio.Editor.PlaceCaret("Attribute");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudio.Editor.PlaceCaret("list", charsOffset: 8);
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudio.Editor.PlaceCaret("list", charsOffset: -8);
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudio.Editor.PlaceCaret("null", charsOffset: -8);
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void VerifyProjectConfigChange()
        {
            VisualStudio.Editor.SetText(@"
namespace ClassLibrary1
{
    public class Class1
    {
#if DEBUG
        void Goo()
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

            VisualStudio.ExecuteCommand(WellKnownCommandNames.Build_SolutionConfigurations, argument: "Debug");
            VisualStudio.Editor.PlaceCaret("Goo");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "method name");
            VisualStudio.Editor.PlaceCaret("Bar");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "excluded code");
            VisualStudio.Editor.MoveCaret(0);
            VisualStudio.ExecuteCommand("Build.SolutionConfigurations", argument: "Release");
            VisualStudio.Editor.PlaceCaret("Goo");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "excluded code");
            VisualStudio.Editor.PlaceCaret("Bar");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "method name");
        }
    }
}
