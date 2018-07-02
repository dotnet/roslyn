// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReplClassification : AbstractIdeInteractiveWindowTest
    {
        [IdeFact]
        public async Task VerifyColorOfSomeTokensAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"using System.Console;
/// <summary>innertext
/// </summary>
/// <see cref=""System.Environment"" />
/// <!--comment-->
/// <![CDATA[cdata]]]]>&gt;
/// <typeparam name=""attribute"" />
public static void Main(string[] args)
            {
                WriteLine(""Hello World"");
            }");

            await VisualStudio.InteractiveWindow.PlaceCaretAsync("using");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "keyword");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("{");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "punctuation");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("Main");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "method name");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("Hello");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "string");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("<summary", charsOffset: -1);
            await VisualStudio.SendKeys.SendAsync(new KeyPress(VirtualKey.Right, ShiftState.Alt));
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("summary");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - name");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("innertext");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - text");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("!--");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("comment");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - comment");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("CDATA");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("cdata");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - cdata section");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("attribute");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "identifier");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("Environment");
            await VisualStudio.InteractiveWindow.Verify.CurrentTokenTypeAsync(tokenType: "class name");
        }
    }
}
