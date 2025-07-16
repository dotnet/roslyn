' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Public Class ScanConditionalTests

    <Fact>
    Public Sub Scanner_ConditionalSkipEol()
        Dim Str = <text>
#hi
                  </text>.Value

        Using s As New InternalSyntax.Scanner(SourceText.From(Str), TestOptions.Regular)
            Dim res = s.SkipToNextConditionalLine
            Assert.Equal(0, res.Start)
            Assert.Equal(1, res.Length)

            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)
        End Using
    End Sub

    <Fact>
    Public Sub Scanner_ConditionalSkipSomeText()
        Dim Str = <text>
blah
blah
#hi
boo</text>.Value

        Using s As New InternalSyntax.Scanner(SourceText.From(Str), TestOptions.Regular)

            ' grab the whole text (why not)
            Dim disabled = s.GetDisabledTextAt(New TextSpan(0, Str.Length))
            Assert.Equal(Str, disabled.ToFullString)

            Dim res = s.SkipToNextConditionalLine
            Assert.Equal(0, res.Start)
            Assert.Equal(11, res.Length)

            ' grab skipped text.
            disabled = s.GetDisabledTextAt(res)
            Assert.Equal(vbLf & "blah" & vbLf & "blah" & vbLf, disabled.ToFullString)

            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' get off #

            res = s.SkipToNextConditionalLine
            Assert.Equal(12, res.Start)
            Assert.Equal(6, res.Length)

            disabled = s.GetDisabledTextAt(res)
            Assert.Equal("hi" & vbLf & "boo", disabled.ToFullString)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB)
            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        End Using
    End Sub

    <Fact>
    Public Sub Scanner_ConditionalSkipTwice()
        Dim Str = <text>
blah
blah
#hi
boo
#hi</text>.Value

        Using s As New InternalSyntax.Scanner(SourceText.From(Str), TestOptions.Regular)
            Dim res = s.SkipToNextConditionalLine
            Assert.Equal(0, res.Start)
            Assert.Equal(11, res.Length)

            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #

            res = s.SkipToNextConditionalLine
            Assert.Equal(12, res.Start)
            Assert.Equal(7, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #
            res = s.SkipToNextConditionalLine
            Assert.Equal(20, res.Start)
            Assert.Equal(2, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        End Using
    End Sub

    <Fact>
    Public Sub Scanner_ConditionalLineCont()
        Dim Str = <text>blah _
# here
boo
#here _
_
_
#here _

#here _
_</text>.Value

        Using s As New InternalSyntax.Scanner(SourceText.From(Str), TestOptions.Regular)
            Dim res = s.SkipToNextConditionalLine
            Assert.Equal(0, res.Start)
            Assert.Equal(7, res.Length)

            Dim tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #

            res = s.SkipToNextConditionalLine
            Assert.Equal(9, res.Start)
            Assert.Equal(9, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #

            res = s.SkipToNextConditionalLine
            Assert.Equal(19, res.Start)
            Assert.Equal(11, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #

            res = s.SkipToNextConditionalLine
            Assert.Equal(31, res.Start)
            Assert.Equal(8, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.HashToken, tk.Kind)

            s.GetNextTokenInState(InternalSyntax.ScannerState.VB) ' skip #

            res = s.SkipToNextConditionalLine
            Assert.Equal(40, res.Start)
            Assert.Equal(8, res.Length)

            tk = s.GetCurrentToken
            Assert.Equal(SyntaxKind.EndOfFileToken, tk.Kind)
        End Using
    End Sub
End Class
