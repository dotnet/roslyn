// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReplClassification : AbstractInteractiveWindowTest
    {
        public CSharpReplClassification(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        [WpfFact]
        public void VerifyColorOfSomeTokens()
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

            VisualStudio.InteractiveWindow.PlaceCaret("using");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "keyword");
            VisualStudio.InteractiveWindow.PlaceCaret("{");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "punctuation");
            VisualStudio.InteractiveWindow.PlaceCaret("Main");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "method name");
            VisualStudio.InteractiveWindow.PlaceCaret("Hello");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "string");
            VisualStudio.InteractiveWindow.PlaceCaret("<summary", charsOffset: -1);
            VisualStudio.SendKeys.Send(new KeyPress(VirtualKey.Right, ShiftState.Alt));
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudio.InteractiveWindow.PlaceCaret("summary");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - name");
            VisualStudio.InteractiveWindow.PlaceCaret("innertext");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - text");
            VisualStudio.InteractiveWindow.PlaceCaret("!--");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudio.InteractiveWindow.PlaceCaret("comment");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - comment");
            VisualStudio.InteractiveWindow.PlaceCaret("CDATA");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudio.InteractiveWindow.PlaceCaret("cdata");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - cdata section");
            VisualStudio.InteractiveWindow.PlaceCaret("attribute");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudio.InteractiveWindow.PlaceCaret("Environment");
            VisualStudio.InteractiveWindow.Verify.CurrentTokenType(tokenType: "class name");
        }
    }
}
