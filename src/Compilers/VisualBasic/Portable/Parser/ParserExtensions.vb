' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        <Extension>
        Friend Function IsAnyOf(value As SyntaxKind, k0 As SyntaxKind, k1 As SyntaxKind) As Boolean
            Return (value = k0) OrElse (value = k1)
        End Function

        <Extension>
        Friend Function IsAnyOf(value As SyntaxKind, k0 As SyntaxKind, k1 As SyntaxKind, k2 As SyntaxKind) As Boolean
            Return (value = k0) OrElse (value = k1) OrElse (value = k2)
        End Function

        <Extension>
        Friend Function IsAnyOf(value As SyntaxKind, k0 As SyntaxKind, k1 As SyntaxKind, k2 As SyntaxKind, k3 As SyntaxKind) As Boolean
            Return (value = k0) OrElse (value = k1) OrElse (value = k2) OrElse (value = k3)
        End Function

        <Extension>
        Friend Function IsAnyOf(value As SyntaxKind, k0 As SyntaxKind, k1 As SyntaxKind, k2 As SyntaxKind, k3 As SyntaxKind,
                                                     k4 As SyntaxKind) As Boolean
            Return (value = k0) OrElse (value = k1) OrElse (value = k2) OrElse (value = k3) OrElse
                   (value = k4)
        End Function

        <Extension>
        Friend Function IsAnyOf(value As SyntaxKind, k0 As SyntaxKind, k1 As SyntaxKind, k2 As SyntaxKind, k3 As SyntaxKind,
                                                     k4 As SyntaxKind, k5 As SyntaxKind) As Boolean
            Return (value = k0) OrElse (value = k1) OrElse (value = k2) OrElse (value = k3) OrElse
                   (value = k4) OrElse (value = k5)
        End Function

        <Extension>
        Friend Function IsAnyOf(value As SyntaxKind, k0 As SyntaxKind, k1 As SyntaxKind, k2 As SyntaxKind, k3 As SyntaxKind,
                                                     k4 As SyntaxKind, k5 As SyntaxKind, k6 As SyntaxKind) As Boolean
            Return (value = k0) OrElse (value = k1) OrElse (value = k2) OrElse (value = k3) OrElse
                   (value = k4) OrElse (value = k5) OrElse (value = k6)
        End Function

        <Extension>
        Friend Function IsAnyOf(value As SyntaxKind, k0 As SyntaxKind, k1 As SyntaxKind, k2 As SyntaxKind, k3 As SyntaxKind,
                                                     k4 As SyntaxKind, k5 As SyntaxKind, k6 As SyntaxKind, k7 As SyntaxKind) As Boolean
            Return (value = k0) OrElse (value = k1) OrElse (value = k2) OrElse (value = k3) OrElse
                   (value = k4) OrElse (value = k5) OrElse (value = k6) OrElse (value = k7)
        End Function

        <Extension>
        Friend Function IsAnyOf(value As SyntaxKind, k0 As SyntaxKind, k1 As SyntaxKind, k2 As SyntaxKind, k3 As SyntaxKind,
                                                     k4 As SyntaxKind, k5 As SyntaxKind, k6 As SyntaxKind, k7 As SyntaxKind,
                                                     k8 As SyntaxKind) As Boolean
            Return (value = k0) OrElse (value = k1) OrElse (value = k2) OrElse (value = k3) OrElse
                   (value = k4) OrElse (value = k5) OrElse (value = k6) OrElse (value = k7) OrElse
                   (value = k8)
        End Function

        <Extension>
        Friend Function IsAnyOf(value As SyntaxKind, k0 As SyntaxKind, k1 As SyntaxKind, k2 As SyntaxKind, k3 As SyntaxKind,
                                                     k4 As SyntaxKind, k5 As SyntaxKind, k6 As SyntaxKind, k7 As SyntaxKind,
                                                     k8 As SyntaxKind, k9 As SyntaxKind, k10 As SyntaxKind, k11 As SyntaxKind,
                                                     k12 As SyntaxKind) As Boolean
            Return (value = k0) OrElse (value = k1) OrElse (value = k2) OrElse (value = k3) OrElse
                   (value = k4) OrElse (value = k5) OrElse (value = k6) OrElse (value = k7) OrElse
                   (value = k8) OrElse (value = k9) OrElse (value = k10) OrElse (value = k11) OrElse
                   (value = k12)
        End Function

        <Extension>
        Friend Function IsKindAndKeywordKind(CurrentToken As SyntaxToken, OfKind As SyntaxKind, KeywordKind As SyntaxKind) As Boolean
            Return (CurrentToken.Kind = OfKind) AndAlso
                   DirectCast(CurrentToken, IdentifierTokenSyntax).PossibleKeywordKind = KeywordKind

        End Function

    End Module
End Namespace

