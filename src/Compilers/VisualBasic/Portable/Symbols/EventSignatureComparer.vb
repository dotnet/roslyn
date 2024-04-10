' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Implementation of IEqualityComparer for EventSymbol, with options for various aspects
    ''' to compare.
    ''' </summary>
    Friend Class EventSignatureComparer
        Implements IEqualityComparer(Of EventSymbol)

        ''' <summary>
        ''' This instance is used when trying to determine which implemented interface event is implemented
        ''' by a event with an Implements clause, according to VB rules.
        ''' This comparer uses event signature that may come from As clause delegate or from a parameter list.
        ''' The event signatures are compared without regard to name (including the interface part, if any)
        ''' and the return type must match. (NOTE: that return type of implementing event is always Void)
        ''' </summary>
        Public Shared ReadOnly ExplicitEventImplementationComparer As EventSignatureComparer =
            New EventSignatureComparer(considerName:=False,
                                        considerType:=False,
                                        considerCustomModifiers:=False,
                                        considerTupleNames:=False,
                                        considerIsShared:=True)

        Public Shared ReadOnly ExplicitEventImplementationWithTupleNamesComparer As EventSignatureComparer =
            New EventSignatureComparer(considerName:=False,
                                        considerType:=False,
                                        considerCustomModifiers:=False,
                                        considerTupleNames:=True,
                                        considerIsShared:=True)

        ''' <summary>
        ''' This instance is used to check whether one event overrides another, according to the VB definition.
        ''' </summary>
        Public Shared ReadOnly OverrideSignatureComparer As EventSignatureComparer =
            New EventSignatureComparer(considerName:=True,
                                        considerType:=False,
                                        considerCustomModifiers:=False,
                                        considerTupleNames:=False,
                                        considerIsShared:=False)

        ''' <summary>
        ''' This instance is intended to reflect the definition of signature equality used by the runtime (ECMA 335 Section 8.6.1.6).
        ''' It considers type, name, parameters, and custom modifiers.
        ''' </summary>
        Public Shared ReadOnly RuntimeEventSignatureComparer As EventSignatureComparer =
            New EventSignatureComparer(considerName:=True,
                                        considerType:=True,
                                        considerCustomModifiers:=True,
                                        considerTupleNames:=False,
                                        considerIsShared:=False)

        ''' <summary>
        ''' This instance is used to compare potential WinRT fake events in type projection.
        ''' 
        ''' FIXME(angocke): This is almost certainly wrong. The semantics of WinRT conflict 
        ''' comparison should probably match overload resolution (i.e., we should not add a member
        '''  to lookup that would result in ambiguity), but this is closer to what Dev12 does.
        ''' 
        ''' The real fix here is to establish a spec for how WinRT conflict comparison should be
        ''' performed. Once this is done we should remove these comments.
        ''' </summary>
        Public Shared ReadOnly WinRTConflictComparer As EventSignatureComparer =
            New EventSignatureComparer(considerName:=True,
                                       considerType:=False,
                                       considerCustomModifiers:=False,
                                       considerTupleNames:=False,
                                       considerIsShared:=False)

        ' Compare the event name (no explicit part)
        Private ReadOnly _considerName As Boolean

        ' Compare the event type
        Private ReadOnly _considerType As Boolean

        ' Consider custom modifiers on/in parameters and return types (if return is considered).
        Private ReadOnly _considerCustomModifiers As Boolean

        ' Consider tuple names in parameters and return types (if return is considered).
        Private ReadOnly _considerTupleNames As Boolean

        Private ReadOnly _considerIsShared As Boolean

        Private Sub New(considerName As Boolean,
                        considerType As Boolean,
                        considerCustomModifiers As Boolean,
                        considerTupleNames As Boolean,
                        considerIsShared As Boolean)

            Me._considerName = considerName
            Me._considerType = considerType
            Me._considerCustomModifiers = considerCustomModifiers
            Me._considerTupleNames = considerTupleNames
            Me._considerIsShared = considerIsShared
        End Sub

#Region "IEqualityComparer(Of EventSymbol) Members"

        Public Overloads Function Equals(event1 As EventSymbol, event2 As EventSymbol) As Boolean _
            Implements IEqualityComparer(Of EventSymbol).Equals

            If event1 = event2 Then
                Return True
            End If

            If event1 Is Nothing OrElse event2 Is Nothing Then
                Return False
            End If

            If _considerIsShared AndAlso event1.IsShared <> event2.IsShared Then
                Return False
            End If

            If _considerName AndAlso Not IdentifierComparison.Equals(event1.Name, event2.Name) Then
                Return False
            End If

            If _considerType Then
                Dim comparison As TypeCompareKind = MethodSignatureComparer.MakeTypeCompareKind(_considerCustomModifiers, _considerTupleNames)
                If Not event1.Type.IsSameType(event2.Type, comparison) Then
                    Return False
                End If
            End If

            If event1.DelegateParameters.Length > 0 OrElse event2.DelegateParameters.Length > 0 Then
                If Not MethodSignatureComparer.HaveSameParameterTypes(event1.DelegateParameters,
                                                                      Nothing,
                                                                      event2.DelegateParameters,
                                                                      Nothing,
                                                                      considerByRef:=True,
                                                                      considerCustomModifiers:=_considerCustomModifiers,
                                                                      considerTupleNames:=_considerTupleNames) Then
                    Return False
                End If
            End If

            Return True
        End Function

        Public Overloads Function GetHashCode([event] As EventSymbol) As Integer _
            Implements IEqualityComparer(Of EventSymbol).GetHashCode

            Dim _hash As Integer = 1
            If [event] IsNot Nothing Then
                If _considerName Then
                    _hash = Hash.Combine([event].Name, _hash)
                End If

                If _considerType AndAlso Not _considerCustomModifiers Then
                    _hash = Hash.Combine([event].Type, _hash)
                End If

                _hash = Hash.Combine(_hash, [event].DelegateParameters.Length)
            End If

            Return _hash
        End Function
#End Region

    End Class

End Namespace

