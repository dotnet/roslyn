' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis

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

        Private ReadOnly _locations As ImmutableArray(Of Location)
        Private ReadOnly _useSiteDiagnosticInfo As DiagnosticInfo
        Private ReadOnly _correspondingDefaultField As TupleErrorFieldSymbol

        ' default tuple elements like Item1 Or Item20 could be provided by the user or
        ' otherwise implicitly declared by compiler
        Private ReadOnly _isImplicitlyDeclared As Boolean

        Public Sub New(container As NamedTypeSymbol,
                       name As String,
                       tupleElementIndex As Integer,
                       location As Location,
                       type As TypeSymbol,
                       useSiteDiagnosticInfo As DiagnosticInfo,
                       isImplicitlyDeclared As Boolean,
                       correspondingDefaultFieldOpt As TupleErrorFieldSymbol)

            MyBase.New(container, container, type, name, Accessibility.Public)

            Debug.Assert(name <> Nothing)
            Me._locations = If((location Is Nothing), ImmutableArray(Of Location).Empty, ImmutableArray.Create(Of Location)(location))
            Me._useSiteDiagnosticInfo = useSiteDiagnosticInfo
            Me._tupleElementIndex = If(correspondingDefaultFieldOpt Is Nothing, tupleElementIndex << 1, (tupleElementIndex << 1) + 1)
            Me._isImplicitlyDeclared = isImplicitlyDeclared

            Debug.Assert(correspondingDefaultFieldOpt Is Nothing = Me.IsDefaultTupleElement)
            Debug.Assert(correspondingDefaultFieldOpt Is Nothing OrElse correspondingDefaultFieldOpt.IsDefaultTupleElement)

            _correspondingDefaultField = If(correspondingDefaultFieldOpt, Me)
        End Sub

        Public Overrides ReadOnly Property IsTupleField As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' If this is a field representing a tuple element,
        ''' returns the index of the element (zero-based).
        ''' Otherwise returns -1
        ''' </summary>
        Public Overrides ReadOnly Property TupleElementIndex As Integer
            Get
                If _tupleElementIndex < 0 Then
                    Return -1
                End If

                Return _tupleElementIndex >> 1
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefaultTupleElement As Boolean
            Get
                ' not negative and even
                Return (_tupleElementIndex And ((1 << 31) Or 1)) = 0
            End Get
        End Property

        Public Overrides ReadOnly Property TupleUnderlyingField As FieldSymbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me._locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return If(_isImplicitlyDeclared,
                    ImmutableArray(Of SyntaxReference).Empty,
                    GetDeclaringSyntaxReferenceHelper(Of VisualBasicSyntaxNode)(_locations))
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _isImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property CorrespondingTupleField As FieldSymbol
            Get
                Return _correspondingDefaultField
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return Me._type
            End Get
        End Property

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Return Me._useSiteDiagnosticInfo
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Me.ContainingType.GetHashCode(), Me._tupleElementIndex.GetHashCode())
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, TupleErrorFieldSymbol))
        End Function

        Public Overloads Function Equals(other As TupleErrorFieldSymbol) As Boolean
            Return other Is Me OrElse
                (other IsNot Nothing AndAlso Me._tupleElementIndex = other._tupleElementIndex AndAlso TypeSymbol.Equals(Me.ContainingType, other.ContainingType, TypeCompareKind.ConsiderEverything))
        End Function
    End Class
End Namespace
