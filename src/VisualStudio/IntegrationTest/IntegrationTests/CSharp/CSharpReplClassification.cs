// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
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
            VerifyCurrentTokenType(tokenType: "keyword");
            PlaceCaret("{");
            VerifyCurrentTokenType(tokenType: "punctuation");
            PlaceCaret("Main");
            VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("Hello");
            VerifyCurrentTokenType(tokenType: "string");
            PlaceCaret("<summary", charsOffset: -1);
            SendKeys(Alt(VirtualKey.Right));
            VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("summary");
            VerifyCurrentTokenType(tokenType: "xml doc comment - name");
            PlaceCaret("innertext");
            VerifyCurrentTokenType(tokenType: "xml doc comment - text");
            PlaceCaret("!--");
            VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("comment");
            VerifyCurrentTokenType(tokenType: "xml doc comment - comment");
            PlaceCaret("CDATA");
            VerifyCurrentTokenType(tokenType: "xml doc comment - delimiter");
            PlaceCaret("cdata");
            VerifyCurrentTokenType(tokenType: "xml doc comment - cdata section");
            PlaceCaret("attribute");
            VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("Environment");
            VerifyCurrentTokenType(tokenType: "class name");
        }
    }
}