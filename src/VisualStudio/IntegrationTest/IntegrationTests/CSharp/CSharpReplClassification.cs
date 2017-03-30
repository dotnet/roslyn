// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Interactive;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReplClassification : AbstractInteractiveWindowTest
    {
        public CSharpReplClassification(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        [Fact]
        public void VerifyColorOfSomeTokens()
        {
            this.InsertCode(@"using System.Console;
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

            this.PlaceCaret("using");
            this.VerifyCurrentTokenType(tokenType: "keyword");
            this.PlaceCaret("{");
            this.VerifyCurrentTokenType(tokenType: "punctuation");
            this.PlaceCaret("Main");
            this.VerifyCurrentTokenType(tokenType: "identifier");
            this.PlaceCaret("Hello");
            this.VerifyCurrentTokenType(tokenType: "string");
            this.PlaceCaret("<summary", charsOffset: -1);
            this.SendKeys(new KeyPress(VirtualKey.Right, ShiftState.Alt));
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            this.PlaceCaret("summary");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - name");
            this.PlaceCaret("innertext");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - text");
            this.PlaceCaret("!--");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            this.PlaceCaret("comment");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - comment");
            this.PlaceCaret("CDATA");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            this.PlaceCaret("cdata");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - cdata section");
            this.PlaceCaret("attribute");
            this.VerifyCurrentTokenType(tokenType: "identifier");
            this.PlaceCaret("Environment");
            this.VerifyCurrentTokenType(tokenType: "class name");
        }
    }
}