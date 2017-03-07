// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
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
        public void VerifyColorOfSomeTokens()
        {
            Editor.SetText(@"using System;
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
            VerifyCurrentTokenType(tokenType: "keyword");
            PlaceCaret("{");
            VerifyCurrentTokenType(tokenType: "punctuation");
            PlaceCaret("Program");
            VerifyCurrentTokenType(tokenType: "class name");
            PlaceCaret("Main");
            VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("Hello");
            VerifyCurrentTokenType(tokenType: "string");
            PlaceCaret("<summary", charsOffset: -1);
            VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("summary");
            VerifyCurrentTokenType(tokenType: "xml doc comment - name");
            PlaceCaret("innertext");
            VerifyCurrentTokenType(tokenType: "xml doc comment - text");
            PlaceCaret("comment");
            VerifyCurrentTokenType(tokenType: "xml doc comment - comment");
            PlaceCaret("CDATA");
            VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("cdata");
            VerifyCurrentTokenType(tokenType: "xml doc comment - cdata section");
            PlaceCaret("attribute");
            VerifyCurrentTokenType(tokenType: "identifier");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void SemanticClassification()
        {
            Editor.SetText(@"
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
            VerifyCurrentTokenType(tokenType: "class name");
            PlaceCaret("list", charsOffset: 8);
            VerifyCurrentTokenType(tokenType: "class name");
            PlaceCaret("list", charsOffset: -8);
            VerifyCurrentTokenType(tokenType: "class name");
            PlaceCaret("null", charsOffset: -8);
            VerifyCurrentTokenType(tokenType: "class name");
            Editor.MoveCaret(0);
            DeleteText(@"using System;");
            DeleteText(@"using System.Collections.Generic;");
            PlaceCaret("Attribute");
            VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("list", charsOffset: 8);
            VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("list", charsOffset: -8);
            VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("null", charsOffset: -8);
            VerifyCurrentTokenType(tokenType: "class name");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void VerifyProjectConfigChange()
        {
            Editor.SetText(@"
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
            VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("Bar");
            VerifyCurrentTokenType(tokenType: "excluded code");
            Editor.MoveCaret(0);
            ExecuteCommand("Build.SolutionConfigurations", argument: "Release");
            PlaceCaret("Foo");
            VerifyCurrentTokenType(tokenType: "excluded code");
            PlaceCaret("Bar");
            VerifyCurrentTokenType(tokenType: "identifier");
        }

    }
}
