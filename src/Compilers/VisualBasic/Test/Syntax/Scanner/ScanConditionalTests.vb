' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Public Class ScanConditionalTests


    <Fact>
    Public Sub Scanner_ConditionalSkipEol()
        Dim Str = "
#hi
"

        Using s As New InternalSyntax.Scanner(SourceText.From(Str), TestOptions.Regular)
            Dim res = s.SkipToNextConditionalLine
            Assert.Equal(0, res.Start)
            Assert.Equal(2, res.Length)

            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)
        End Using
    End Sub

    <Fact>
    Public Sub Scanner_ConditionalSkipSomeText()
        Dim Str = "
blah
blah
#hi
boo"


        Using s As New InternalSyntax.Scanner(SourceText.From(Str), TestOptions.Regular)

            ' grab the whole text (why not)
            Dim disabled = s.GetDisabledTextAt(New TextSpan(0, Str.Length))
            Assert.Equal(Str, disabled.ToFullString)

            Dim res = s.SkipToNextConditionalLine
            Assert.Equal(0, res.Start)
            Assert.Equal(14, res.Length)

            ' grab skipped text.
            disabled = s.GetDisabledTextAt(res)
            Assert.Equal(vbCrLf & "blah" & vbCrLf & "blah" & vbCrLf, disabled.ToFullString)

            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' get off #

            res = s.SkipToNextConditionalLine
            Assert.Equal(15, res.Start)
            Assert.Equal(7, res.Length)

            disabled = s.GetDisabledTextAt(res)
            Assert.Equal("hi" & vbCrLf & "boo", disabled.ToFullString)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB)
            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        End Using
    End Sub

    <Fact>
    Public Sub Scanner_ConditionalSkipTwice()
        Dim Str = "
blah
blah
#hi
boo
#hi"

        Using s As New InternalSyntax.Scanner(SourceText.From(Str), TestOptions.Regular)
            Dim res = s.SkipToNextConditionalLine
            Assert.Equal(0, res.Start)
            Assert.Equal(14, res.Length)

            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #

            res = s.SkipToNextConditionalLine
            Assert.Equal(15, res.Start)
            Assert.Equal(9, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #
            res = s.SkipToNextConditionalLine
            Assert.Equal(25, res.Start)
            Assert.Equal(2, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        End Using
    End Sub

    <Fact>
    Public Sub Scanner_ConditionalLineCont()
        Dim Str = "blah _
# here
boo
#here _
_
_
#here _

#here _
_"

        Using s As New InternalSyntax.Scanner(SourceText.From(Str), TestOptions.Regular)
            Dim res = s.SkipToNextConditionalLine
            Assert.Equal(0, res.Start)
            Assert.Equal(8, res.Length)

            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #

            res = s.SkipToNextConditionalLine
            Assert.Equal(10, res.Start)
            Assert.Equal(11, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #

            res = s.SkipToNextConditionalLine
            Assert.Equal(22, res.Start)
            Assert.Equal(14, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #

            res = s.SkipToNextConditionalLine
            Assert.Equal(37, res.Start)
            Assert.Equal(10, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #

            res = s.SkipToNextConditionalLine
            Assert.Equal(48, res.Start)
            Assert.Equal(9, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        End Using
    End Sub
End Class
