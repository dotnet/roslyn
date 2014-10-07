Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module SymbolInfoExtensions
        <Extension()>
        Public Function GetAllSymbols(info As SymbolInfo) As IEnumerable(Of Symbol)
            Return GetAllSymbolsWorker(info).Distinct()
        End Function

        <Extension()>
        Private Function GetAllSymbolsWorker(info As SymbolInfo) As IEnumerable(Of Symbol)
            Dim result = New List(Of Symbol)
            If info.Symbol IsNot Nothing Then
                result.Add(info.Symbol)
            End If

            For Each symbol In info.CandidateSymbols
                result.Add(symbol)
            Next

            Return result
        End Function

        <Extension()>
        Public Function GetAnySymbol(info As SymbolInfo) As Symbol
            Return info.GetAllSymbols().FirstOrDefault()
        End Function

        <Extension()>
        Public Function GetAnySymbol(info As SymbolInfo, ParamArray allowableReasons As CandidateReason()) As Symbol
            If info.Symbol IsNot Nothing Then
                Return info.Symbol
            End If

            If allowableReasons.Contains(info.CandidateReason) Then
                Return info.CandidateSymbols.FirstOrDefault()
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetBestOrAllSymbols(info As SymbolInfo) As IEnumerable(Of Symbol)
            If info.Symbol IsNot Nothing Then
                Return SpecializedCollections.SingletonEnumerable(info.Symbol)
            ElseIf info.CandidateSymbols.Length > 0 Then
                Return info.CandidateSymbols
            End If

            Return SpecializedCollections.EmptyEnumerable(Of Symbol)()
        End Function
    End Module
End Namespace