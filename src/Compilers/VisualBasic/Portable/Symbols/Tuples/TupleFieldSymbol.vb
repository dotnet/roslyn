' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a non-element field of a tuple type (such as (int, byte).Rest)
    ''' that is backed by a real field within the tuple underlying type.
    ''' </summary>
    Friend Class TupleFieldSymbol
        Inherits WrappedFieldSymbol

        Protected _containingTuple As TupleTypeSymbol

        ''' <summary>
        ''' If this field represents a tuple element with index X, the field contains
        '''  2X      if this field represents a Default-named element
        '''  2X + 1  if this field represents a Friendly-named element
        ''' Otherwise, (-1 - [index in members array]);
        ''' </summary>
        Private _tupleElementIndex As Integer

        Public Overrides ReadOnly Property IsTupleField As Boolean = True

        Public Overrides ReadOnly Property TupleUnderlyingField As FieldSymbol
            Get
                Return UnderlyingField
            End Get
        End Property

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

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return _containingTuple.GetTupleMemberSymbolForUnderlyingMember(Of Symbol)(UnderlyingField.AssociatedSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingTuple
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return UnderlyingField.CustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return UnderlyingField.Type
            End Get
        End Property

        Public Sub New(container As TupleTypeSymbol, underlyingField As FieldSymbol, tupleElementIndex As Integer)
            MyBase.New(underlyingField)

            Debug.Assert(container.UnderlyingNamedType.IsSameTypeIgnoringAll(underlyingField.ContainingType) OrElse TypeOf Me Is TupleVirtualElementFieldSymbol,
                                            "virtual fields should be represented by " & NameOf(TupleVirtualElementFieldSymbol))
            _containingTuple = container
            _tupleElementIndex = tupleElementIndex
        End Sub

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return UnderlyingField.GetAttributes()
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Dim useSiteDiagnostic As DiagnosticInfo = MyBase.GetUseSiteErrorInfo
            MyBase.MergeUseSiteErrorInfo(useSiteDiagnostic, UnderlyingField.GetUseSiteErrorInfo())
            Return useSiteDiagnostic
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(_containingTuple.GetHashCode(), _tupleElementIndex.GetHashCode())
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, TupleFieldSymbol))
        End Function

        Public Overloads Function Equals(other As TupleFieldSymbol) As Boolean
            Return (other IsNot Nothing) AndAlso (_tupleElementIndex = other._tupleElementIndex AndAlso
                                                  _containingTuple = other._containingTuple)
        End Function
    End Class

    ''' <summary>
    ''' Represents an element field of a tuple type (such as (int, byte).Item1)
    ''' that is backed by a real field with the same name within the tuple underlying type.
    ''' </summary>
    Friend Class TupleElementFieldSymbol
        Inherits TupleFieldSymbol

        ' default tuple elements like Item1 Or Item20 could be provided by the user or
        ' otherwise implicitly declared by compiler

        Public Sub New(
                        container As TupleTypeSymbol,
                        underlyingField As FieldSymbol,
                        tupleElementIndex As Integer,
                        location As Location,
                        isImplicitlyDeclared As Boolean,
                        correspondingDefaultFieldOpt As TupleElementFieldSymbol
                      )

            MyBase.New(container, underlyingField, If(correspondingDefaultFieldOpt Is Nothing, tupleElementIndex << 1, (tupleElementIndex << 1) + 1))

            Locations = If((location Is Nothing), ImmutableArray(Of Location).Empty, ImmutableArray.Create(Of Location)(location))
            Me.IsImplicitlyDeclared = isImplicitlyDeclared

            Debug.Assert(correspondingDefaultFieldOpt Is Nothing = IsDefaultTupleElement)
            Debug.Assert(correspondingDefaultFieldOpt Is Nothing OrElse correspondingDefaultFieldOpt.IsDefaultTupleElement)

            Me.CorrespondingTupleField = If(correspondingDefaultFieldOpt, Me)
        End Sub

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return If(IsImplicitlyDeclared, ImmutableArray(Of SyntaxReference).Empty,
                                                 GetDeclaringSyntaxReferenceHelper(Of VisualBasicSyntaxNode)(_Locations))
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Dim flag As Boolean = UnderlyingField.ContainingType IsNot _containingTuple.TupleUnderlyingType
                If flag Then Return Nothing
                Return MyBase.TypeLayoutOffset
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Dim flag As Boolean = UnderlyingField.ContainingType IsNot _containingTuple.TupleUnderlyingType
                If flag Then Return Nothing
                Return MyBase.AssociatedSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property CorrespondingTupleField As FieldSymbol

    End Class

    ''' <summary>
    ''' Represents an element field of a tuple type (such as (int a, byte b).a, or (int a, byte b).b)
    ''' that is backed by a real field with a different name within the tuple underlying type.
    ''' </summary>
    Friend NotInheritable Class TupleVirtualElementFieldSymbol
        Inherits TupleElementFieldSymbol

        Public Sub New(container As TupleTypeSymbol,
                       underlyingField As FieldSymbol,
                       name As String,
                       tupleElementOrdinal As Integer,
                       location As Location,
                       isImplicitlyDeclared As Boolean,
                       correspondingDefaultFieldOpt As TupleElementFieldSymbol)

            MyBase.New(container, underlyingField, tupleElementOrdinal, location, isImplicitlyDeclared, correspondingDefaultFieldOpt)

            Debug.Assert(name <> Nothing)
            Debug.Assert(name <> underlyingField.Name OrElse Not container.UnderlyingNamedType.Equals(underlyingField.ContainingType),
                                "fields that map directly to underlying should not be represented by " & NameOf(TupleVirtualElementFieldSymbol))

            Me.Name = name
        End Sub

        Public Overrides ReadOnly Property Name As String

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer? = Nothing

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol = Nothing

        Public Overrides ReadOnly Property IsVirtualTupleField As Boolean = True

    End Class

End Namespace
