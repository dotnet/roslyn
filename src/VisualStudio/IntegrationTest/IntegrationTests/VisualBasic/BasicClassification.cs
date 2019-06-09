// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicClassification : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicClassification(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(BasicClassification))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void Verify_Color_Of_Some_Tokens()
        {
            VisualStudio.Editor.SetText(@"Imports System
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

            VisualStudio.Editor.PlaceCaret("MathAlias");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudio.Editor.PlaceCaret("Namespace");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "keyword");
            VisualStudio.Editor.PlaceCaret("summary");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - name");
            VisualStudio.Editor.PlaceCaret("innertext");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - text");
            VisualStudio.Editor.PlaceCaret("!--");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudio.Editor.PlaceCaret("comment");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - comment");
            VisualStudio.Editor.PlaceCaret("CDATA");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - delimiter");
            VisualStudio.Editor.PlaceCaret("cdata");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "xml doc comment - cdata section");
            VisualStudio.Editor.PlaceCaret("attribute");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "identifier");
            VisualStudio.Editor.PlaceCaret("Class");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "keyword");
            VisualStudio.Editor.PlaceCaret("Program");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudio.Editor.PlaceCaret("Hello");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "string");
            VisualStudio.Editor.PlaceCaret("comment");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "comment");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void Semantic_Classification()
        {
            VisualStudio.Editor.SetText(@"
Imports System
Class Goo
    Inherits Attribute
End Class");
            VisualStudio.Editor.PlaceCaret("Goo");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudio.Editor.PlaceCaret("Attribute");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
        }
    }
}
