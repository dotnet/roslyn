' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.PooledObjects
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

            If TypeSymbol.Equals(t1, t2, TypeCompareKind.ConsiderEverything) Then
                Return True
            End If

            Dim substitution As TypeSubstitution = Nothing
            Dim result As Boolean = CanUnifyHelper(containingGenericType,
                                                   If(t1 Is Nothing, Nothing, New TypeWithModifiers(t1)),
                                                   If(t2 Is Nothing, Nothing, New TypeWithModifiers(t2)),
                                                   substitution)
#If DEBUG Then
            Debug.Assert(Not result OrElse
                SubstituteAllTypeParameters(substitution, New TypeWithModifiers(t1)).IsSameType(SubstituteAllTypeParameters(substitution, New TypeWithModifiers(t2)), TypeCompareKind.IgnoreTupleNames))
#End If
            Return result
        End Function

#If DEBUG Then
        Private Shared Function SubstituteAllTypeParameters(substitution As TypeSubstitution, type As TypeWithModifiers) As TypeWithModifiers
            If substitution IsNot Nothing Then
                Dim previous As TypeWithModifiers
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
        Private Shared Function CanUnifyHelper(containingGenericType As NamedTypeSymbol, t1 As TypeWithModifiers, t2 As TypeWithModifiers, ByRef substitution As TypeSubstitution) As Boolean
            If t1 = t2 Then
                Return True
            ElseIf t1.Type Is Nothing OrElse t2.Type Is Nothing Then
                Return False
            End If

            If substitution IsNot Nothing Then
                t1 = t1.InternalSubstituteTypeParameters(substitution)
                t2 = t2.InternalSubstituteTypeParameters(substitution)
            End If

            If t1 = t2 Then
                Return True
            End If

            If Not t1.Type.IsTypeParameter() AndAlso t2.Type.IsTypeParameter() Then
                Dim tmp As TypeWithModifiers = t1
                t1 = t2
                t2 = tmp
            End If

            Debug.Assert(t1.Type.IsTypeParameter() OrElse Not t2.Type.IsTypeParameter())
            Select Case t1.Type.Kind
                Case SymbolKind.ArrayType
                    If t2.Type.TypeKind <> t1.Type.TypeKind OrElse Not t1.CustomModifiers.SequenceEqual(t2.CustomModifiers) Then
                        Return False
                    End If

                    Dim at1 As ArrayTypeSymbol = DirectCast(t1.Type, ArrayTypeSymbol)
                    Dim at2 As ArrayTypeSymbol = DirectCast(t2.Type, ArrayTypeSymbol)
                    If Not at1.HasSameShapeAs(at2) Then
                        Return False
                    End If

                    Return CanUnifyHelper(containingGenericType, New TypeWithModifiers(at1.ElementType, at1.CustomModifiers), New TypeWithModifiers(at2.ElementType, at2.CustomModifiers), substitution)

                Case SymbolKind.NamedType, SymbolKind.ErrorType
                    If t2.Type.TypeKind <> t1.Type.TypeKind OrElse Not t1.CustomModifiers.SequenceEqual(t2.CustomModifiers) Then
                        Return False
                    End If

                    Dim nt1 As NamedTypeSymbol = DirectCast(t1.Type, NamedTypeSymbol)
                    Dim nt2 As NamedTypeSymbol = DirectCast(t2.Type, NamedTypeSymbol)

                    If nt1.IsTupleType OrElse nt2.IsTupleType Then
                        Return CanUnifyHelper(containingGenericType,
                                              New TypeWithModifiers(nt1.GetTupleUnderlyingTypeOrSelf()),
                                              New TypeWithModifiers(nt2.GetTupleUnderlyingTypeOrSelf()),
                                              substitution)
                    End If

                    If Not nt1.IsGenericType Then
                        Return Not nt2.IsGenericType AndAlso TypeSymbol.Equals(nt1, nt2, TypeCompareKind.ConsiderEverything)
                    ElseIf Not nt2.IsGenericType Then
                        Return False
                    End If

                    Dim arity As Integer = nt1.Arity
                    If nt2.Arity <> arity OrElse Not TypeSymbol.Equals(nt2.OriginalDefinition, nt1.OriginalDefinition, TypeCompareKind.ConsiderEverything) Then
                        Return False
                    End If

                    Dim nt1Arguments = nt1.TypeArgumentsNoUseSiteDiagnostics
                    Dim nt2Arguments = nt2.TypeArgumentsNoUseSiteDiagnostics

                    Dim nt1HasModifiers = nt1.HasTypeArgumentsCustomModifiers
                    Dim nt2HasModifiers = nt2.HasTypeArgumentsCustomModifiers

                    For i As Integer = 0 To arity - 1
                        If Not CanUnifyHelper(containingGenericType,
                                              New TypeWithModifiers(nt1Arguments(i), If(nt1HasModifiers, nt1.GetTypeArgumentCustomModifiers(i), Nothing)),
                                              New TypeWithModifiers(nt2Arguments(i), If(nt2HasModifiers, nt2.GetTypeArgumentCustomModifiers(i), Nothing)),
                                              substitution) Then
                            Return False
                        End If
                    Next

                    'TODO: Calling CanUnifyHelper for the containing type is an overkill, we simply need to go through type arguments for all containers.
                    Return nt1.ContainingType Is Nothing OrElse CanUnifyHelper(containingGenericType, New TypeWithModifiers(nt1.ContainingType), New TypeWithModifiers(nt2.ContainingType), substitution)

                Case SymbolKind.TypeParameter
                    If t2.Type.SpecialType = SpecialType.System_Void Then
                        Return False
                    End If

                    Dim tp1 As TypeParameterSymbol = DirectCast(t1.Type, TypeParameterSymbol)
                    If Contains(t2.Type, tp1) Then
                        Return False
                    End If

                    If t1.CustomModifiers.IsDefaultOrEmpty Then
                        AddSubstitution(substitution, containingGenericType, tp1, t2)
                        Return True
                    End If

                    If t1.CustomModifiers.SequenceEqual(t2.CustomModifiers) Then
                        AddSubstitution(substitution, containingGenericType, tp1, New TypeWithModifiers(t2.Type))
                        Return True
                    End If

                    If t1.CustomModifiers.Length < t2.CustomModifiers.Length AndAlso
                       t1.CustomModifiers.SequenceEqual(t2.CustomModifiers.Take(t1.CustomModifiers.Length)) Then
                        AddSubstitution(substitution, containingGenericType, tp1,
                                        New TypeWithModifiers(t2.Type,
                                                              ImmutableArray.Create(t2.CustomModifiers, t1.CustomModifiers.Length, t2.CustomModifiers.Length - t1.CustomModifiers.Length)))
                        Return True
                    End If

                    If t2.Type.IsTypeParameter Then
                        Dim tp2 As TypeParameterSymbol = DirectCast(t2.Type, TypeParameterSymbol)

                        If t2.CustomModifiers.IsDefaultOrEmpty Then
                            AddSubstitution(substitution, containingGenericType, tp2, t1)
                            Return True
                        End If

                        If t2.CustomModifiers.Length < t1.CustomModifiers.Length AndAlso
                           t2.CustomModifiers.SequenceEqual(t1.CustomModifiers.Take(t2.CustomModifiers.Length)) Then
                            AddSubstitution(substitution, containingGenericType, tp2,
                                            New TypeWithModifiers(t1.Type,
                                                                  ImmutableArray.Create(t1.CustomModifiers, t2.CustomModifiers.Length, t1.CustomModifiers.Length - t2.CustomModifiers.Length)))
                            Return True
                        End If
                    End If

                    Return False

                Case Else
                    Return t1 = t2
            End Select
        End Function

        ''' <summary>
        ''' Add a type parameter -> type argument substitution to a TypeSubstitution object, returning a new TypeSubstitution object
        ''' ByRef.
        ''' </summary>
        Private Shared Sub AddSubstitution(ByRef substitution As TypeSubstitution, targetGenericType As NamedTypeSymbol, tp As TypeParameterSymbol, typeArgument As TypeWithModifiers)
            If substitution IsNot Nothing Then
                Dim substitutionPairs = ArrayBuilder(Of KeyValuePair(Of TypeParameterSymbol, TypeWithModifiers)).GetInstance()
                substitutionPairs.AddRange(substitution.PairsIncludingParent)

                ' Insert the new pair at the right point, because TypeSubstitution.Create requires that.
                For i As Integer = 0 To substitutionPairs.Count   ' intentionally going 1 past end of current range
                    If i > substitutionPairs.Count - 1 OrElse substitutionPairs(i).Key.ContainingType.IsSameOrNestedWithin(tp.ContainingType) Then
                        substitutionPairs.Insert(i, New KeyValuePair(Of TypeParameterSymbol, TypeWithModifiers)(tp, typeArgument))
                        Exit For
                    End If
                Next

                Dim count = substitutionPairs.Count

                Dim typeParameters(0 To count - 1) As TypeParameterSymbol
                Dim typeArguments(0 To count - 1) As TypeWithModifiers

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
                        Dim typeParts = If(namedType.IsTupleType, namedType.TupleElementTypes, namedType.TypeArgumentsNoUseSiteDiagnostics)
                        For Each typePart In typeParts
                            If Contains(typePart, typeParam) Then
                                Return True
                            End If
                        Next

                        namedType = namedType.ContainingType
                    End While

                    Return False
                Case SymbolKind.TypeParameter
                    Return TypeSymbol.Equals(type, typeParam, TypeCompareKind.ConsiderEverything)
                Case Else
                    Return False
            End Select
        End Function
    End Class
End Namespace

