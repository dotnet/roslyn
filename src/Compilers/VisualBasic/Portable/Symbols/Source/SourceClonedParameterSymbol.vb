' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a source parameter cloned from another <see cref="SourceParameterSymbol"/> , 
    ''' when they must share attribute data.
    ''' </summary>  
    ''' <remarks>
    ''' For example, parameters on delegate Invoke method are cloned to delegate BeginInvoke, EndInvoke methods. 
    ''' </remarks>
    Friend MustInherit Class SourceClonedParameterSymbol
        Inherits SourceParameterSymbolBase

        Protected ReadOnly _originalParam As SourceParameterSymbolBase

        Friend Sub New(originalParam As SourceParameterSymbolBase, newOwner As MethodSymbol, newOrdinal As Integer)
            MyBase.New(newOwner, newOrdinal)
            Debug.Assert(originalParam IsNot Nothing)
            _originalParam = originalParam
        End Sub

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                ' Since you can't get from the syntax node that represents the original parameter 
                ' back to this symbol we decided not to return the original syntax node here.
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property
#Region "Forwarded"

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _originalParam.Type
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return _originalParam.IsMetadataIn
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return _originalParam.IsMetadataOut
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _originalParam.Locations
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return _originalParam.GetAttributes()
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return _originalParam.Name
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _originalParam.CustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _originalParam.RefCustomModifiers
            End Get
        End Property

        Friend Overloads Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return _originalParam.ExplicitDefaultConstantValue(inProgress)
            End Get
        End Property

        Friend Overrides ReadOnly Property HasParamArrayAttribute As Boolean
            Get
                Return _originalParam.HasParamArrayAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDefaultValueAttribute As Boolean
            Get
                Return _originalParam.HasDefaultValueAttribute
            End Get
        End Property

        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return _originalParam.HasExplicitDefaultValue
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return _originalParam.HasOptionCompare
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                Return _originalParam.IsIDispatchConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                Return _originalParam.IsIUnknownConstant
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return _originalParam.IsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return _originalParam.IsExplicitByRef
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return _originalParam.IsOptional
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return _originalParam.IsParamArray
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return _originalParam.MarshallingInformation
            End Get
        End Property
#End Region

        Friend Overrides Function WithTypeAndCustomModifiers(type As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), refCustomModifiers As ImmutableArray(Of CustomModifier)) As ParameterSymbol
            Return New SourceClonedParameterSymbolWithCustomModifiers(Me, DirectCast(Me.ContainingSymbol, MethodSymbol), Me.Ordinal, type, customModifiers, refCustomModifiers)
        End Function

        Friend NotInheritable Class SourceClonedParameterSymbolWithCustomModifiers
            Inherits SourceClonedParameterSymbol

            Private ReadOnly _type As TypeSymbol
            Private ReadOnly _customModifiers As ImmutableArray(Of CustomModifier)
            Private ReadOnly _refCustomModifiers As ImmutableArray(Of CustomModifier)

            Friend Sub New(
                originalParam As SourceClonedParameterSymbol,
                newOwner As MethodSymbol,
                newOrdinal As Integer,
                type As TypeSymbol,
                customModifiers As ImmutableArray(Of CustomModifier),
                refCustomModifiers As ImmutableArray(Of CustomModifier)
            )
                MyBase.New(originalParam, newOwner, newOrdinal)
                _type = type
                _customModifiers = customModifiers.NullToEmpty()
                _refCustomModifiers = refCustomModifiers.NullToEmpty()

                Debug.Assert(_refCustomModifiers.IsEmpty OrElse IsByRef)
            End Sub

            Public Overrides ReadOnly Property Type As TypeSymbol
                Get
                    Return _type
                End Get
            End Property

            Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _customModifiers
                End Get
            End Property

            Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _refCustomModifiers
                End Get
            End Property

            Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
                Get
                    Return _originalParam.IsCallerLineNumber
                End Get
            End Property

            Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
                Get
                    Return _originalParam.IsCallerMemberName
                End Get
            End Property

            Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
                Get
                    Return _originalParam.IsCallerFilePath
                End Get
            End Property

            Friend Overrides ReadOnly Property CallerArgumentExpressionParameterIndex As Integer
                Get
                    Return _originalParam.CallerArgumentExpressionParameterIndex
                End Get
            End Property

            Friend Overrides Function WithTypeAndCustomModifiers(type As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), refCustomModifiers As ImmutableArray(Of CustomModifier)) As ParameterSymbol
                Throw ExceptionUtilities.Unreachable
            End Function
        End Class
    End Class
End Namespace
