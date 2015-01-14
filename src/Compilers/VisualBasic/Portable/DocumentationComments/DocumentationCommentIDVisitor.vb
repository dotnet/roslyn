' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class DocumentationCommentIdVisitor
        Inherits VisualBasicSymbolVisitor(Of StringBuilder, Object)

        Public Shared ReadOnly Instance As New DocumentationCommentIdVisitor()

        Private Sub New()
        End Sub

        Public Overrides Function DefaultVisit(symbol As Symbol, builder As StringBuilder) As Object
            Return Nothing
        End Function

        Public Overrides Function VisitNamespace(symbol As NamespaceSymbol, builder As StringBuilder) As Object
            Return VisitSymbolUsingPrefix(symbol, builder, "N:")
        End Function

        Public Overrides Function VisitEvent(symbol As EventSymbol, builder As StringBuilder) As Object
            Return VisitSymbolUsingPrefix(symbol, builder, "E:")
        End Function

        Public Overrides Function VisitMethod(symbol As MethodSymbol, builder As StringBuilder) As Object
            Return VisitSymbolUsingPrefix(symbol, builder, "M:")
        End Function

        Public Overrides Function VisitField(symbol As FieldSymbol, builder As StringBuilder) As Object
            Return VisitSymbolUsingPrefix(symbol, builder, "F:")
        End Function

        Public Overrides Function VisitProperty(symbol As PropertySymbol, builder As StringBuilder) As Object
            Return VisitSymbolUsingPrefix(symbol, builder, "P:")
        End Function

        Public Overrides Function VisitNamedType(symbol As NamedTypeSymbol, builder As StringBuilder) As Object
            Return VisitSymbolUsingPrefix(symbol, builder, "T:")
        End Function

        Public Overrides Function VisitArrayType(symbol As ArrayTypeSymbol, builder As StringBuilder) As Object
            Return VisitSymbolUsingPrefix(symbol, builder, "T:")
        End Function

        Public Overrides Function VisitTypeParameter(symbol As TypeParameterSymbol, builder As StringBuilder) As Object
            Return VisitSymbolUsingPrefix(symbol, builder, "!:")
        End Function

        Public Overrides Function VisitErrorType(symbol As ErrorTypeSymbol, builder As StringBuilder) As Object
            Return VisitSymbolUsingPrefix(symbol, builder, "!:")
        End Function

        Private Shared Function VisitSymbolUsingPrefix(symbol As Symbol, builder As StringBuilder, prefix As String) As Object
            builder.Append(prefix)
            PartVisitor.Instance.Visit(symbol, builder)
            Return Nothing
        End Function

    End Class
End Namespace
