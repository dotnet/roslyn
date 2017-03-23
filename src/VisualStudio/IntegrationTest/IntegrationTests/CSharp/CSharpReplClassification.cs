// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
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
            InsertCode(@"using System.Console;
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

            PlaceCaret("using");
            this.VerifyCurrentTokenType(tokenType: "keyword");
            PlaceCaret("{");
            this.VerifyCurrentTokenType(tokenType: "punctuation");
            PlaceCaret("Main");
            this.VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("Hello");
            this.VerifyCurrentTokenType(tokenType: "string");
            PlaceCaret("<summary", charsOffset: -1);
            SendKeys(new KeyPress(VirtualKey.Right, ShiftState.Alt));
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("summary");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - name");
            PlaceCaret("innertext");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - text");
            PlaceCaret("!--");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("comment");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - comment");
            PlaceCaret("CDATA");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("cdata");
            this.VerifyCurrentTokenType(tokenType: "xml doc comment - cdata section");
            PlaceCaret("attribute");
            this.VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("Environment");
            this.VerifyCurrentTokenType(tokenType: "class name");
        }
    }
}