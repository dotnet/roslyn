// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpClassification : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpClassification()
            : base(nameof(CSharpClassification))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VerifyColorOfSomeTokensAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"using System;
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

            await VisualStudio.Editor.PlaceCaretAsync("class");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "keyword");
            await VisualStudio.Editor.PlaceCaretAsync("{");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "punctuation");
            await VisualStudio.Editor.PlaceCaretAsync("Program");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "class name");
            await VisualStudio.Editor.PlaceCaretAsync("Main");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "method name");
            await VisualStudio.Editor.PlaceCaretAsync("Hello");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "string");
            await VisualStudio.Editor.PlaceCaretAsync("<summary", charsOffset: -1);
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter");
            await VisualStudio.Editor.PlaceCaretAsync("summary");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - name");
            await VisualStudio.Editor.PlaceCaretAsync("innertext");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - text");
            await VisualStudio.Editor.PlaceCaretAsync("comment");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - comment");
            await VisualStudio.Editor.PlaceCaretAsync("CDATA");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter");
            await VisualStudio.Editor.PlaceCaretAsync("cdata");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - cdata section");
            await VisualStudio.Editor.PlaceCaretAsync("attribute");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "identifier");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task SemanticClassificationAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
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
            await VisualStudio.Editor.PlaceCaretAsync("Attribute");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "class name");
            await VisualStudio.Editor.PlaceCaretAsync("list", charsOffset: 8);
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "class name");
            await VisualStudio.Editor.PlaceCaretAsync("list", charsOffset: -8);
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "class name");
            await VisualStudio.Editor.PlaceCaretAsync("null", charsOffset: -8);
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "class name");
            await VisualStudio.Editor.MoveCaretAsync(0);
            await VisualStudio.Editor.DeleteTextAsync(@"using System;");
            await VisualStudio.Editor.DeleteTextAsync(@"using System.Collections.Generic;");
            await VisualStudio.Editor.PlaceCaretAsync("Attribute");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "identifier");
            await VisualStudio.Editor.PlaceCaretAsync("list", charsOffset: 8);
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "identifier");
            await VisualStudio.Editor.PlaceCaretAsync("list", charsOffset: -8);
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "identifier");
            await VisualStudio.Editor.PlaceCaretAsync("null", charsOffset: -8);
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "class name");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VerifyProjectConfigChangeAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
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

            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Build_SolutionConfigurations, args: "Debug");
            await VisualStudio.Editor.PlaceCaretAsync("Goo");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "method name");
            await VisualStudio.Editor.PlaceCaretAsync("Bar");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "excluded code");
            await VisualStudio.Editor.MoveCaretAsync(0);
            await VisualStudio.VisualStudio.ExecuteCommandAsync("Build.SolutionConfigurations", args: "Release");
            await VisualStudio.Editor.PlaceCaretAsync("Goo");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "excluded code");
            await VisualStudio.Editor.PlaceCaretAsync("Bar");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "method name");
        }
    }
}
