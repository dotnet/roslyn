' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a field of a tuple type (such as (int, byte).Item1)
    ''' that doesn't have a corresponding backing field within the tuple underlying type.
    ''' Created in response to an error condition.
    ''' </summary>
    Friend NotInheritable Class TupleErrorFieldSymbol
        Inherits SynthesizedFieldSymbol

        ''' <summary>
        ''' If this field represents a tuple element with index X, the field contains
        '''  2X      if this field represents a Default-named element
        '''  2X + 1  if this field represents a Friendly-named element
        ''' Otherwise, (-1 - [index in members array]);
        ''' </summary>
        Private ReadOnly _tupleElementIndex As Integer

        Private ReadOnly _useSiteDiagnosticInfo As DiagnosticInfo

        ' default tuple elements like Item1 Or Item20 could be provided by the user or
        ' otherwise implicitly declared by compiler
        'Private ReadOnly _isImplicitlyDeclared As Boolean

        Public Sub New(
                        container As NamedTypeSymbol,
                        name As String,
                        tupleElementIndex As Integer,
                        location As Location,
                        type As TypeSymbol,
                        useSiteDiagnosticInfo As DiagnosticInfo,
                        isImplicitlyDeclared As Boolean,
                        correspondingDefaultFieldOpt As TupleErrorFieldSymbol
                      )

            MyBase.New(container, container, type, name, Accessibility.Public)

            Debug.Assert(name <> Nothing)
            Locations = If((location Is Nothing), ImmutableArray(Of Location).Empty, ImmutableArray.Create(Of Location)(location))
            _useSiteDiagnosticInfo = useSiteDiagnosticInfo
            _tupleElementIndex = If(correspondingDefaultFieldOpt Is Nothing, tupleElementIndex << 1, (tupleElementIndex << 1) + 1)
            Me.IsImplicitlyDeclared = isImplicitlyDeclared

            Debug.Assert(correspondingDefaultFieldOpt Is Nothing = IsDefaultTupleElement)
            Debug.Assert(correspondingDefaultFieldOpt Is Nothing OrElse correspondingDefaultFieldOpt.IsDefaultTupleElement)

            CorrespondingTupleField = If(correspondingDefaultFieldOpt, Me)
        End Sub

        Public Overrides ReadOnly Property IsTupleField As Boolean = True

        ''' <summary>
        ''' If this is a field representing a tuple element,
        ''' returns the index of the element (zero-based).
        ''' Otherwise returns -1
        ''' </summary>
        Public Overrides ReadOnly Property TupleElementIndex As Integer
            Get
                Return If(_tupleElementIndex < 0, -1, _tupleElementIndex >> 1)
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefaultTupleElement As Boolean
            Get
                ' not negative and even
                Return (_tupleElementIndex And ((1 << 31) Or 1)) = 0
            End Get
        End Property

        Public Overrides ReadOnly Property TupleUnderlyingField As FieldSymbol = Nothing

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return If(_IsImplicitlyDeclared, ImmutableArray(Of SyntaxReference).Empty,
                                                 GetDeclaringSyntaxReferenceHelper(Of VisualBasicSyntaxNode)(_Locations))
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean

        Public Overrides ReadOnly Property CorrespondingTupleField As FieldSymbol

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Return _useSiteDiagnosticInfo
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(ContainingType.GetHashCode(), _tupleElementIndex.GetHashCode())
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, TupleErrorFieldSymbol))
        End Function

        Public Overloads Function Equals(other As TupleErrorFieldSymbol) As Boolean
            Return (other Is Me) OrElse (other IsNot Nothing AndAlso
                                         _tupleElementIndex = other._tupleElementIndex AndAlso
                                         ContainingType = other.ContainingType)
        End Function

    End Class

End Namespace
