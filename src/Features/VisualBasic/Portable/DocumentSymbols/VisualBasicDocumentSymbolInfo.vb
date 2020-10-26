' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.DocumentSymbols

Namespace Microsoft.CodeAnalysis.VisualBasic.DocumentSymbols

    Friend Class VisualBasicDocumentSymbolInfo
        Inherits DocumentSymbolInfo

        Private Shared ReadOnly s_typeFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance)

        Friend Shared ReadOnly MemberFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
            parameterOptions:=SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)


        Public Sub New(symbol As ISymbol, childrenSymbols As ImmutableArray(Of DocumentSymbolInfo))
            MyBase.New(symbol, childrenSymbols)
        End Sub

        Public Sub New(symbol As ISymbol)
            Me.New(symbol, ImmutableArray(Of DocumentSymbolInfo).Empty)
        End Sub

        Protected Overrides Function FormatSymbol() As String
            If TypeOf Symbol Is ITypeSymbol Then
                Return Symbol.ToDisplayString(s_typeFormat)
            Else
                Return Symbol.ToDisplayString(MemberFormat)
            End If
        End Function
    End Class

End Namespace
