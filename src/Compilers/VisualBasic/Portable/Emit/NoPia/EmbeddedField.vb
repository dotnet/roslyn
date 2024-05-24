' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

#If Not DEBUG Then
Imports FieldSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.FieldSymbol
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedField
        Inherits EmbeddedTypesManager.CommonEmbeddedField

        Public Sub New(containingType As EmbeddedType, underlyingField As FieldSymbolAdapter)
            MyBase.New(containingType, underlyingField)
        End Sub

        Friend Overrides ReadOnly Property TypeManager As EmbeddedTypesManager
            Get
                Return ContainingType.TypeManager
            End Get
        End Property

        Protected Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return UnderlyingField.AdaptedFieldSymbol.GetCustomAttributesToEmit(moduleBuilder)
        End Function

        Protected Overrides Function GetCompileTimeValue(context As EmitContext) As MetadataConstant
            Return UnderlyingField.GetMetadataConstantValue(context)
        End Function

        Protected Overrides ReadOnly Property IsCompileTimeConstant As Boolean
            Get
                Return UnderlyingField.AdaptedFieldSymbol.IsMetadataConstant
            End Get
        End Property

        Protected Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Return UnderlyingField.AdaptedFieldSymbol.IsNotSerialized
            End Get
        End Property

        Protected Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return UnderlyingField.AdaptedFieldSymbol.IsReadOnly
            End Get
        End Property

        Protected Overrides ReadOnly Property IsRuntimeSpecial As Boolean
            Get
                Return UnderlyingField.AdaptedFieldSymbol.HasRuntimeSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSpecialName As Boolean
            Get
                Return UnderlyingField.AdaptedFieldSymbol.HasSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsStatic As Boolean
            Get
                Return UnderlyingField.AdaptedFieldSymbol.IsShared
            End Get
        End Property

        Protected Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return UnderlyingField.AdaptedFieldSymbol.IsMarshalledExplicitly
            End Get
        End Property

        Protected Overrides ReadOnly Property MarshallingInformation As Cci.IMarshallingInformation
            Get
                Return UnderlyingField.AdaptedFieldSymbol.MarshallingInformation
            End Get
        End Property

        Protected Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return UnderlyingField.AdaptedFieldSymbol.MarshallingDescriptor
            End Get
        End Property

        Protected Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return UnderlyingField.AdaptedFieldSymbol.TypeLayoutOffset
            End Get
        End Property

        Protected Overrides ReadOnly Property Visibility As Cci.TypeMemberVisibility
            Get
                Return UnderlyingField.AdaptedFieldSymbol.MetadataVisibility
            End Get
        End Property

        Protected Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingField.AdaptedFieldSymbol.MetadataName
            End Get
        End Property

    End Class

End Namespace
