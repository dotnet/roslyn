' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis.Compilers
Imports Microsoft.CodeAnalysis.Compilers.Common
Imports Microsoft.CodeAnalysis.Compilers.VisualBasic
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Services.Editor.VisualBasic.Extensions

    Class SubstituteTypesVisitor(Of TType1 As TypeSymbol, TType2 As TypeSymbol)
        Inherits SymbolVisitor(Of Object, TypeSymbol)

        Private ReadOnly compilation As Compilation
        Private ReadOnly map As IDictionary(Of TType1, TType2)

        Friend Sub New(compilation As Compilation, map As IDictionary(Of TType1, TType2))
            Me.compilation = compilation
            Me.map = map
        End Sub

        Private Function VisitType(symbol As TypeSymbol, argument As Object) As TypeSymbol
            Dim converted As TType2 = Nothing
            If TypeOf symbol Is TType1 AndAlso map.TryGetValue(DirectCast(symbol, TType1), converted) Then
                Return converted
            End If

            Return symbol
        End Function

        Public Overrides Function VisitTypeParameter(symbol As TypeParameterSymbol, argument As Object) As TypeSymbol
            Return VisitType(symbol, argument)
        End Function

        Public Overrides Function VisitErrorType(symbol As ErrorTypeSymbol, argument As Object) As TypeSymbol
            Return VisitType(symbol, argument)
        End Function

        Public Overrides Function VisitNamedType(symbol As NamedTypeSymbol, argument As Object) As TypeSymbol
            If symbol.TypeArguments.Count = 0 Then
                Return symbol
            End If

            Dim substitutedArguments = symbol.TypeArguments.[Select](Function(t) Visit(t)).ToArray()
            Return (DirectCast(symbol.OriginalDefinition, NamedTypeSymbol)).Construct(substitutedArguments)
        End Function

        Public Overrides Function VisitArrayType(symbol As ArrayTypeSymbol, argument As Object) As TypeSymbol
            Return compilation.CreateArrayTypeSymbol(Visit(symbol.ElementType), symbol.Rank)
        End Function
    End Class
End Namespace
