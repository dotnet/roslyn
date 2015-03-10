' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains extension method helpers for object derived from SyntaxNode
'-----------------------------------------------------------------------------

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Module ParserExtensions

        <Extension()>
        Friend Function Any(Of T As VisualBasicSyntaxNode)(this As SyntaxList(Of T),
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
        Friend Function AnyAndOnly(Of T As VisualBasicSyntaxNode)(this As SyntaxList(Of T),
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
        Friend Function ContainsDiagnostics(Of T As VisualBasicSyntaxNode)(this As SyntaxList(Of T)) As Boolean
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
                If this.Item(i).ContainsDiagnostics Then
                    Return True
                End If
            Next
            Return False
        End Function

    End Module
End Namespace

