' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Class TypeUnification
        ''' <summary>
        ''' Determine whether there is any substitution of type parameters that will
        ''' make two types identical.
        ''' </summary>
        Public Shared Function CanUnify(containingGenericType As NamedTypeSymbol, t1 As TypeSymbol, t2 As TypeSymbol) As Boolean
            If Not containingGenericType.IsGenericType Then
                Return False
            End If

            Dim substitution As TypeSubstitution = Nothing
            Dim result As Boolean = CanUnifyHelper(containingGenericType, t1, t2, substitution)
#If DEBUG Then
            Debug.Assert(Not result OrElse
                SubstituteAllTypeParameters(substitution, t1) = SubstituteAllTypeParameters(substitution, t2))
#End If
            Return result
        End Function

#If DEBUG Then
        Private Shared Function SubstituteAllTypeParameters(substitution As TypeSubstitution, type As TypeSymbol) As TypeSymbol
            If substitution IsNot Nothing Then
                Dim previous As TypeSymbol
                Do
                    previous = type
                    type = type.InternalSubstituteTypeParameters(substitution)
                Loop While previous <> type
            End If
            Return type
        End Function
#End If

        ''' <summary>
        ''' Determine whether there is any substitution of type parameters that will
        ''' make two types identical.
        ''' </summary>
        ''' <param name="containingGenericType">The generic containing type.</param>
        ''' <param name="t1">LHS</param>
        ''' <param name="t2">RHS</param>
        ''' <param name="substitution">
        ''' Substitutions performed so far (or null for none).
        ''' Keys are type parameters, values are types (possibly type parameters).
        ''' Will be updated with new substitutions by the callee.
        ''' Irrelevant if false is returned.
        ''' </param>
        ''' <returns>True if there exists a type map such that Map(LHS) == Map(RHS).</returns>
        ''' <remarks>
        ''' Derived from C# Dev10's BSYMMGR::UnifyTypes.
        ''' Two types will not unify if they have different custom modifiers.
        ''' </remarks>
        Private Shared Function CanUnifyHelper(containingGenericType As NamedTypeSymbol, t1 As TypeSymbol, t2 As TypeSymbol, ByRef substitution As TypeSubstitution) As Boolean
            If t1 Is t2 Then
                Return True
            ElseIf t1 Is Nothing OrElse t2 Is Nothing Then
                Return False
            End If

            If substitution IsNot Nothing Then
                t1 = t1.InternalSubstituteTypeParameters(substitution)
                t2 = t2.InternalSubstituteTypeParameters(substitution)
            End If

            If t1 Is t2 Then
                Return True
            End If

            If Not t1.IsTypeParameter() AndAlso t2.IsTypeParameter() Then
                Dim tmp As TypeSymbol = t1
                t1 = t2
                t2 = tmp
            End If

            Debug.Assert(t1.IsTypeParameter() OrElse Not t2.IsTypeParameter())
            Select Case t1.Kind
                Case SymbolKind.ArrayType
                    If t2.TypeKind <> t1.TypeKind Then
                        Return False
                    End If

                    Dim at1 As ArrayTypeSymbol = DirectCast(t1, ArrayTypeSymbol)
                    Dim at2 As ArrayTypeSymbol = DirectCast(t2, ArrayTypeSymbol)
                    If at1.Rank <> at2.Rank OrElse Not at1.CustomModifiers.SequenceEqual(at2.CustomModifiers) Then
                        Return False
                    End If

                    Return CanUnifyHelper(containingGenericType, at1.ElementType, at2.ElementType, substitution)

                Case SymbolKind.NamedType, SymbolKind.ErrorType
                    If t2.TypeKind <> t1.TypeKind Then
                        Return False
                    End If

                    Dim nt1 As NamedTypeSymbol = DirectCast(t1, NamedTypeSymbol)
                    Dim nt2 As NamedTypeSymbol = DirectCast(t2, NamedTypeSymbol)
                    If Not nt1.IsGenericType Then
                        Return Not nt2.IsGenericType AndAlso nt1 = nt2
                    ElseIf Not nt2.IsGenericType Then
                        Return False
                    End If

                    Dim arity As Integer = nt1.Arity
                    If nt2.Arity <> arity OrElse nt2.OriginalDefinition <> nt1.OriginalDefinition Then
                        Return False
                    End If

                    For i As Integer = 0 To arity - 1
                        If Not CanUnifyHelper(containingGenericType, nt1.TypeArgumentsNoUseSiteDiagnostics(i), nt2.TypeArgumentsNoUseSiteDiagnostics(i), substitution) Then
                            Return False
                        End If
                    Next

                    Return nt1.ContainingType Is Nothing OrElse CanUnifyHelper(containingGenericType, nt1.ContainingType, nt2.ContainingType, substitution)

                Case SymbolKind.TypeParameter
                    If t2.SpecialType = SpecialType.System_Void Then
                        Return False
                    End If

                    Dim tp1 As TypeParameterSymbol = DirectCast(t1, TypeParameterSymbol)
                    If Contains(t2, tp1) Then
                        Return False
                    End If

                    AddSubstitution(substitution, containingGenericType, tp1, t2)
                    Return True

                Case Else
                    Return t1 = t2
            End Select
        End Function

        ''' <summary>
        ''' Add a type parameter -> type argument substitution to a TypeSubstitution object, returning a new TypeSubstitution object
        ''' ByRef.
        ''' </summary>
        Private Shared Sub AddSubstitution(ByRef substitution As TypeSubstitution, targetGenericType As NamedTypeSymbol, tp As TypeParameterSymbol, typeArgument As TypeSymbol)
            If substitution IsNot Nothing Then
                Dim substitutionPairs = ArrayBuilder(Of KeyValuePair(Of TypeParameterSymbol, TypeSymbol)).GetInstance()
                substitutionPairs.AddRange(substitution.PairsIncludingParent)

                ' Insert the new pair at the right point, because TypeSubstitution.Create requires that.
                For i As Integer = 0 To substitutionPairs.Count   ' intentionally going 1 past end of current range
                    If i > substitutionPairs.Count - 1 OrElse substitutionPairs(i).Key.ContainingType.IsSameOrNestedWithin(tp.ContainingType) Then
                        substitutionPairs.Insert(i, New KeyValuePair(Of TypeParameterSymbol, TypeSymbol)(tp, typeArgument))
                        Exit For
                    End If
                Next

                Dim count = substitutionPairs.Count

                Dim typeParameters(0 To count - 1) As TypeParameterSymbol
                Dim typeArguments(0 To count - 1) As TypeSymbol

                For i As Integer = 0 To count - 1
                    typeParameters(i) = substitutionPairs(i).Key
                    typeArguments(i) = substitutionPairs(i).Value
                Next

                substitutionPairs.Free()
                substitution = TypeSubstitution.Create(targetGenericType, typeParameters, typeArguments)
            Else
                substitution = TypeSubstitution.Create(targetGenericType, {tp}, {typeArgument})
            End If
        End Sub




        ''' <summary>
        ''' Return true if the given type contains the specified type parameter.
        ''' </summary>
        Private Shared Function Contains(type As TypeSymbol, typeParam As TypeParameterSymbol) As Boolean
            Select Case type.Kind
                Case SymbolKind.ArrayType
                    Return Contains((DirectCast(type, ArrayTypeSymbol)).ElementType, typeParam)
                Case SymbolKind.NamedType, SymbolKind.ErrorType
                    Dim namedType As NamedTypeSymbol = DirectCast(type, NamedTypeSymbol)
                    While namedType IsNot Nothing
                        For Each typeArg In namedType.TypeArgumentsNoUseSiteDiagnostics
                            If Contains(typeArg, typeParam) Then
                                Return True
                            End If
                        Next

                        namedType = namedType.ContainingType
                    End While

                    Return False
                Case SymbolKind.TypeParameter
                    Return type = typeParam
                Case Else
                    Return False
            End Select
        End Function
    End Class
End Namespace

