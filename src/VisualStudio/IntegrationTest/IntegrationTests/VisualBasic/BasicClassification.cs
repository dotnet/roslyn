// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicClassification : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicClassification( )
            : base( nameof(BasicClassification))
        {
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
        public void Verify_Color_Of_Some_Tokens()
        {
            VisualStudioInstance.Editor.SetText(@"Imports System
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

            VisualStudioInstance.Editor.PlaceCaret("MathAlias");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudioInstance.Editor.PlaceCaret("Namespace");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "keyword");
            VisualStudioInstance.Editor.PlaceCaret("summary");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - name");
            VisualStudioInstance.Editor.PlaceCaret("innertext");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - text");
            VisualStudioInstance.Editor.PlaceCaret("!--");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudioInstance.Editor.PlaceCaret("comment");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - comment");
            VisualStudioInstance.Editor.PlaceCaret("CDATA");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudioInstance.Editor.PlaceCaret("cdata");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - cdata section");
            VisualStudioInstance.Editor.PlaceCaret("attribute");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudioInstance.Editor.PlaceCaret("Class");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "keyword");
            VisualStudioInstance.Editor.PlaceCaret("Program");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudioInstance.Editor.PlaceCaret("Hello");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "string");
            VisualStudioInstance.Editor.PlaceCaret("comment");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "comment");
        }

        [TestMethod, TestCategory(Traits.Features.Classification)]
        public void Semantic_Classification()
        {
            VisualStudioInstance.Editor.SetText(@"
Imports System
Class Goo
    Inherits Attribute
End Class");
            VisualStudioInstance.Editor.PlaceCaret("Goo");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudioInstance.Editor.PlaceCaret("Attribute");
            VisualStudioInstance.Editor.Verify.CurrentTokenType(tokenType: "class name");
        }
    }
}
