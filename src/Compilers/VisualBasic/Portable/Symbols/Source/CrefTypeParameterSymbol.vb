﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Type parameters in documentation comments are complicated since they sort of act as declarations,
    ''' rather than references. Like in the following example:
    ''' 
    '''     <code>
    '''         ''' <see CREF="TypeA(Of X, Y).MethodB(x As X, y As Y)"/>
    '''         Class Clazz
    '''             ...
    '''     </code>
    ''' 
    ''' </summary>
    Friend NotInheritable Class CrefTypeParameterSymbol
        Inherits TypeParameterSymbol

        Private ReadOnly _ordinal As Integer ' 0 is first type parameter, etc.
        Private ReadOnly _name As String
        Private ReadOnly _syntaxReference As SyntaxReference

        Public Sub New(ordinal As Integer, name As String, syntax As TypeSyntax)
            _ordinal = ordinal
            _name = name
            _syntaxReference = syntax.GetReference()
        End Sub

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return ImmutableArray(Of VisualBasicAttributeData).Empty
        End Function

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                Return ImmutableArray(Of TypeSymbol).Empty
            End Get
        End Property

        Friend Overrides Function GetConstraints() As ImmutableArray(Of TypeParameterConstraint)
            Return ImmutableArray(Of TypeParameterConstraint).Empty
        End Function

        Friend Overrides Sub ResolveConstraints(inProgress As ConsList(Of TypeParameterSymbol))
        End Sub

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
        End Sub

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray.Create(Of SyntaxReference)(_syntaxReference)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(Of Location)(_syntaxReference.GetLocation())
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Return TypeParameterKind.Cref
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return VarianceKind.None
            End Get
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Nothing Then
                Return False
            End If

            If Me Is obj Then
                Return True
            End If

            Dim other = TryCast(obj, CrefTypeParameterSymbol)
            If other Is Nothing Then
                Return False
            End If

            Return (_name = other._name) AndAlso (_ordinal = other._ordinal) AndAlso
                    _syntaxReference.GetSyntax().Equals(other._syntaxReference.GetSyntax())
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(_name, _ordinal)
        End Function
    End Class

End Namespace

