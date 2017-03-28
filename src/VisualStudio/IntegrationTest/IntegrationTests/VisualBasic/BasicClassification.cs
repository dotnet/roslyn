// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
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

            this.PlaceCaret("MathAlias");
            this.VerifyCurrentTokenType(tokenType: "identifier");
            this.PlaceCaret("Namespace");
            this.VerifyCurrentTokenType(tokenType: "keyword");
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
            this.PlaceCaret("Class");
            this.VerifyCurrentTokenType(tokenType: "keyword");
            this.PlaceCaret("Program");
            this.VerifyCurrentTokenType(tokenType: "class name");
            this.PlaceCaret("Hello");
            this.VerifyCurrentTokenType(tokenType: "string");
            this.PlaceCaret("comment");
            this.VerifyCurrentTokenType(tokenType: "comment");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void Semantic_Classification()
        {
            Editor.SetText(@"
Imports System
Class Foo
    Inherits Attribute
End Class");
            this.PlaceCaret("Foo");
            this.VerifyCurrentTokenType(tokenType: "class name");
            this.PlaceCaret("Attribute");
            this.VerifyCurrentTokenType(tokenType: "class name");
        }
    }
}
