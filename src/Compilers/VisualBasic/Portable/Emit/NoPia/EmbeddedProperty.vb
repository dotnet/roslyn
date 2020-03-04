' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedProperty
        Inherits EmbeddedTypesManager.CommonEmbeddedProperty

        Public Sub New(underlyingProperty As PropertySymbol, getter As EmbeddedMethod, setter As EmbeddedMethod)
            MyBase.New(underlyingProperty, getter, setter)
        End Sub

        Protected Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return UnderlyingProperty.GetCustomAttributesToEmit(moduleBuilder.CompilationState)
        End Function

        Protected Overrides Function GetParameters() As ImmutableArray(Of EmbeddedParameter)
            Return EmbeddedTypesManager.EmbedParameters(Me, UnderlyingProperty.Parameters)
        End Function

        Protected Overrides ReadOnly Property IsRuntimeSpecial As Boolean
            Get
                Return UnderlyingProperty.HasRuntimeSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSpecialName As Boolean
            Get
                Return UnderlyingProperty.HasSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property UnderlyingPropertySignature As ISignature
            Get
                Return UnderlyingProperty
            End Get
        End Property

        Protected Overrides ReadOnly Property ContainingType As EmbeddedType
            Get
                Return AnAccessor.ContainingType
            End Get
        End Property

        Protected Overrides ReadOnly Property Visibility As Cci.TypeMemberVisibility
            Get
                Return PEModuleBuilder.MemberVisibility(UnderlyingProperty)
            End Get
        End Property

        Protected Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingProperty.MetadataName
            End Get
        End Property

    End Class

End Namespace
