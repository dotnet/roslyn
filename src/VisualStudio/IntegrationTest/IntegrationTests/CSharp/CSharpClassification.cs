// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpClassification : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpClassification( )
            : base( nameof(CSharpClassification))
        {
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
        public void VerifyColorOfSomeTokens()
        {
            VisualStudioInstance.Editor.SetText(@"using System;
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

            VisualStudioInstance.Editor.PlaceCaret("class");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "keyword");
            VisualStudioInstance.Editor.PlaceCaret("{");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "punctuation");
            VisualStudioInstance.Editor.PlaceCaret("Program");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudioInstance.Editor.PlaceCaret("Main");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "method name");
            VisualStudioInstance.Editor.PlaceCaret("Hello");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "string");
            VisualStudioInstance.Editor.PlaceCaret("<summary", charsOffset: -1);
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudioInstance.Editor.PlaceCaret("summary");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - name");
            VisualStudioInstance.Editor.PlaceCaret("innertext");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - text");
            VisualStudioInstance.Editor.PlaceCaret("comment");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - comment");
            VisualStudioInstance.Editor.PlaceCaret("CDATA");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudioInstance.Editor.PlaceCaret("cdata");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - cdata section");
            VisualStudioInstance.Editor.PlaceCaret("attribute");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "identifier");
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
        public void SemanticClassification()
        {
            VisualStudioInstance.Editor.SetText(@"
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
            VisualStudioInstance.Editor.PlaceCaret("Attribute");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudioInstance.Editor.PlaceCaret("list", charsOffset: 8);
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudioInstance.Editor.PlaceCaret("list", charsOffset: -8);
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudioInstance.Editor.PlaceCaret("null", charsOffset: -8);
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudioInstance.Editor.MoveCaret(0);
            VisualStudioInstance.Editor.DeleteText(@"using System;");
            VisualStudioInstance.Editor.DeleteText(@"using System.Collections.Generic;");
            VisualStudioInstance.Editor.PlaceCaret("Attribute");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudioInstance.Editor.PlaceCaret("list", charsOffset: 8);
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudioInstance.Editor.PlaceCaret("list", charsOffset: -8);
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudioInstance.Editor.PlaceCaret("null", charsOffset: -8);
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "class name");
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
        public void VerifyProjectConfigChange()
        {
            VisualStudioInstance.Editor.SetText(@"
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

            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Build_SolutionConfigurations, argument: "Debug");
            VisualStudioInstance.Editor.PlaceCaret("Goo");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "method name");
            VisualStudioInstance.Editor.PlaceCaret("Bar");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "excluded code");
            VisualStudioInstance.Editor.MoveCaret(0);
            VisualStudioInstance.ExecuteCommand("Build.SolutionConfigurations", argument: "Release");
            VisualStudioInstance.Editor.PlaceCaret("Goo");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "excluded code");
            VisualStudioInstance.Editor.PlaceCaret("Bar");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "method name");
        }
    }
}
