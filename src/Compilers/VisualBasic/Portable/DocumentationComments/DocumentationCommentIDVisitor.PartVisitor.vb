' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class DocumentationCommentIdVisitor
        Private NotInheritable Class PartVisitor
            Inherits VisualBasicSymbolVisitor(Of StringBuilder, Object)

            Public Shared ReadOnly Instance As New PartVisitor(inParameterOrReturnType:=False)

            Private Shared ReadOnly s_parameterOrReturnTypeInstance As New PartVisitor(inParameterOrReturnType:=True)

            Private ReadOnly _inParameterOrReturnType As Boolean

            Private Sub New(inParameterOrReturnType As Boolean)
                _inParameterOrReturnType = inParameterOrReturnType
            End Sub

            Public Overrides Function VisitArrayType(symbol As ArrayTypeSymbol, builder As StringBuilder) As Object
                Visit(symbol.ElementType, builder)

                ' Rank-one arrays are displayed different than rectangular arrays
                If symbol.IsSZArray Then
                    builder.Append("[]")
                Else
                    builder.Append("[0:")
                    For i = 1 To symbol.Rank - 1
                        builder.Append(",0:")
                    Next
                    builder.Append("]"c)
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitEvent(symbol As EventSymbol, builder As StringBuilder) As Object
                Visit(symbol.ContainingType, builder)
                builder.Append("."c)
                builder.Append(symbol.Name)

                Return Nothing
            End Function

            Public Overrides Function VisitField(symbol As FieldSymbol, builder As StringBuilder) As Object
                Visit(symbol.ContainingType, builder)
                builder.Append("."c)
                builder.Append(symbol.Name)

                Return Nothing
            End Function

            Private Sub VisitParameters(parameters As ImmutableArray(Of ParameterSymbol), builder As StringBuilder)
                builder.Append("("c)

                Dim needsComma As Boolean = False
                For Each parameter In parameters
                    If needsComma Then
                        builder.Append(","c)
                    End If
                    Visit(parameter, builder)
                    needsComma = True
                Next

                builder.Append(")"c)
            End Sub

            Public Overrides Function VisitMethod(symbol As MethodSymbol, builder As StringBuilder) As Object
                Visit(symbol.ContainingType, builder)
                builder.Append("."c)
                builder.Append(symbol.MetadataName.Replace("."c, "#"c))

                If symbol.Arity <> 0 Then
                    builder.Append("``")
                    builder.Append(symbol.Arity)
                End If

                If symbol.Parameters.Any() Then
                    s_parameterOrReturnTypeInstance.VisitParameters(symbol.Parameters, builder)
                End If

                If symbol.MethodKind = MethodKind.Conversion Then
                    builder.Append("~"c)
                    s_parameterOrReturnTypeInstance.Visit(symbol.ReturnType, builder)
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitProperty(symbol As PropertySymbol, builder As StringBuilder) As Object
                Visit(symbol.ContainingType, builder)
                builder.Append("."c)
                builder.Append(symbol.MetadataName)

                If symbol.Parameters.Any() Then
                    s_parameterOrReturnTypeInstance.VisitParameters(symbol.Parameters, builder)
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitTypeParameter(symbol As TypeParameterSymbol, builder As StringBuilder) As Object
                Dim ordinalOffset As Integer = 0
                Dim containingSymbol As Symbol = symbol.ContainingSymbol

                If containingSymbol.Kind = SymbolKind.NamedType Then
                    ' If the containing type is nested within other types, then we need to add their arities.
                    ' e.g. A(Of T).B(Of U).M(Of V)(t As T, u As U, v As V) should be M(`0, `1, ``0).
                    Dim curr As NamedTypeSymbol = containingSymbol.ContainingType
                    While curr IsNot Nothing
                        ordinalOffset += curr.Arity
                        curr = curr.ContainingType
                    End While

                    builder.Append("`"c)
                ElseIf containingSymbol.Kind = SymbolKind.Method Then
                    builder.Append("``")
                Else
                    Throw ExceptionUtilities.UnexpectedValue(containingSymbol.Kind)
                End If

                builder.Append(symbol.Ordinal + ordinalOffset)

                Return Nothing
            End Function

            Public Overrides Function VisitNamedType(symbol As NamedTypeSymbol, builder As StringBuilder) As Object
                If symbol.IsTupleType Then
                    Return VisitNamedType(DirectCast(symbol, TupleTypeSymbol).UnderlyingNamedType, builder)
                End If

                If symbol.ContainingSymbol IsNot Nothing AndAlso symbol.ContainingSymbol.Name.Length <> 0 Then
                    Visit(symbol.ContainingSymbol, builder)
                    builder.Append("."c)
                End If

                builder.Append(symbol.Name)

                If symbol.Arity <> 0 Then
                    ' Special case: dev11 treats types instances of the declaring type in the parameter list
                    ' (And return type, for conversions) as constructed with its own type parameters.
                    If Not _inParameterOrReturnType AndAlso TypeSymbol.Equals(symbol, symbol.ConstructedFrom, TypeCompareKind.ConsiderEverything) Then
                        builder.Append(MetadataHelpers.GenericTypeNameManglingChar)
                        builder.Append(symbol.Arity)
                    Else
                        builder.Append("{"c)

                        Dim needsComma As Boolean = False
                        For Each typeArgument In symbol.TypeArgumentsNoUseSiteDiagnostics
                            If needsComma Then
                                builder.Append(","c)
                            End If
                            Visit(typeArgument, builder)
                            needsComma = True
                        Next

                        builder.Append("}"c)
                    End If
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitNamespace(symbol As NamespaceSymbol, builder As StringBuilder) As Object
                If symbol.ContainingNamespace IsNot Nothing AndAlso symbol.ContainingNamespace.Name.Length <> 0 Then
                    Visit(symbol.ContainingNamespace, builder)
                    builder.Append("."c)
                End If

                builder.Append(symbol.Name)

                Return Nothing
            End Function

            Public Overrides Function VisitParameter(symbol As ParameterSymbol, builder As StringBuilder) As Object
                Debug.Assert(_inParameterOrReturnType)

                Visit(symbol.Type, builder)

                If symbol.IsByRef Then
                    builder.Append("@"c)
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitErrorType(symbol As ErrorTypeSymbol, arg As StringBuilder) As Object
                Return VisitNamedType(symbol, arg)
            End Function
        End Class
    End Class
End Namespace
