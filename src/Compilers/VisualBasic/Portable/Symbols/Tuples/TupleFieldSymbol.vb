' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis

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

        Public Overrides ReadOnly Property IsTupleField As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property TupleUnderlyingField As FieldSymbol
            Get
                Return Me._underlyingField
            End Get
        End Property

        Public ReadOnly Property TupleFieldId As Integer
            Get
                Return Me._tupleElementIndex
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Me._containingTuple.GetTupleMemberSymbolForUnderlyingMember(Of Symbol)(Me._underlyingField.AssociatedSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me._containingTuple
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me._underlyingField.CustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return Me._underlyingField.Type
            End Get
        End Property

        Public Sub New(container As TupleTypeSymbol, underlyingField As FieldSymbol, tupleElementIndex As Integer)
            MyBase.New(underlyingField)

            Debug.Assert(container.UnderlyingNamedType.IsSameTypeIgnoringCustomModifiers(underlyingField.ContainingType) OrElse TypeOf Me Is TupleVirtualElementFieldSymbol,
                                            "virtual fields should be represented by " + NameOf(TupleVirtualElementFieldSymbol))

            _containingTuple = container
            _tupleElementIndex = tupleElementIndex
        End Sub

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me._underlyingField.GetAttributes()
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Dim useSiteDiagnostic As DiagnosticInfo = MyBase.GetUseSiteErrorInfo
            MyBase.MergeUseSiteErrorInfo(useSiteDiagnostic, Me._underlyingField.GetUseSiteErrorInfo())
            Return useSiteDiagnostic
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(
                Hash.Combine(_containingTuple.GetHashCode(), _tupleElementIndex.GetHashCode()),
                             Me.Name.GetHashCode())
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, TupleFieldSymbol))
        End Function

        Public Overloads Function Equals(other As TupleFieldSymbol) As Boolean
            Return other IsNot Nothing AndAlso
                _tupleElementIndex = other._tupleElementIndex AndAlso
                _containingTuple = other._containingTuple
        End Function
    End Class

    ''' <summary>
    ''' Represents an element field of a tuple type (such as (int, byte).Item1)
    ''' that is backed by a real field with the same name within the tuple underlying type.
    ''' </summary>
    Friend Class TupleElementFieldSymbol
        Inherits TupleFieldSymbol

        Private _locations As ImmutableArray(Of Location)

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me._locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return Symbol.GetDeclaringSyntaxReferenceHelper(Of VisualBasicSyntaxNode)(Me._locations)
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Dim flag As Boolean = Me._underlyingField.ContainingType IsNot Me._containingTuple.TupleUnderlyingType
                Dim result As Integer?
                If flag Then
                    result = Nothing
                Else
                    result = MyBase.TypeLayoutOffset
                End If
                Return result
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Dim flag As Boolean = Me._underlyingField.ContainingType IsNot Me._containingTuple.TupleUnderlyingType
                Dim result As Symbol
                If flag Then
                    result = Nothing
                Else
                    result = MyBase.AssociatedSymbol
                End If
                Return result
            End Get
        End Property

        Public Sub New(container As TupleTypeSymbol, underlyingField As FieldSymbol, tupleFieldId As Integer, location As Location)
            MyBase.New(container, underlyingField, tupleFieldId)
            Me._locations = If((location Is Nothing), ImmutableArray(Of Location).Empty, ImmutableArray.Create(Of Location)(location))
        End Sub
    End Class

    ''' <summary>
    ''' Represents an element field of a tuple type (such as (int a, byte b).a, or (int a, byte b).b)
    ''' that is backed by a real field with a different name within the tuple underlying type.
    ''' </summary>
    Friend NotInheritable Class TupleVirtualElementFieldSymbol
        Inherits TupleElementFieldSymbol

        Private _name As String

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me._name
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Sub New(container As TupleTypeSymbol, underlyingField As FieldSymbol, name As String, tupleElementOrdinal As Integer, location As Location)
            MyBase.New(container, underlyingField, tupleElementOrdinal, location)
            Debug.Assert(name <> Nothing)
            Debug.Assert(name <> underlyingField.Name)
            Me._name = name
        End Sub
    End Class
End Namespace
