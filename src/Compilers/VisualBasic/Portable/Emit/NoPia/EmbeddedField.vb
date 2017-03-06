﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedField
        Inherits EmbeddedTypesManager.CommonEmbeddedField

        Public Sub New(containingType As EmbeddedType, underlyingField As FieldSymbol)
            MyBase.New(containingType, underlyingField)
        End Sub

        Friend Overrides ReadOnly Property TypeManager As EmbeddedTypesManager
            Get
                Return ContainingType.TypeManager
            End Get
        End Property

        Protected Overrides Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            Return UnderlyingField.GetCustomAttributesToEmit(compilationState)
        End Function

        Protected Overrides Function GetCompileTimeValue(context As EmitContext) As MetadataConstant
            Return UnderlyingField.GetMetadataConstantValue(context)
        End Function

        Protected Overrides ReadOnly Property IsCompileTimeConstant As Boolean
            Get
                Return UnderlyingField.IsMetadataConstant
            End Get
        End Property

        Protected Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Return UnderlyingField.IsNotSerialized
            End Get
        End Property

        Protected Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return UnderlyingField.IsReadOnly
            End Get
        End Property

        Protected Overrides ReadOnly Property IsRuntimeSpecial As Boolean
            Get
                Return UnderlyingField.HasRuntimeSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSpecialName As Boolean
            Get
                Return UnderlyingField.HasSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsStatic As Boolean
            Get
                Return UnderlyingField.IsShared
            End Get
        End Property

        Protected Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return UnderlyingField.IsMarshalledExplicitly
            End Get
        End Property

        Protected Overrides ReadOnly Property MarshallingInformation As Cci.IMarshallingInformation
            Get
                Return UnderlyingField.MarshallingInformation
            End Get
        End Property

        Protected Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return UnderlyingField.MarshallingDescriptor
            End Get
        End Property

        Protected Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return UnderlyingField.TypeLayoutOffset
            End Get
        End Property

        Protected Overrides ReadOnly Property Visibility As Cci.TypeMemberVisibility
            Get
                Return PEModuleBuilder.MemberVisibility(UnderlyingField)
            End Get
        End Property

        Protected Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingField.MetadataName
            End Get
        End Property

    End Class

End Namespace
