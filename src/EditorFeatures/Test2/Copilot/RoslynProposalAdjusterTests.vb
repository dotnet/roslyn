' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Copilot
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Copilot
    <UseExportProvider>
    Public NotInheritable Class RoslynProposalAdjusterTests
        Private Shared ReadOnly s_composition As TestComposition = FeaturesTestCompositions.Features

        Private Shared Async Function Test(
                code As String,
                expected As String,
                language As String,
                Optional fixers As ImmutableHashSet(Of String) = Nothing,
                Optional compilationOptions As CompilationOptions = Nothing) As Task
            Using workspace = If(language Is LanguageNames.CSharp,
                    EditorTestWorkspace.CreateCSharp(code, compilationOptions:=compilationOptions, composition:=s_composition),
                    EditorTestWorkspace.CreateVisualBasic(code, compilationOptions:=compilationOptions, composition:=s_composition))
                Dim documentId = workspace.Documents.First().Id
                Dim proposalSpans = workspace.Documents.First().SelectedSpans

                Dim sourceText = Await workspace.CurrentSolution.GetDocument(documentId).GetTextAsync()

                ' Get the original document without the proposal edit in it.
                Dim originalDocument = workspace.CurrentSolution.GetDocument(documentId).WithText(
                    sourceText.WithChanges(proposalSpans.Select(Function(s) New TextChange(s, newText:=""))))

                Dim changes = New List(Of TextChange)()
                Dim delta = 0
                For Each selectionSpan In proposalSpans
                    changes.Add(New TextChange(
                        New TextSpan(selectionSpan.Start + delta, 0), newText:=sourceText.ToString(selectionSpan)))

                    delta -= selectionSpan.Length
                Next

                ' Default to all the flags on if adjuster not specified.
                If fixers Is Nothing Then
                    fixers = {
                        ProposalAdjusterKinds.AddMissingTokens,
                        ProposalAdjusterKinds.AddMissingImports,
                        ProposalAdjusterKinds.FormatCode
                    }.ToImmutableHashSet()
                End If

                Dim service = originalDocument.GetRequiredLanguageService(Of ICopilotProposalAdjusterService)
                Dim tuple = Await service.TryAdjustProposalAsync(
                    allowableAdjustments:=fixers, originalDocument, CopilotUtilities.TryNormalizeCopilotTextChanges(changes), lineFormattingOptions:=Nothing, CancellationToken.None)

                Dim adjustedChanges = tuple.TextChanges
                Dim format = tuple.Format
                Dim originalDocumentText = Await originalDocument.GetTextAsync()
                Dim adjustedDocumentTextAndFinalSpans = CopilotUtilities.GetNewTextAndChangedSpans(originalDocumentText, adjustedChanges)
                Dim adjustedDocumentText = adjustedDocumentTextAndFinalSpans.newText
                Dim finalSpans = adjustedDocumentTextAndFinalSpans.newSpans

                If format Then
                    Dim adjustedDocument = originalDocument.WithText(adjustedDocumentText)
                    Dim formattedDocument = Await Formatter.FormatAsync(adjustedDocument, finalSpans)
                    Dim formattedText = Await formattedDocument.GetTextAsync()
                    adjustedDocumentText = formattedText
                End If

                AssertEx.Equal(expected, adjustedDocumentText.ToString())
            End Using
        End Function

#Region "C#"

        Private Shared Async Function TestCSharp(code As String, expected As String, Optional fixers As ImmutableHashSet(Of String) = Nothing) As Task
            Await Test(code, expected, LanguageNames.CSharp, fixers)
        End Function

        <WpfFact>
        Public Async Function TestCSharp1() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_ExistingUsingAfter() As Task
            Await TestCSharp("
using Test;

class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
using System;
using Test;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_ExistingUsingBefore() As Task
            Await TestCSharp("
using System;

class C
{
    void M()
    {
        [|Task.Yield();|]
    }
}", "
using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        Task.Yield();
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_PartiallyWritten() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        Con[|sole.WriteLine(1);|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_AddMultiple_Different() As Task
            Await TestCSharp("
using System.Collections.Generic;

class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
        if (true) { }
        [|Task.Yield();|]
    }
}", "
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    void M()
    {
        Console.WriteLine(1);
        if (true) { }
        Task.Yield();
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_AddMultiple_Same() As Task
            Await TestCSharp("
using System.Collections.Generic;

class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
        if (true) { }
        [|Console.WriteLine();|]
    }
}", "
using System;
using System.Collections.Generic;

class C
{
    void M()
    {
        Console.WriteLine(1);
        if (true) { }
        Console.WriteLine();
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBrace1() As Task
            Await TestCSharp("
class C
{
    void M()
    [|{
        Console.WriteLine(1);|]
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBrace2() As Task
            Await TestCSharp("
class C
{
    void M()
    [|{
        Console.WriteLine(1);|]
", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}
")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBrace3() As Task
            Await TestCSharp("
class C
{
    void M()
    [|{
        Console.WriteLine(1);|]

    public void N() { }
}
", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }

    public void N() { }
}
")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_RequiresFormatting() As Task
            Await TestCSharp("
using System;

class C
{
    void M()
    {
            [| Console  .  WriteLine ( 1 )   ;|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_RequiresUsingAndFormatting() As Task
            Await TestCSharp("
class C
{
    void M()
    {
            [| Console  .  WriteLine ( 1 )   ;|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBraceUsingAndFormatting() As Task
            Await TestCSharp("
class C
{
    void M()
    [|{
        Console  . WriteLine( 1 )  ;|]

    public void N() { }
}
", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }

    public void N() { }
}
")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBraceAndFormattingPlusWhiteSpaceAfter() As Task
            ' Note that the trailing whitespace after the proposal causes the AddMissingTokens fixer
            ' to not add the closing brace. This could be improved in the future.
            Await TestCSharp("
class C
{
    void M()
    [|{
        System.Console  . WriteLine( 1 )  ; |]

    public void N() { }
}
", "
class C
{
    void M()
    {
        System.Console.WriteLine(1);

    public void N() { }
}
")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingBraceAndFormatting() As Task
            Await TestCSharp("
class C
{
    void M()
    [|{
        System.Console  . WriteLine( 1 )  ;|]

    public void N() { }
}
", "
class C
{
    void M()
    {
        System.Console.WriteLine(1);
    }

    public void N() { }
}
")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_Multi_Line_Formatting() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [| if (false) {
System.Console  .  WriteLine ( 1 )   ; } |]
    }
}", "
class C
{
    void M()
    {
        if (false)
        {
            System.Console.WriteLine(1);
        }
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_Formatting_Outside_Proposal() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [| Console  .  WriteLine ( 1 )   ;|]
            if (    true    ) {
            [| Console  .  WriteLine ( 1 )   ;|]
            }
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
        if (true)
        {
            Console.WriteLine(1);
        }
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_Partial_Formatting() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [| System . Console  .  Writ|]
    }
}", "
class C
{
    void M()
    {
        System.Console.Writ
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_AnalyzersOff() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}", ImmutableHashSet(Of String).Empty)
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingImportsOnly() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}", {ProposalAdjusterKinds.AddMissingImports}.ToImmutableHashSet())
        End Function

        <WpfFact>
        Public Async Function TestCSharp_MissingTokensOnly() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}", {ProposalAdjusterKinds.AddMissingTokens}.ToImmutableHashSet())
        End Function

        <WpfFact>
        Public Async Function TestCSharp_FormatOnly() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}", {ProposalAdjusterKinds.FormatCode}.ToImmutableHashSet())
        End Function

        <WpfFact>
        Public Async Function TestCSharp_LineEndingsPreserved_LfOnly() As Task
            ' Ensure that when the original document uses LF-only line endings, the adjusted
            ' text changes also use LF-only line endings (not CRLF).
            Await TestLineEndingsPreserved(vbLf)
        End Function

        <WpfFact>
        Public Async Function TestCSharp_LineEndingsPreserved_CrLf() As Task
            ' Ensure that when the original document uses CRLF line endings, the adjusted
            ' text changes also use CRLF line endings.
            Await TestLineEndingsPreserved(vbCrLf)
        End Function

        Private Shared Async Function TestLineEndingsPreserved(lineEnding As String) As Task
            ' Build a C# source file that uses the specified line ending and that will
            ' trigger the AddMissingImports adjuster
            Dim nl = lineEnding
            Dim originalCode =
                "class C" & nl &
                "{" & nl &
                "    void M()" & nl &
                "    {" & nl &
                "    }" & nl &
                "}"

            Dim proposalText = "Console.WriteLine(1);"

            Using workspace = EditorTestWorkspace.CreateCSharp(
                    originalCode, composition:=s_composition)
                Dim documentId = workspace.Documents.First().Id
                Dim originalDocument = workspace.CurrentSolution.GetDocument(documentId)
                Dim originalText = Await originalDocument.GetTextAsync()

                ' Verify the document actually has the line endings we expect.
                Assert.True(originalText.ToString().Contains(lineEnding))

                Dim insertPos = originalCode.IndexOf("    {" & nl, originalCode.IndexOf("void M()")) + ("    {" & nl).Length
                Dim changes = CopilotUtilities.TryNormalizeCopilotTextChanges(
                    {New TextChange(New TextSpan(insertPos, 0), "        " & proposalText & nl)})

                Dim fixers = {
                    ProposalAdjusterKinds.AddMissingImports,
                    ProposalAdjusterKinds.FormatCode
                }.ToImmutableHashSet()

                Dim service = originalDocument.GetRequiredLanguageService(Of ICopilotProposalAdjusterService)
                Dim result = Await service.TryAdjustProposalAsync(
                    allowableAdjustments:=fixers, originalDocument, changes, lineFormattingOptions:=Nothing, CancellationToken.None)

                ' The adjuster should have made changes (at minimum, adding "using System;").
                Assert.False(result.TextChanges.IsDefaultOrEmpty)

                ' Verify that every line ending in every TextChange.NewText matches the
                ' original document's line ending style.
                For Each change In result.TextChanges
                    Dim newText = change.NewText
                    If newText Is Nothing Then Continue For

                    If lineEnding = vbLf Then
                        ' LF-only: there should be no CR characters at all.
                        Assert.DoesNotContain(vbCr, newText)
                    Else
                        ' CRLF: every LF should be preceded by a CR.
                        For i = 0 To newText.Length - 1
                            If newText(i) = CChar(vbLf) Then
                                Assert.True(i > 0 AndAlso newText(i - 1) = CChar(vbCr),
                                    $"Found bare LF at position {i} in TextChange.NewText: ""{newText}""")
                            End If
                        Next
                    End If
                Next
            End Using
        End Function

        <WpfFact>
        Public Async Function TestCSharp_LineEndingsPreserved_Mixed() As Task
            ' Build a document that uses CRLF for most lines but LF for the line inside the
            ' method body.  The adjuster should preserve each line's original ending in the
            ' corresponding TextChange, even when they differ within a single change span.
            Dim crlf = vbCrLf
            Dim lf = vbLf
            Dim originalCode =
                "class C" & crlf &
                "{" & crlf &
                "    void M()" & crlf &
                "    {" & lf &
                "    }" & crlf &
                "}"

            Dim proposalText = "Console.WriteLine(1);"

            Using workspace = EditorTestWorkspace.CreateCSharp(
                    originalCode, composition:=s_composition)
                Dim documentId = workspace.Documents.First().Id
                Dim originalDocument = workspace.CurrentSolution.GetDocument(documentId)
                Dim originalText = Await originalDocument.GetTextAsync()

                ' The document should contain both CRLF and bare-LF.
                Dim originalString = originalText.ToString()
                Assert.Contains(crlf, originalString)
                Assert.True(originalString.Contains("    {" & lf))

                Dim insertPos = originalCode.IndexOf("    {" & lf) + ("    {" & lf).Length
                Dim changes = CopilotUtilities.TryNormalizeCopilotTextChanges(
                    {New TextChange(New TextSpan(insertPos, 0), "        " & proposalText & lf)})

                Dim fixers = {
                    ProposalAdjusterKinds.AddMissingImports,
                    ProposalAdjusterKinds.FormatCode
                }.ToImmutableHashSet()

                Dim service = originalDocument.GetRequiredLanguageService(Of ICopilotProposalAdjusterService)
                Dim result = Await service.TryAdjustProposalAsync(
                    allowableAdjustments:=fixers, originalDocument, changes, lineFormattingOptions:=Nothing, CancellationToken.None)

                Assert.False(result.TextChanges.IsDefaultOrEmpty)

                ' For each change, collect the line endings from the original text within
                ' the change's span, then verify the NewText uses those same endings in order.
                For Each change In result.TextChanges
                    Dim newText = change.NewText
                    If newText Is Nothing Then Continue For

                    ' Collect expected line endings from the original span.
                    Dim expectedEndings = New List(Of String)()
                    Dim startLine = originalText.Lines.GetLineFromPosition(change.Span.Start).LineNumber
                    Dim endLine = originalText.Lines.GetLineFromPosition(change.Span.End).LineNumber
                    For i = startLine To endLine
                        Dim line = originalText.Lines(i)
                        If line.End >= change.Span.Start AndAlso line.End < change.Span.End Then
                            Dim breakLen = line.EndIncludingLineBreak - line.End
                            If breakLen = 2 Then
                                expectedEndings.Add(crlf)
                            ElseIf breakLen = 1 Then
                                expectedEndings.Add(If(originalString(line.End) = CChar(vbLf), lf, vbCr))
                            End If
                        End If
                    Next

                    ' Collect actual line endings from the new text.
                    Dim actualEndings = New List(Of String)()
                    Dim j = 0
                    While j < newText.Length
                        If newText(j) = CChar(vbCr) AndAlso j + 1 < newText.Length AndAlso newText(j + 1) = CChar(vbLf) Then
                            actualEndings.Add(crlf)
                            j += 2
                        ElseIf newText(j) = CChar(vbLf) Then
                            actualEndings.Add(lf)
                            j += 1
                        ElseIf newText(j) = CChar(vbCr) Then
                            actualEndings.Add(vbCr)
                            j += 1
                        Else
                            j += 1
                        End If
                    End While

                    ' The endings that overlap with original span positions should match 1:1.
                    For i = 0 To Math.Min(expectedEndings.Count, actualEndings.Count) - 1
                        Assert.Equal(expectedEndings(i), actualEndings(i))
                    Next
                Next
            End Using
        End Function

        <WpfFact>
        Public Async Function TestCSharp_LineFormattingOptions_OverridesDocumentNewLine() As Task
            ' A CRLF document, but we pass LineFormattingOptions with LF as the newline.
            Dim crlf = vbCrLf
            Dim lf = vbLf

            Dim originalCode =
                "class C" & crlf &
                "{" & crlf &
                "    void M()" & crlf &
                "    {" & crlf &
                "    }" & crlf &
                "}"

            Dim proposalText = "Console.WriteLine(1);"

            Using workspace = EditorTestWorkspace.CreateCSharp(
                    originalCode, composition:=s_composition)
                Dim documentId = workspace.Documents.First().Id
                Dim originalDocument = workspace.CurrentSolution.GetDocument(documentId)

                Dim insertPos = originalCode.IndexOf("    {" & crlf, originalCode.IndexOf("void M()")) + ("    {" & crlf).Length
                Dim changes = CopilotUtilities.TryNormalizeCopilotTextChanges(
                    {New TextChange(New TextSpan(insertPos, 0), "        " & proposalText & crlf)})

                ' Pass LineFormattingOptions that say the file uses LF.
                Dim lfOptions = New LineFormattingOptions() With {.NewLine = lf}

                Dim fixers = {
                    ProposalAdjusterKinds.AddMissingImports,
                    ProposalAdjusterKinds.FormatCode
                }.ToImmutableHashSet()

                Dim service = originalDocument.GetRequiredLanguageService(Of ICopilotProposalAdjusterService)
                Dim result = Await service.TryAdjustProposalAsync(
                    allowableAdjustments:=fixers, originalDocument, changes, lineFormattingOptions:=lfOptions, CancellationToken.None)

                Assert.False(result.TextChanges.IsDefaultOrEmpty)

                Dim usingChange = result.TextChanges.FirstOrDefault(
                    Function(c) c.NewText IsNot Nothing AndAlso c.NewText.Contains("using System"))
                Assert.NotNull(usingChange.NewText)
            End Using
        End Function

        <WpfFact>
        Public Sub TestCSharp_FixLineEndingBoundaries_NewTextStartsWithLfAfterCr()
            ' "AB\r\nCD\r\nEF" - insert "\nX" at position 3 (between \r and \n).
            ' NewText[0]=\n and preceding char is \r, so would be rejected.
            ' The leading \n is dropped.
            Dim originalText = SourceText.From("AB" & vbCrLf & "CD" & vbCrLf & "EF")
            Dim changes = ImmutableArray.Create(
                New TextChange(New TextSpan(3, 0), vbLf & "X"))

            Dim fixed = AbstractCopilotProposalAdjusterService.TestAccessor.FixLineEndingBoundaries(originalText, changes)
            Assert.Single(fixed)
            Assert.Equal(4, fixed(0).Span.Start)
            Assert.Equal(4, fixed(0).Span.End)
            Assert.Equal("X", fixed(0).NewText)

            Dim result = originalText.WithChanges(fixed)
            Assert.Equal("AB" & vbCrLf & "XCD" & vbCrLf & "EF", result.ToString())
        End Sub

        <WpfFact>
        Public Sub TestCSharp_FixLineEndingBoundaries_NewTextEndsWithCrBeforeLf()
            ' "AB\r\nCD\r\nEF" - insert "X\r" at position 7 (between \r and \n).
            ' NewText[^1]=\r and following char is \n, so would be rejected.
            ' The trailing \r is dropped.
            Dim originalText = SourceText.From("AB" & vbCrLf & "CD" & vbCrLf & "EF")
            Dim changes = ImmutableArray.Create(
                New TextChange(New TextSpan(7, 0), "X" & vbCr))

            Dim fixed = AbstractCopilotProposalAdjusterService.TestAccessor.FixLineEndingBoundaries(originalText, changes)
            Assert.Single(fixed)
            Assert.Equal(6, fixed(0).Span.Start)
            Assert.Equal(6, fixed(0).Span.End)
            Assert.Equal("X", fixed(0).NewText)

            Dim result = originalText.WithChanges(fixed)
            Assert.Equal("AB" & vbCrLf & "CDX" & vbCrLf & "EF", result.ToString())
        End Sub

        <WpfFact>
        Public Sub TestCSharp_FixLineEndingBoundaries_NoBoundaryIssue()
            ' No boundary issue, returned unchanged.
            Dim originalText = SourceText.From("AB" & vbCrLf & "CD" & vbCrLf & "EF")
            Dim changes = ImmutableArray.Create(
                New TextChange(New TextSpan(4, 2), "XY"))

            Dim fixed = AbstractCopilotProposalAdjusterService.TestAccessor.FixLineEndingBoundaries(originalText, changes)
            Assert.Single(fixed)
            Assert.Equal(4, fixed(0).Span.Start)
            Assert.Equal(6, fixed(0).Span.End)
            Assert.Equal("XY", fixed(0).NewText)

            Dim result = originalText.WithChanges(fixed)
            Assert.Equal("AB" & vbCrLf & "XY" & vbCrLf & "EF", result.ToString())
        End Sub

        <WpfFact>
        Public Sub TestCSharp_FixLineEndingBoundaries_AdjacentChangesSplitCrLf()
            ' Two adjacent changes split a \r\n pair across the boundary.
            ' "ABC\r" + "\nDEF" - the trailing \r and leading \n are dropped,
            ' and the spans are shrunk since the original chars match.
            Dim originalText = SourceText.From("abc" & vbCrLf & "def")
            Dim changes = ImmutableArray.Create(
                New TextChange(New TextSpan(0, 4), "ABC" & vbCr),
                New TextChange(New TextSpan(4, 4), vbLf & "DEF"))

            Dim fixed = AbstractCopilotProposalAdjusterService.TestAccessor.FixLineEndingBoundaries(originalText, changes)
            Assert.Equal(2, fixed.Length)

            ' First change: trailing \r dropped, span shrunk from [0,4) to [0,3).
            Assert.Equal(0, fixed(0).Span.Start)
            Assert.Equal(3, fixed(0).Span.End)
            Assert.Equal("ABC", fixed(0).NewText)

            ' Second change: leading \n dropped, span shrunk from [4,8) to [5,8).
            Assert.Equal(5, fixed(1).Span.Start)
            Assert.Equal(8, fixed(1).Span.End)
            Assert.Equal("DEF", fixed(1).NewText)

            ' The original \r\n at positions 3-4 is preserved.
            Dim result = originalText.WithChanges(fixed)
            Assert.Equal("ABC" & vbCrLf & "DEF", result.ToString())
        End Sub

#End Region

#Region "Visual Basic"

        Private Shared Async Function TestVisualBasic(code As String, expected As String, Optional fixers As ImmutableHashSet(Of String) = Nothing) As Task
            Await Test(code, expected, LanguageNames.VisualBasic, fixers, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic1() As Task
            Await TestVisualBasic("
class C
    sub M()
        [|Console.WriteLine(1)|]
    end sub
end class", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_ExistingImportsAfter() As Task
            Await TestVisualBasic("
Imports Test

class C
    sub M()
        [|Console.WriteLine(1)|]
    end sub
end class", "
Imports System
Imports Test

class C
    sub M()
        Console.WriteLine(1)
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_ExistingImportsBefore() As Task
            Await TestVisualBasic("
Imports System

class C
    sub M()
        [|Task.Yield()|]
    end sub
end class", "
Imports System
Imports System.Threading.Tasks

class C
    sub M()
        Task.Yield()
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_PartiallyWritten() As Task
            Await TestVisualBasic("
class C
    sub M()
        Con[|sole.WriteLine(1)|]
    end sub
end class", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_AddMultiple_Different() As Task
            Await TestVisualBasic("
Imports System.Collections.Generic

class C
    sub M()
        [|Console.WriteLine(1)|]
        if (true)
        end if
        [|Task.Yield()|]
    end sub
end class", "
Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks

class C
    sub M()
        Console.WriteLine(1)
        if (true)
        end if
        Task.Yield()
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_AddMultiple_Same() As Task
            Await TestVisualBasic("
Imports System.Collections.Generic

class C
    sub M()
        [|Console.WriteLine(1)|]
        if (true)
        end if
        [|Console.WriteLine()|]
    end sub
end class", "
Imports System
Imports System.Collections.Generic

class C
    sub M()
        Console.WriteLine(1)
        if (true)
        end if
        Console.WriteLine()
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_RequiresFormatting() As Task
            Await TestVisualBasic("
Imports System

class C
    sub M()
        [| Console . WriteLine ( 1 )   |]
    end sub
end class", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_RequiresUsingAndFormatting() As Task
            Await TestVisualBasic("
class C
    sub M()
        [| Console . WriteLine ( 1 )   |]
    end sub
end class", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_Multi_Line_Formatting() As Task
            Await TestVisualBasic("
class C
    sub M()
        [| Console . WriteLine ( 1 )   |]
        if (false)
        end if
        [| Console . WriteLine ( 1 )   |]
    end sub
end class
", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
        if (false)
        end if
        Console.WriteLine(1)
    end sub
end class
")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_Formatting_Outside_Proposal() As Task
            Await TestVisualBasic("
class C
    sub M()
        [| Console  .  WriteLine ( 1 )  |]
            if (    true    )
            [| Console  .  WriteLine ( 1 )   |]
            end if
    end sub
end class", "
Imports System

class C
    sub M()
        Console.WriteLine(1)
        if (true)
            Console.WriteLine(1)
        end if
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_Partial_Formatting() As Task
            Await TestVisualBasic("
class C
    sub M()
        [| System .  Console  .  Writ|]
    end sub
end class", "
class C
    sub M()
        System.Console.Writ
    end sub
end class")
        End Function

        <WpfFact>
        Public Async Function TestVisualBasic_AnalyzersOff() As Task
            Await TestVisualBasic("
class C
    sub M()
        [| Console . WriteLine ( 1 )   |]
    end sub
end class", "
class C
    sub M()
         Console . WriteLine ( 1 )   
    end sub
end class", ImmutableHashSet(Of String).Empty)
        End Function

#End Region
    End Class
End Namespace
