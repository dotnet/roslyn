// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicClassification : AbstractIdeEditorTest
    {
        public BasicClassification()
            : base(nameof(BasicClassification))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task Verify_Color_Of_Some_TokensAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"Imports System
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

            await VisualStudio.Editor.PlaceCaretAsync("MathAlias");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "identifier");
            await VisualStudio.Editor.PlaceCaretAsync("Namespace");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "keyword");
            await VisualStudio.Editor.PlaceCaretAsync("summary");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - name");
            await VisualStudio.Editor.PlaceCaretAsync("innertext");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - text");
            await VisualStudio.Editor.PlaceCaretAsync("!--");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter");
            await VisualStudio.Editor.PlaceCaretAsync("comment");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - comment");
            await VisualStudio.Editor.PlaceCaretAsync("CDATA");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter");
            await VisualStudio.Editor.PlaceCaretAsync("cdata");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "xml doc comment - cdata section");
            await VisualStudio.Editor.PlaceCaretAsync("attribute");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "identifier");
            await VisualStudio.Editor.PlaceCaretAsync("Class");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "keyword");
            await VisualStudio.Editor.PlaceCaretAsync("Program");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "class name");
            await VisualStudio.Editor.PlaceCaretAsync("Hello");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "string");
            await VisualStudio.Editor.PlaceCaretAsync("comment");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "comment");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task Semantic_ClassificationAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
Imports System
Class Goo
    Inherits Attribute
End Class");
            await VisualStudio.Editor.PlaceCaretAsync("Goo");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "class name");
            await VisualStudio.Editor.PlaceCaretAsync("Attribute");
            await VisualStudio.Editor.Verify.CurrentTokenTypeAsync(tokenType: "class name");
        }
    }
}
