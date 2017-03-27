// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
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

            this.PlaceCaret("class");
            this.VerifyCurrentTokenType(tokenType: "keyword");
            this.PlaceCaret("{");
            this.VerifyCurrentTokenType(tokenType: "punctuation");
            this.PlaceCaret("Program");
            this.VerifyCurrentTokenType(tokenType: "class name");
            this.PlaceCaret("Main");
            this.VerifyCurrentTokenType(tokenType: "identifier");
            this.PlaceCaret("Hello");
            this.VerifyCurrentTokenType(tokenType: "string");
            this.PlaceCaret("<summary", charsOffset: -1);
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            this.PlaceCaret("summary");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - name");
            this.PlaceCaret("innertext");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - text");
            this.PlaceCaret("comment");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - comment");
            this.PlaceCaret("CDATA");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            this.PlaceCaret("cdata");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - cdata section");
            this.PlaceCaret("attribute");
            this.VerifyCurrentTokenType(tokenType: "identifier");
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
            this.PlaceCaret("Attribute");
            this.VerifyCurrentTokenType(tokenType: "class name");
            this.PlaceCaret("list", charsOffset: 8);
            this.VerifyCurrentTokenType(tokenType: "class name");
            this.PlaceCaret("list", charsOffset: -8);
            this.VerifyCurrentTokenType(tokenType: "class name");
            this.PlaceCaret("null", charsOffset: -8);
            this.VerifyCurrentTokenType(tokenType: "class name");
            Editor.MoveCaret(0);
            this.DeleteText(@"using System;");
            this.DeleteText(@"using System.Collections.Generic;");
            this.PlaceCaret("Attribute");
            this.VerifyCurrentTokenType(tokenType: "identifier");
            this.PlaceCaret("list", charsOffset: 8);
            this.VerifyCurrentTokenType(tokenType: "identifier");
            this.PlaceCaret("list", charsOffset: -8);
            this.VerifyCurrentTokenType(tokenType: "identifier");
            this.PlaceCaret("null", charsOffset: -8);
            this.VerifyCurrentTokenType(tokenType: "class name");
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

            this.ExecuteCommand(WellKnownCommandNames.Build_SolutionConfigurations, argument: "Debug");
            this.PlaceCaret("Foo");
            this.VerifyCurrentTokenType(tokenType: "identifier");
            this.PlaceCaret("Bar");
            this.VerifyCurrentTokenType(tokenType: "excluded code");
            Editor.MoveCaret(0);
            this.ExecuteCommand("Build.SolutionConfigurations", argument: "Release");
            this.PlaceCaret("Foo");
            this.VerifyCurrentTokenType(tokenType: "excluded code");
            this.PlaceCaret("Bar");
            this.VerifyCurrentTokenType(tokenType: "identifier");
        }
    }
}
