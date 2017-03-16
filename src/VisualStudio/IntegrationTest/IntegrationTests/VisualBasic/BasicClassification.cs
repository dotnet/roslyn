// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicClassification : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicClassification(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicClassification))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void Verify_Color_Of_Some_Tokens()
        {
            Editor.SetText(@"Imports System
Imports MathAlias = System.Math
Namespace Acme
    ''' <summary>innertext
    ''' </summary>
    ''' <!--comment-->
    ''' <![CDATA[cdata]]>
    ''' <typeparam name=""attribute"" />
    Public Class Program
        Public Shared Sub Main(args As String())
            Console.WriteLine(""Hello World"") 'comment
        End Sub
    End Class
End Namespace");

            PlaceCaret("MathAlias");
            VerifyCurrentTokenType(tokenType: "identifier");
            PlaceCaret("Namespace");
            VerifyCurrentTokenType(tokenType: "keyword");
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
            PlaceCaret("Class");
            VerifyCurrentTokenType(tokenType: "keyword");
            PlaceCaret("Program");
            VerifyCurrentTokenType(tokenType: "class name");
            PlaceCaret("Hello");
            VerifyCurrentTokenType(tokenType: "string");
            PlaceCaret("comment");
            VerifyCurrentTokenType(tokenType: "comment");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void Semantic_Classification()
        {
            Editor.SetText(@"
Imports System
Class Foo
    Inherits Attribute
End Class");
            PlaceCaret("Foo");
            VerifyCurrentTokenType(tokenType: "class name");
            PlaceCaret("Attribute");
            VerifyCurrentTokenType(tokenType: "class name");
        }
    }
}
