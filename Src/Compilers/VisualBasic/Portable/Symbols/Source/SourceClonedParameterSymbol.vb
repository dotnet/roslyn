' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a source parameter cloned from another <see cref="SourceParameterSymbol"/> , 
    ''' when they must share attribute data.
    ''' </summary>  
    ''' <remarks>
    ''' For example, parameters on delegate Invoke method are cloned to delegate BeginInvoke, EndInvoke methods. 
    ''' </remarks>
    Friend Class SourceClonedParameterSymbol
        Inherits SourceParameterSymbolBase

        Private ReadOnly m_originalParam As SourceParameterSymbol

        Friend Sub New(originalParam As SourceParameterSymbol, newOwner As MethodSymbol, newOrdinal As Integer)
            MyBase.New(newOwner, newOrdinal)
            Debug.Assert(originalParam IsNot Nothing)
            m_originalParam = originalParam
        End Sub

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                ' Since you can't get from the syntax node that represents the orginal parameter 
                ' back to this symbol we decided not to return the original syntax node here.
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property
#Region "Forwarded"

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return m_originalParam.Type
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return m_originalParam.IsMetadataIn
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return m_originalParam.IsMetadataOut
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_originalParam.Locations
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return m_originalParam.GetAttributes()
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_originalParam.Name
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return m_originalParam.CustomModifiers
            End Get
        End Property

        Friend Overrides ReadOnly Property HasByRefBeforeCustomModifiers As Boolean
            Get
                Return m_originalParam.HasByRefBeforeCustomModifiers
            End Get
        End Property

        Friend Overloads Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return m_originalParam.ExplicitDefaultConstantValue(inProgress)
            End Get
        End Property

        Friend Overrides ReadOnly Property HasParamArrayAttribute As Boolean
            Get
                Return m_originalParam.HasParamArrayAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDefaultValueAttribute As Boolean
            Get
                Return m_originalParam.HasDefaultValueAttribute
            End Get
        End Property

        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return m_originalParam.HasExplicitDefaultValue
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return m_originalParam.HasOptionCompare
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                Return m_originalParam.IsIDispatchConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                Return m_originalParam.IsIUnknownConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                Return m_originalParam.IsCallerLineNumber
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                Return m_originalParam.IsCallerMemberName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                Return m_originalParam.IsCallerFilePath
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return m_originalParam.IsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return m_originalParam.IsExplicitByRef
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return m_originalParam.IsOptional
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return m_originalParam.IsParamArray
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return m_originalParam.MarshallingInformation
            End Get
        End Property
#End Region

        Friend Overrides Function WithTypeAndCustomModifiers(type As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), hasByRefBeforeCustomModifiers As Boolean) As ParameterSymbol
            Return New SourceClonedParameterSymbolWithCustomModifiers(m_originalParam, DirectCast(Me.ContainingSymbol, MethodSymbol), Me.Ordinal, type, customModifiers, hasByRefBeforeCustomModifiers)
        End Function

        Friend NotInheritable Class SourceClonedParameterSymbolWithCustomModifiers
            Inherits SourceClonedParameterSymbol

            Private ReadOnly m_type As TypeSymbol
            Private ReadOnly m_customModifiers As ImmutableArray(Of CustomModifier)
            Private ReadOnly m_hasByRefBeforeCustomModifiers As Boolean

            Friend Sub New(
                originalParam As SourceParameterSymbol,
                newOwner As MethodSymbol,
                newOrdinal As Integer,
                type As TypeSymbol,
                customModifiers As ImmutableArray(Of CustomModifier),
                hasByRefBeforeCustomModifiers As Boolean
            )
                MyBase.New(originalParam, newOwner, newOrdinal)
                m_type = type
                m_customModifiers = If(customModifiers.IsDefault, ImmutableArray(Of CustomModifier).Empty, customModifiers)
                m_hasByRefBeforeCustomModifiers = hasByRefBeforeCustomModifiers
            End Sub

            Public Overrides ReadOnly Property Type As TypeSymbol
                Get
                    Return m_type
                End Get
            End Property

            Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return m_customModifiers
                End Get
            End Property

            Friend Overrides ReadOnly Property HasByRefBeforeCustomModifiers As Boolean
                Get
                    Return m_hasByRefBeforeCustomModifiers
                End Get
            End Property

            Friend Overrides Function WithTypeAndCustomModifiers(type As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), hasByRefBeforeCustomModifiers As Boolean) As ParameterSymbol
                Throw ExceptionUtilities.Unreachable
            End Function
        End Class
    End Class
End Namespace