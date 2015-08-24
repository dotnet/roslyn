' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Binder
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a parameters declared in source, that is not optional, does not have a default value, 
    ''' attributes, or is a ParamArray. This is a separate class to save memory, since there are LOTS
    ''' of parameters.
    ''' </summary>
    Friend Class SourceSimpleParameterSymbol
        Inherits SourceParameterSymbol

        Friend Sub New(
            container As Symbol,
            name As String,
            ordinal As Integer,
            type As TypeSymbol,
            location As Location)

            MyBase.New(container, name, ordinal, type, location)
        End Sub

        Friend Overrides Function ChangeOwner(newContainingSymbol As Symbol) As ParameterSymbol
            Return New SourceSimpleParameterSymbol(newContainingSymbol, Name, Ordinal, Type, Location)
        End Function

        Private Function GetCorrespondingPartialParameter() As SourceComplexParameterSymbol
            ' the attributes for partial method implementation are stored on the corresponding definition:
            Dim method = TryCast(Me.ContainingSymbol, SourceMemberMethodSymbol)
            If method IsNot Nothing AndAlso method.IsPartialImplementation Then
                ' partial definition always has complex parameters:
                Return DirectCast(method.SourcePartialDefinition.Parameters(Me.Ordinal), SourceComplexParameterSymbol)
            End If

            Return Nothing
        End Function

        Friend Overrides ReadOnly Property AttributeDeclarationList As SyntaxList(Of AttributeListSyntax)
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            ' the attributes for partial method implementation are stored on the corresponding definition:
            Dim other = GetCorrespondingPartialParameter()
            If other IsNot Nothing Then
                Return other.GetAttributesBag()
            End If

            Return CustomAttributesBag(Of VisualBasicAttributeData).Empty
        End Function

        Friend Overrides Function GetEarlyDecodedWellKnownAttributeData() As ParameterEarlyWellKnownAttributeData
            ' the attributes for partial method implementation are stored on the corresponding definition:
            Dim other = GetCorrespondingPartialParameter()
            If other IsNot Nothing Then
                Return other.GetEarlyDecodedWellKnownAttributeData()
            End If

            Return Nothing
        End Function

        Friend Overrides Function GetDecodedWellKnownAttributeData() As CommonParameterWellKnownAttributeData
            ' the attributes for partial method implementation are stored on the corresponding definition:
            Dim other = GetCorrespondingPartialParameter()
            If other IsNot Nothing Then
                Return other.GetDecodedWellKnownAttributeData()
            End If

            Return Nothing
        End Function

        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Dim data = GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasParamArrayAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                Dim data = GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasCallerLineNumberAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                Dim data = GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasCallerMemberNameAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                Dim data = GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasCallerFilePathAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                ' SourceSimpleParameterSymbol is never created for a parameter with ByRef modifier.
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property CountOfCustomModifiersPrecedingByRef As UShort
            Get
                Return 0
            End Get
        End Property

        Friend Overrides Function WithTypeAndCustomModifiers(type As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), countOfCustomModifiersPrecedingByRef As UShort) As ParameterSymbol
            If customModifiers.IsDefaultOrEmpty Then
                Return New SourceSimpleParameterSymbol(Me.ContainingSymbol, Me.Name, Me.Ordinal, type, Me.Location)
            End If

            Return New SourceSimpleParameterSymbolWithCustomModifiers(Me.ContainingSymbol, Me.Name, Me.Ordinal, type, Me.Location, customModifiers, countOfCustomModifiersPrecedingByRef)
        End Function

        Friend NotInheritable Class SourceSimpleParameterSymbolWithCustomModifiers
            Inherits SourceSimpleParameterSymbol

            Private ReadOnly _customModifiers As ImmutableArray(Of CustomModifier)
            Private ReadOnly _countOfCustomModifiersPrecedingByRef As UShort

            Friend Sub New(
                container As Symbol,
                name As String,
                ordinal As Integer,
                type As TypeSymbol,
                location As Location,
                customModifiers As ImmutableArray(Of CustomModifier),
                countOfCustomModifiersPrecedingByRef As UShort
            )
                MyBase.New(container, name, ordinal, type, location)

                Debug.Assert(Not customModifiers.IsDefaultOrEmpty)
                _customModifiers = If(customModifiers.IsDefault, ImmutableArray(Of CustomModifier).Empty, customModifiers)
                _countOfCustomModifiersPrecedingByRef = countOfCustomModifiersPrecedingByRef

                Debug.Assert(_countOfCustomModifiersPrecedingByRef = 0 OrElse IsByRef)
                Debug.Assert(_countOfCustomModifiersPrecedingByRef <= _customModifiers.Length)
            End Sub

            Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _customModifiers
                End Get
            End Property

            Friend Overrides ReadOnly Property CountOfCustomModifiersPrecedingByRef As UShort
                Get
                    Return _countOfCustomModifiersPrecedingByRef
                End Get
            End Property

            Friend Overrides Function WithTypeAndCustomModifiers(type As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), countOfCustomModifiersPrecedingByRef As UShort) As ParameterSymbol
                Throw ExceptionUtilities.Unreachable
            End Function
        End Class

    End Class

End Namespace

