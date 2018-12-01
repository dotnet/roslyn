// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpReplClassification : AbstractInteractiveWindowTest
    {
        public CSharpReplClassification( )
            : base()
        {
        }

        [TestMethod]
        public void VerifyColorOfSomeTokens()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode(@"using System.Console;
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

            VisualStudioInstance.InteractiveWindow.PlaceCaret("using");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "keyword");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("{");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "punctuation");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("Main");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "method name");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("Hello");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "string");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("<summary", charsOffset: -1);
            VisualStudioInstance.SendKeys.Send(new KeyPress(VirtualKey.Right, ShiftState.Alt));
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("summary");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - name");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("innertext");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - text");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("!--");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("comment");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - comment");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("CDATA");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("cdata");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "xml doc comment - cdata section");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("attribute");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("Environment");
            VisualStudioInstance.InteractiveWindow.Verify.CurrentTokenType(tokenType: "class name");
        }
    }
}
