Imports System.Collections.Generic
Imports System.Linq
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic

Namespace Roslyn.Services.Editor.VisualBasic.Extensions
    Friend Class UnavailableTypeParameterRemover
        Inherits SymbolVisitor(Of Object, TypeSymbol)

        Private ReadOnly compilation As Compilation

        Private ReadOnly availableTypeParameterNames As ISet(Of String)

        Public Sub New(compilation As Compilation, availableTypeParameterNames As ISet(Of String))
            Me.compilation = compilation
            Me.availableTypeParameterNames = availableTypeParameterNames
        End Sub

        Protected Overrides Function VisitErrorType(symbol As ErrorTypeSymbol, arg As Object) As TypeSymbol
            Return VisitNamedType(symbol, arg)
        End Function

        Protected Overrides Function VisitArrayType(symbol As ArrayTypeSymbol, argument As Object) As TypeSymbol
            Dim elementType = Visit(symbol.ElementType)
            If elementType = symbol.ElementType Then
                Return symbol
            End If

            Return compilation.CreateArrayTypeSymbol(elementType, symbol.Rank)
        End Function

        Protected Overrides Function VisitNamedType(symbol As NamedTypeSymbol, argument As Object) As TypeSymbol
            Dim arguments = symbol.TypeArguments.[Select](Function(t) Visit(t)).ToArray()
            If arguments.SequenceEqual(symbol.TypeArguments.AsEnumerable()) Then
                Return symbol
            End If

            Return symbol.ConstructedFrom.Construct(arguments.ToArray())
        End Function

        Protected Overrides Function VisitTypeParameter(symbol As TypeParameterSymbol, argument As Object) As TypeSymbol
            If availableTypeParameterNames.Contains(symbol.Name) Then
                Return symbol
            End If

            Return compilation.ObjectType
        End Function
    End Class
End Namespace