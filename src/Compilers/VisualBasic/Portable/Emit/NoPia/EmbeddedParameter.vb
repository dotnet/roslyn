' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedParameter
        Inherits EmbeddedTypesManager.CommonEmbeddedParameter

        Public Sub New(containingPropertyOrMethod As EmbeddedTypesManager.CommonEmbeddedMember, underlyingParameter As ParameterSymbol)
            MyBase.New(containingPropertyOrMethod, underlyingParameter)
            Debug.Assert(underlyingParameter.IsDefinition)
        End Sub

        Protected Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return UnderlyingParameter.GetCustomAttributesToEmit(moduleBuilder.CompilationState)
        End Function

        Protected Overrides ReadOnly Property HasDefaultValue As Boolean
            Get
                Return UnderlyingParameter.HasMetadataConstantValue
            End Get
        End Property

        Protected Overrides Function GetDefaultValue(context As EmitContext) As MetadataConstant
            Return UnderlyingParameter.GetMetadataConstantValue(context)
        End Function

        Protected Overrides ReadOnly Property IsIn As Boolean
            Get
                Return UnderlyingParameter.IsMetadataIn
            End Get
        End Property

        Protected Overrides ReadOnly Property IsOut As Boolean
            Get
                Return UnderlyingParameter.IsMetadataOut
            End Get
        End Property

        Protected Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return UnderlyingParameter.IsMetadataOptional
            End Get
        End Property

        Protected Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return UnderlyingParameter.IsMarshalledExplicitly
            End Get
        End Property

        Protected Overrides ReadOnly Property MarshallingInformation As Cci.IMarshallingInformation
            Get
                Return UnderlyingParameter.MarshallingInformation
            End Get
        End Property

        Protected Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return UnderlyingParameter.MarshallingDescriptor
            End Get
        End Property

        Protected Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingParameter.MetadataName
            End Get
        End Property

        Protected Overrides ReadOnly Property UnderlyingParameterTypeInformation As Cci.IParameterTypeInformation
            Get
                Return DirectCast(UnderlyingParameter, Cci.IParameterTypeInformation)
            End Get
        End Property

        Protected Overrides ReadOnly Property Index As UShort
            Get
                Return CUShort(UnderlyingParameter.Ordinal)
            End Get
        End Property

    End Class

End Namespace
