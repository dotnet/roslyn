Imports System.Collections.Generic
Imports Roslyn.Compilers.VisualBasic

Namespace Roslyn.Services.Editor.VisualBasic.Extensions

    Class CollectMethodTypeParameterSymbolsVisitor
        Inherits SymbolVisitor(Of IList(Of TypeParameterSymbol), Object)

        Public Shared ReadOnly Instance As SymbolVisitor(Of IList(Of TypeParameterSymbol), Object) = New CollectMethodTypeParameterSymbolsVisitor()

        Private Sub New()
        End Sub

        Public Overrides Function VisitArrayType(symbol As ArrayTypeSymbol, arg As IList(Of TypeParameterSymbol)) As Object
            Return Me.Visit(symbol.ElementType, arg)
        End Function

        Public Overrides Function VisitErrorType(symbol As ErrorTypeSymbol, arg As IList(Of TypeParameterSymbol)) As Object
            For Each child In symbol.TypeArguments
                Visit(child, arg)
            Next

            Return Nothing
        End Function

        Public Overrides Function VisitNamedType(symbol As NamedTypeSymbol, arg As IList(Of TypeParameterSymbol)) As Object
            For Each child In symbol.TypeArguments
                Visit(child, arg)
            Next

            Return Nothing
        End Function

        Public Overrides Function VisitTypeParameter(symbol As TypeParameterSymbol, arg As IList(Of TypeParameterSymbol)) As Object
            If TypeOf symbol.ContainingSymbol Is MethodSymbol Then
                If Not arg.Contains(symbol) Then
                    arg.Add(symbol)
                End If
            End If

            Return Nothing
        End Function
    End Class
End Namespace