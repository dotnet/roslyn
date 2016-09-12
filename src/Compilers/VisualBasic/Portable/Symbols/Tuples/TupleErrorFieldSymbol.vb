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
        ''' If this field represents a tuple element (including the name match), 
        ''' id is an index of the element (zero-based).
        ''' Otherwise, (-1 - [index in members array]);
        ''' </summary>
        Private ReadOnly _tupleFieldId As Integer

        Private ReadOnly _locations As ImmutableArray(Of Location)

        Private ReadOnly _useSiteDiagnosticInfo As DiagnosticInfo

        Public Overrides ReadOnly Property IsTupleField As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' If this field represents a tuple element (including the name match), 
        ''' id is an index of the element (zero-based).
        ''' Otherwise, (-1 - [index in members array]);
        ''' </summary>
        Public ReadOnly Property TupleFieldId As Integer
            Get
                Return Me._tupleFieldId
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
                Return Symbol.GetDeclaringSyntaxReferenceHelper(Of VisualBasicSyntaxNode)(Me._locations)
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Sub New(container As NamedTypeSymbol, name As String, tupleFieldId As Integer, location As Location, type As TypeSymbol, useSiteDiagnosticInfo As DiagnosticInfo)
            MyBase.New(container, container, type, name, Accessibility.Public)
            Debug.Assert(name <> Nothing)
            Me._locations = If((location Is Nothing), ImmutableArray(Of Location).Empty, ImmutableArray.Create(Of Location)(location))
            Me._useSiteDiagnosticInfo = useSiteDiagnosticInfo
            Me._tupleFieldId = tupleFieldId
        End Sub

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return Me._type
            End Get
        End Property

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Return Me._useSiteDiagnosticInfo
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Me.ContainingType.GetHashCode(), Me._tupleFieldId.GetHashCode())
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, TupleErrorFieldSymbol))
        End Function

        Public Overloads Function Equals(other As TupleErrorFieldSymbol) As Boolean
            Return other Is Me OrElse
                (other IsNot Nothing AndAlso Me._tupleFieldId = other._tupleFieldId AndAlso Me.ContainingType = other.ContainingType)
        End Function
    End Class
End Namespace
