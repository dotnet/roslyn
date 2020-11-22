' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains extension method helpers for object derived from SyntaxNode
'-----------------------------------------------------------------------------

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Module ParserExtensions

        <Extension>
        Function IsIn(kind As SyntaxKind,
                      kind0 As SyntaxKind, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind, kind4 As SyntaxKind,
                      kind5 As SyntaxKind, kind6 As SyntaxKind, kind7 As SyntaxKind, kind8 As SyntaxKind, kind9 As SyntaxKind) As Boolean
            Return (kind = kind0) Or (kind = kind1) Or (kind = kind2) Or (kind = kind3) Or (kind = kind4) Or
                   (kind = kind5) Or (kind = kind6) Or (kind = kind7) Or (kind = kind8) Or (kind = kind9)
        End Function

        <Extension>
        Function IsIn(kind As SyntaxKind,
                      kind0 As SyntaxKind, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind, kind4 As SyntaxKind,
                      kind5 As SyntaxKind, kind6 As SyntaxKind, kind7 As SyntaxKind, kind8 As SyntaxKind) As Boolean
            Return (kind = kind0) Or (kind = kind1) Or (kind = kind2) Or (kind = kind3) Or (kind = kind4) Or
                   (kind = kind5) Or (kind = kind6) Or (kind = kind7) Or (kind = kind8)
        End Function

        <Extension>
        Function IsIn(kind As SyntaxKind,
                      kind0 As SyntaxKind, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind, kind4 As SyntaxKind,
                      kind5 As SyntaxKind, kind6 As SyntaxKind, kind7 As SyntaxKind) As Boolean
            Return (kind = kind0) Or (kind = kind1) Or (kind = kind2) Or (kind = kind3) Or (kind = kind4) Or
                   (kind = kind5) Or (kind = kind6) Or (kind = kind7)
        End Function

        <Extension>
        Function IsIn(kind As SyntaxKind,
                      kind0 As SyntaxKind, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind, kind4 As SyntaxKind,
                      kind5 As SyntaxKind, kind6 As SyntaxKind) As Boolean
            Return (kind = kind0) Or (kind = kind1) Or (kind = kind2) Or (kind = kind3) Or (kind = kind4) Or
                   (kind = kind5) Or (kind = kind6)
        End Function

        <Extension>
        Function IsIn(kind As SyntaxKind,
                      kind0 As SyntaxKind, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind, kind4 As SyntaxKind,
                      kind5 As SyntaxKind) As Boolean
            Return (kind = kind0) Or (kind = kind1) Or (kind = kind2) Or (kind = kind3) Or (kind = kind4) Or
                   (kind = kind5)
        End Function

        <Extension>
        Function IsIn(kind As SyntaxKind,
                      kind0 As SyntaxKind, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind, kind4 As SyntaxKind) As Boolean
            Return (kind = kind0) Or (kind = kind1) Or (kind = kind2) Or (kind = kind3) Or (kind = kind4)
        End Function

        <Extension>
        Function IsIn(kind As SyntaxKind,
                      kind0 As SyntaxKind, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind) As Boolean
            Return (kind = kind0) Or (kind = kind1) Or (kind = kind2) Or (kind = kind3)
        End Function

        <Extension>
        Function IsIn(kind As SyntaxKind,
                      kind0 As SyntaxKind, kind1 As SyntaxKind, kind2 As SyntaxKind) As Boolean
            Return (kind = kind0) Or (kind = kind1) Or (kind = kind2)
        End Function

        <Extension>
        Function IsIn(kind As SyntaxKind, kind0 As SyntaxKind, kind1 As SyntaxKind) As Boolean
            Return (kind = kind0) Or (kind = kind1)
        End Function

        <Extension>
        Function IsIn(kind As SyntaxKind, kind0 As SyntaxKind) As Boolean
            Return (kind = kind0)
        End Function

        <Extension>
        Function ToListAndFree(Of T As GreenNode)(builder As SyntaxListBuilder(Of T), pool As SyntaxListPool) As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of T)
            Dim result = builder.ToList
            pool.free(builder)
            Return result
        End Function

        <Extension>
        Function ToListAndFree(Of T As GreenNode)(builder As SeparatedSyntaxListBuilder(Of T), pool As SyntaxListPool) As CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList(Of T)
            Dim result = builder.ToList
            pool.free(builder)
            Return result
        End Function

        <Extension()>
        Friend Function Any(Of T As VisualBasicSyntaxNode)(this As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of T),
                                                    ParamArray kinds As SyntaxKind()) As Boolean
            Debug.Assert(kinds IsNot Nothing)

            For i = 0 To kinds.Length - 1
                If this.Any(kinds(i)) Then
                    Return True
                End If
            Next
            Return False
        End Function

        <Extension()>
        Friend Function AnyAndOnly(Of T As VisualBasicSyntaxNode)(this As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of T),
                                                    ParamArray kinds As SyntaxKind()) As Boolean
            Debug.Assert(kinds IsNot Nothing)
            Dim found As Boolean = False

            For i = 0 To this.Count - 1
                found = kinds.Contains(this(i).Kind)
                If Not found Then
                    Return False
                End If
            Next
            Return found
        End Function

        <Extension()>
        Friend Function ContainsDiagnostics(Of T As VisualBasicSyntaxNode)(this As CodeAnalysis.Syntax.InternalSyntax.SyntaxList(Of T)) As Boolean
            For i = 0 To this.Count - 1
                If this.Item(i).ContainsDiagnostics Then
                    Return True
                End If
            Next
            Return False
        End Function

        <Extension()>
        Friend Function ContainsDiagnostics(Of T As VisualBasicSyntaxNode)(this As SyntaxListBuilder(Of T)) As Boolean
            For i = 0 To this.Count - 1
                If this(i).ContainsDiagnostics Then
                    Return True
                End If
            Next
            Return False
        End Function

    End Module
End Namespace

