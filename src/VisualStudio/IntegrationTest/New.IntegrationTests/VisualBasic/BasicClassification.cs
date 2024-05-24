// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.Classification)]
public class BasicClassification : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicClassification() : base(nameof(BasicClassification))
    {
    }

    [IdeFact]
    public async Task Verify_Color_Of_Some_Tokens()
    {
        await TestServices.Editor.SetTextAsync(@"Imports System
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
End Namespace", HangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("MathAlias", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Namespace", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "keyword", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("summary", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - name", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("innertext", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - text", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("!--", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("comment", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - comment", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("CDATA", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - delimiter", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("cdata", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "xml doc comment - cdata section", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("attribute", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "identifier", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Class", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "keyword", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Program", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Hello", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "string", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("comment", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "comment", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Semantic_Classification()
    {
        await TestServices.Editor.SetTextAsync(@"
Imports System
Class Goo
    Inherits Attribute
End Class", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Goo", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Attribute", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentTokenTypeAsync(tokenType: "class name", HangMitigatingCancellationToken);
    }
}
