// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.Classification)]
    public class CSharpClassification : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpClassification() : base(nameof(CSharpClassification))
        {
        }

        [IdeFact, Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public async Task VerifyColorOfSomeTokens()
        {
            await TestServices.Editor.SetTextAsync(@"using System;
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
    }", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("class", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "keyword", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("{", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "punctuation", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("Program", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("Main", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "method name", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("Hello", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "string", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("<summary", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("summary", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - name", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("innertext", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - text", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("comment", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - comment", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("CDATA", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("cdata", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - cdata section", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("attribute", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "identifier", HangMitigatingCancellationToken);
        }

        [IdeFact, Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        public async Task SemanticClassification()
        {
            await TestServices.Editor.SetTextAsync(@"
using System;
using System.Collections.Generic;
class Program : Attribute
{
    static void Main(string[] args)
    {
        List<int> list = new List<int>();
        Program.Main(null);
    }
}", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("Attribute", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("list", charsOffset: 8, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("list", charsOffset: -8, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("null", charsOffset: -8, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
            await TestServices.Editor.MoveCaretAsync(0, HangMitigatingCancellationToken);
            await TestServices.Editor.DeleteTextAsync(@"using System;", HangMitigatingCancellationToken);
            await TestServices.Editor.DeleteTextAsync(@"using System.Collections.Generic;", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("Attribute", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "identifier", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("list", charsOffset: 8, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "identifier", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("list", charsOffset: -8, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "identifier", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("null", charsOffset: -8, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifyProjectConfigChange()
        {
            await TestServices.Editor.SetTextAsync(@"
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
", HangMitigatingCancellationToken);

            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.SolutionCfg, argument: "Debug", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("Goo", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "method name", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("Bar", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "excluded code", HangMitigatingCancellationToken);
            await TestServices.Editor.MoveCaretAsync(0, HangMitigatingCancellationToken);
            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.SolutionCfg, argument: "Release", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("Goo", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "excluded code", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("Bar", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "method name", HangMitigatingCancellationToken);
        }
    }
}
