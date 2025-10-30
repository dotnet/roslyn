// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public class CSharpReplClassification : AbstractInteractiveWindowTest
{
    [IdeFact]
    public async Task VerifyColorOfSomeTokens()
    {
        await TestServices.InteractiveWindow.InsertCodeAsync("""
            using System.Console;
            /// <summary>innertext
            /// </summary>
            /// <see cref="System.Environment" />
            /// <!--comment-->
            /// <![CDATA[cdata]]]]>&gt;
            /// <typeparam name="attribute" />
            public static void Main(string[] args)
                        {
                            WriteLine("Hello World");
                        }
            """, HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.PlaceCaretAsync("using", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "keyword", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("{", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "punctuation", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("Main", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "method name", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("Hello", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "string", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("<summary", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync((VirtualKeyCode.RIGHT, VirtualKeyCode.MENU), HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("summary", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - name", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("innertext", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - text", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("!--", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("comment", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - comment", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("CDATA", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("cdata", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - cdata section", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("attribute", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "identifier", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindow.PlaceCaretAsync("Environment", HangMitigatingCancellationToken);
        await TestServices.InteractiveWindowVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
    }
}
