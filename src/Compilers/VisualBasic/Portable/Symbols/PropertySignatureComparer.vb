' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Implementation of IEqualityComparer for PropertySymbols, with options for various aspects
    ''' to compare.
    ''' </summary>
    Friend NotInheritable Class PropertySignatureComparer
        Implements IEqualityComparer(Of PropertySymbol)

        ''' <summary>
        ''' This instance is used to compare all aspects.
        ''' </summary>
        Public Shared ReadOnly AllAspectsSignatureComparer As PropertySignatureComparer =
            New PropertySignatureComparer(considerName:=True,
                                          considerType:=True,
                                          considerReadWriteModifiers:=True,
                                          considerOptionalParameters:=True,
                                          considerCustomModifiers:=True)

        ''' <summary>
        ''' This instance is intended to reflect the definition of signature equality used by the runtime (ECMA 335 Section 8.6.1.6).
        ''' It considers type, name, parameters, and custom modifiers.
        ''' </summary>
        Public Shared ReadOnly RuntimePropertySignatureComparer As PropertySignatureComparer =
            New PropertySignatureComparer(considerName:=True,
                                          considerType:=True,
                                          considerReadWriteModifiers:=False,
                                          considerOptionalParameters:=True,
                                          considerCustomModifiers:=True)

        ''' <summary>
        ''' This instance is used to search for properties that have identical signatures in every regard.
        ''' </summary>
        Public Shared ReadOnly RetargetedExplicitPropertyImplementationComparer As PropertySignatureComparer =
            New PropertySignatureComparer(considerName:=True,
                                          considerType:=True,
                                          considerReadWriteModifiers:=True,
                                          considerOptionalParameters:=True,
                                          considerCustomModifiers:=True)

        ''' <summary>
        ''' This instance is used to compare potential WinRT fake properties in type projection.
        ''' 
        ''' FIXME(angocke): This is almost certainly wrong. The semantics of WinRT conflict 
        ''' comparison should probably match overload resolution (i.e., we should not add a member
        '''  to lookup that would result in ambiguity), but this is closer to what Dev12 does.
        ''' 
        ''' The real fix here is to establish a spec for how WinRT conflict comparison should be
        ''' performed. Once this is done we should remove these comments.
        ''' </summary>
        Public Shared ReadOnly WinRTConflictComparer As PropertySignatureComparer =
            New PropertySignatureComparer(considerName:=True,
                                          considerType:=False,
                                          considerReadWriteModifiers:=False,
                                          considerOptionalParameters:=False,
                                          considerCustomModifiers:=False)

        ' Compare the property name (no explicit part)
        Private ReadOnly _considerName As Boolean

        ' Compare the property type
        Private ReadOnly _considerType As Boolean

        ' Consider if property is read-only, read-write, write-only
        Private ReadOnly _considerReadWriteModifiers As Boolean

        ' Consider optional parameters
        Private ReadOnly _considerOptionalParameters As Boolean

        ' Consider custom modifiers on/in parameters and return types (if return is considered).
        Private ReadOnly _considerCustomModifiers As Boolean

        Private Sub New(considerName As Boolean,
                        considerType As Boolean,
                        considerReadWriteModifiers As Boolean,
                        considerOptionalParameters As Boolean,
                        considerCustomModifiers As Boolean)
            Me._considerName = considerName
            Me._considerType = considerType
            Me._considerReadWriteModifiers = considerReadWriteModifiers
            Me._considerOptionalParameters = considerOptionalParameters
            Me._considerCustomModifiers = considerCustomModifiers
        End Sub

#Region "IEqualityComparer(Of PropertySymbol) Members"

        Public Overloads Function Equals(prop1 As PropertySymbol, prop2 As PropertySymbol) As Boolean _
            Implements IEqualityComparer(Of PropertySymbol).Equals

            If prop1 = prop2 Then
                Return True
            End If

            If prop1 Is Nothing OrElse prop2 Is Nothing Then
                Return False
            End If

            If _considerName AndAlso Not IdentifierComparison.Equals(prop1.Name, prop2.Name) Then
                Return False
            End If

            If _considerReadWriteModifiers AndAlso
               ((prop1.IsReadOnly <> prop2.IsReadOnly) OrElse (prop1.IsWriteOnly <> prop2.IsWriteOnly)) Then
                Return False
            End If

            If _considerType Then
                If Not HaveSameTypes(prop1, prop2, _considerCustomModifiers) Then
                    Return False
                End If
            End If

            If prop1.ParameterCount > 0 OrElse prop2.ParameterCount > 0 Then
                If Not MethodSignatureComparer.HaveSameParameterTypes(prop1.Parameters, Nothing, prop2.Parameters, Nothing, False, _considerCustomModifiers) Then
                    Return False
                End If
            End If

            Return True
        End Function

        Public Overloads Function GetHashCode(prop As PropertySymbol) As Integer _
            Implements IEqualityComparer(Of PropertySymbol).GetHashCode

            Dim _hash As Integer = 1
            If prop IsNot Nothing Then
                If _considerName Then
                    _hash = Hash.Combine(prop.Name, _hash)
                End If

                If _considerType AndAlso Not _considerCustomModifiers Then
                    _hash = Hash.Combine(prop.Type, _hash)
                End If

                _hash = Hash.Combine(_hash, prop.ParameterCount)
            End If

            Return _hash
        End Function
#End Region

#Region "Detailed comparison functions"
        Public Shared Function DetailedCompare(
            prop1 As PropertySymbol,
            prop2 As PropertySymbol,
            comparisons As SymbolComparisonResults,
            Optional stopIfAny As SymbolComparisonResults = 0
        ) As SymbolComparisonResults
            Dim results As SymbolComparisonResults = Nothing

            If prop1 = prop2 Then
                Return Nothing
            End If

            If (comparisons And SymbolComparisonResults.PropertyAccessorMismatch) <> 0 Then
                If ((prop1.IsReadOnly <> prop2.IsReadOnly) OrElse (prop1.IsWriteOnly <> prop2.IsWriteOnly)) Then
                    results = results Or SymbolComparisonResults.PropertyAccessorMismatch
                    If (stopIfAny And SymbolComparisonResults.PropertyAccessorMismatch) <> 0 Then
                        GoTo Done
                    End If
                End If
            End If

            If (comparisons And (SymbolComparisonResults.ReturnTypeMismatch Or SymbolComparisonResults.CustomModifierMismatch)) <> 0 Then
                results = results Or MethodSignatureComparer.DetailedReturnTypeCompare(New TypeWithModifiers(prop1.Type, prop1.TypeCustomModifiers), Nothing,
                                                                                       New TypeWithModifiers(prop2.Type, prop2.TypeCustomModifiers), Nothing,
                                                                                       comparisons)
                If (stopIfAny And results) <> 0 Then
                    GoTo Done
                End If
            End If

            If (comparisons And SymbolComparisonResults.AllParameterMismatches) <> 0 Then
                results = results Or MethodSignatureComparer.DetailedParameterCompare(prop1.Parameters, Nothing, prop2.Parameters, Nothing, comparisons, stopIfAny)
                If (stopIfAny And results) <> 0 Then
                    GoTo Done
                End If
            End If

            ' It turns out name comparison is rather expensive relative to the other checks.
            If (comparisons And SymbolComparisonResults.NameMismatch) <> 0 Then
                If Not IdentifierComparison.Equals(prop1.Name, prop2.Name) Then
                    results = results Or SymbolComparisonResults.NameMismatch
                    If (stopIfAny And SymbolComparisonResults.NameMismatch) <> 0 Then
                        GoTo Done
                    End If
                End If
            End If

Done:
            Return results And comparisons
        End Function
#End Region

        Private Shared Function HaveSameTypes(prop1 As PropertySymbol, prop2 As PropertySymbol, considerCustomModifiers As Boolean) As Boolean
            Dim type1 = prop1.Type
            Dim type2 = prop2.Type

            ' the runtime compares custom modifiers using (effectively) SequenceEqual
            Return If(considerCustomModifiers,
                      type1 = type2 AndAlso prop1.TypeCustomModifiers.SequenceEqual(prop2.TypeCustomModifiers),
                      type1.IsSameTypeIgnoringCustomModifiers(type2))
        End Function

    End Class

End Namespace

